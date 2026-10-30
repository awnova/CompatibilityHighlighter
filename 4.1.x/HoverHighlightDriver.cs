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
        private DragItemContext _forwardContext;
        private readonly List<ItemView> _reverseTinted = new List<ItemView>();
        private readonly List<RectTransform> _borderCells = new List<RectTransform>();
        private bool _dragActive;
        private bool _sessionApplied;
        private bool _loggedCompatibilityError;

        internal bool SessionActive => _forwardContext != null || _reverseTinted.Count > 0;

        internal DragItemContext ForwardContext => _forwardContext;

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

            // An unsearched or unexamined item's identity is hidden, so hovering it must not reveal
            // what it fits.
            if (!ViewFields.IsRevealed(view))
            {
                return;
            }

            var context = view.ItemContext;
            if (context == null || !context.IsPreviewHighlightAvailable)
            {
                return;
            }

            var traderException = Plugin.HighlightTraderInventory.Value &&
                                   context.ViewType == EItemViewType.TradingTrader;
            if (!context.DragAvailable && !traderException)
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
            if (_pendingView == null || _sessionApplied)
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
                _sessionApplied = true;
                var views = GetAllItemViews();
                HighlightForward(view);
                HighlightReverse(view, views);
                RebuildBorderOverlay(views);
            }
            catch (Exception e)
            {
                Plugin.LOG.LogError($"Failed to apply hover highlight: {e}");
                _pendingView = null;
                ClearHighlight();
            }
        }

        private void RebuildBorderOverlay(ItemView[] views)
        {
            _borderCells.Clear();

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
            if (ui == null || view.ItemContext == null || !view.ItemContext.DragAvailable)
            {
                return;
            }

            // DragItemContext's ctor reads Item.Parent, which throws for detached items
            // (trader offers, previews, etc.) that have no CurrentAddress.
            if (view.Item.CurrentAddress == null)
            {
                return;
            }

            _forwardContext = new DragItemContext(view.ItemContext, view.ItemRotation);
            ui.RegisterView(_forwardContext);
        }

        private void HighlightReverse(ItemView view, ItemView[] views)
        {
            var hoveredItem = view.Item;
            var acceptors = new List<InvContainer>();
            var installed = new HashSet<Item>();
            CollectAcceptors(hoveredItem, acceptors, installed);

            var candidateAcceptors = new List<InvContainer>();
            var color = Plugin.CompatibleColor.Value;

            foreach (var candidate in views)
            {
                if (candidate == null || candidate == view || !ViewFields.IsRevealed(candidate))
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
                    candidateAcceptors.Clear();
                    CollectAcceptors(item, candidateAcceptors, null);
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

        private static void CollectAcceptors(Item item, List<InvContainer> acceptors, HashSet<Item> installed)
        {
            if (item is CompoundItem compound)
            {
                foreach (var slot in compound.AllSlots)
                {
                    acceptors.Add(slot);
                    if (installed != null && slot.ContainedItem != null)
                    {
                        installed.Add(slot.ContainedItem);
                    }
                }
            }

            if (item is IAmmoContainer ammo && ammo.Cartridges != null)
            {
                acceptors.Add(ammo.Cartridges);
            }
        }

        // ItemViews are split across two separate UI roots: CommonUI (raid/stash screens)
        // and MenuUI (the trader's buy/sell screen), so both must be searched.
        private static ItemView[] GetAllItemViews()
        {
            var commonUi = Singleton<CommonUI>.Instance;
            var commonViews = commonUi != null
                ? commonUi.GetComponentsInChildren<ItemView>(false)
                : Array.Empty<ItemView>();

            var menuUi = Singleton<MenuUI>.Instance;
            var menuViews = menuUi != null
                ? menuUi.GetComponentsInChildren<ItemView>(false)
                : Array.Empty<ItemView>();

            if (menuViews.Length == 0)
            {
                return commonViews;
            }

            if (commonViews.Length == 0)
            {
                return menuViews;
            }

            var combined = new ItemView[commonViews.Length + menuViews.Length];
            commonViews.CopyTo(combined, 0);
            menuViews.CopyTo(combined, commonViews.Length);
            return combined;
        }

        private bool FitsAny(List<InvContainer> acceptors, Item item)
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
                catch (Exception e)
                {
                    // A container that throws is skipped, but log it once per run so a
                    // game update that breaks CheckCompatibility is visible.
                    if (!_loggedCompatibilityError)
                    {
                        _loggedCompatibilityError = true;
                        Plugin.LOG.LogWarning($"CheckCompatibility threw for {item}; skipping (further occurrences suppressed): {e}");
                    }
                }
            }

            return false;
        }

        private void ClearHighlight()
        {
            _sessionApplied = false;

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
