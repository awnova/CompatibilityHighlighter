using EFT.UI.DragAndDrop;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CompatibilityHighlighter
{
    internal static class ViewFields
    {
        internal const float HighlightAlpha = 50f / 255f;

        internal static readonly AccessTools.FieldRef<ItemView, Color> SelectedColor =
            AccessTools.FieldRefAccess<ItemView, Color>("SelectedColor");

        internal static readonly AccessTools.FieldRef<ItemView, bool> HighlightedGlobally =
            AccessTools.FieldRefAccess<ItemView, bool>("HighlightedGlobally");

        internal static readonly AccessTools.FieldRef<ItemView, Image> ColorPanel =
            AccessTools.FieldRefAccess<ItemView, Image>("ColorPanel");

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
