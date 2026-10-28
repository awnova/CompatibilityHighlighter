using System;
using System.Runtime.CompilerServices;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using UnityEngine;

namespace CompatibilityHighlighter
{
    internal static class ViewFields
    {
        internal const float HighlightAlpha = 50f / 255f;

        internal static readonly AccessTools.FieldRef<ItemView, Color> SelectedColor =
            AccessTools.FieldRefAccess<ItemView, Color>("SelectedColor");

        internal static readonly AccessTools.FieldRef<ItemView, bool> HighlightedGlobally =
            AccessTools.FieldRefAccess<ItemView, bool>("HighlightedGlobally");

        // Calling this runs the FieldRefAccess lookups above (they throw if a field is
        // missing), so a rename surfaces at startup instead of on the first hover.
        internal static bool Validate()
        {
            try
            {
                RuntimeHelpers.RunClassConstructor(typeof(ViewFields).TypeHandle);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.LOG.LogError($"ItemView highlight fields could not be resolved: {ex}");
                return false;
            }
        }

        // False while the game still hides the item's identity: not yet searched in a
        // container, or searched but not yet examined.
        internal static bool IsRevealed(ItemView view) => view.IsSearched && view.Examined;

        internal static void ApplyHighlight(ItemView view, Color color)
        {
            color.a = HighlightAlpha;
            SelectedColor(view) = color;
            HighlightedGlobally(view) = true;
            view.UpdateColor();
        }

        internal static void ClearHighlight(ItemView view)
        {
            SelectedColor(view) = ItemView.DefaultSelectedColor;
            HighlightedGlobally(view) = false;
            view.UpdateColor();
        }
    }
}
