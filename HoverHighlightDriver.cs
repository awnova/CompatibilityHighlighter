using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using UnityEngine;
using InvContainer = EFT.InventoryLogic.IContainer;

namespace CompatibilityHighlighter
{
    public sealed class HoverHighlightDriver : MonoBehaviour
    {
        internal static HoverHighlightDriver Instance { get; private set; }

        private ItemView _pendingView;
        private float _pendingSince;
        private ItemContextClass _forwardContext;
        private readonly List<ItemView> _reverseTinted = new List<ItemView>();
        private readonly List<RectTransform> _borderCells = new List<RectTransform>();
        private bool _dragActive;

        internal bool SessionActive => _forwardContext != null || _reverseTinted.Count > 0;

        private static bool FrozenForDrag => !Plugin.SuppressDuringDrag.Value;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            ClearHighlight();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        internal void OnHoverEnter(ItemView view)
        {
            if (!Plugin.Enabled.Value || view == null)
            {
                return;
            }

            if (_dragActive)
            {
                return;
            }

            var context = view.ItemContext;
            if (context == null || !context.DragAvailable || !context.IsPreviewHighlightAvailable)
            {
                return;
            }

            ClearHighlight();
            _pendingView = view;
            _pendingSince = Time.unscaledTime;
        }

        internal void OnHoverExit(ItemView view)
        {
            if (_dragActive && FrozenForDrag)
            {
                return;
            }

            if (view == _pendingView)
            {
                _pendingView = null;
            }

            ClearHighlight();
        }

        internal void OnDragBegin()
        {
            _dragActive = true;

            if (FrozenForDrag)
            {
                return;
            }

            _pendingView = null;
            ClearHighlight();
        }

        internal void OnDragEnd()
        {
            _dragActive = false;
            _pendingView = null;
            ClearHighlight();
        }

        private void Update()
        {
            if (_pendingView == null || _forwardContext != null)
            {
                return;
            }

            if (Time.unscaledTime - _pendingSince < Plugin.HoverDelay.Value)
            {
                return;
            }

            var view = _pendingView;
            if (view.ItemContext == null || view.Item == null)
            {
                _pendingView = null;
                return;
            }

            try
            {
                HighlightForward(view);
                HighlightReverse(view);
                RebuildBorderOverlay();
            }
            catch (Exception e)
            {
                Plugin.LOG.LogError($"Failed to apply hover highlight: {e}");
                ClearHighlight();
            }
        }

        private void RebuildBorderOverlay()
        {
            _borderCells.Clear();

            var views = GetAllItemViews();
            if (views == null)
            {
                CellBorderOverlay.Clear();
                return;
            }

            foreach (var candidate in views)
            {
                if (candidate != null && ViewFields.HighlightedGlobally(candidate) &&
                    candidate.transform is RectTransform rect)
                {
                    _borderCells.Add(rect);
                }
            }

            CellBorderOverlay.Rebuild(_borderCells, Plugin.CompatibleColor.Value);
        }

        private void HighlightForward(ItemView view)
        {
            var ui = ItemUiContext.Instance;
            if (ui == null)
            {
                return;
            }

            _forwardContext = new ItemContextClass(view.ItemContext, view.ItemRotation);
            ui.RegisterView(_forwardContext);
        }

        private void HighlightReverse(ItemView view)
        {
            var hoveredItem = view.Item;
            var acceptors = CollectAcceptors(hoveredItem, out var installed);

            var views = GetAllItemViews();
            if (views == null)
            {
                return;
            }

            var color = Plugin.CompatibleColor.Value;

            foreach (var candidate in views)
            {
                if (candidate == null || candidate == view)
                {
                    continue;
                }

                var item = candidate.Item;
                if (item == null || item == hoveredItem || installed.Contains(item))
                {
                    continue;
                }

                if (ViewFields.HighlightedGlobally(candidate))
                {
                    continue;
                }

                var fits = acceptors.Count > 0 && FitsAny(acceptors, item);

                if (!fits)
                {
                    var candidateAcceptors = CollectAcceptors(item, out _);
                    fits = candidateAcceptors.Count > 0 && FitsAny(candidateAcceptors, hoveredItem);
                }

                if (!fits)
                {
                    continue;
                }

                ViewFields.ApplyHighlight(candidate, color);
                _reverseTinted.Add(candidate);
            }
        }

        private static List<InvContainer> CollectAcceptors(Item item, out HashSet<Item> installed)
        {
            var acceptors = new List<InvContainer>();
            installed = new HashSet<Item>();

            if (item is CompoundItem compound)
            {
                foreach (var slot in compound.AllSlots)
                {
                    acceptors.Add(slot);
                    if (slot.ContainedItem != null)
                    {
                        installed.Add(slot.ContainedItem);
                    }
                }
            }

            if (item is IAmmoContainer ammo && ammo.Cartridges != null)
            {
                acceptors.Add(ammo.Cartridges);
            }

            return acceptors;
        }

        private static ItemView[] GetAllItemViews()
        {
            var ui = Singleton<CommonUI>.Instance;
            return ui != null ? ui.GetComponentsInChildren<ItemView>(false) : null;
        }

        private static bool FitsAny(List<InvContainer> acceptors, Item item)
        {
            foreach (var acceptor in acceptors)
            {
                try
                {
                    if (acceptor.CheckCompatibility(item))
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }

            return false;
        }

        private void ClearHighlight()
        {
            if (_forwardContext != null)
            {
                try
                {
                    ItemUiContext.Instance?.UnregisterView(_forwardContext);
                }
                catch (Exception e)
                {
                    Plugin.LOG.LogError($"Failed to clear hover highlight: {e}");
                }

                _forwardContext = null;
            }

            ClearReverseTints();
            CellBorderOverlay.Clear();
        }

        private void ClearReverseTints()
        {
            foreach (var view in _reverseTinted)
            {
                if (view != null)
                {
                    ViewFields.ClearHighlight(view);
                }
            }

            _reverseTinted.Clear();
        }
    }
}
