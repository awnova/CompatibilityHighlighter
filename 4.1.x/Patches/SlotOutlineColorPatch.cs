using System;
using System.Reflection;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;

namespace CompatibilityHighlighter.Patches
{
    // SlotView paints the outline around equipment and mod slots itself: when the previewed
    // item fits, it sets its border images to its own static "valid" color. SlotView does not
    // derive from GridView, so nothing aimed at the grid highlight panel reaches these borders.
    internal sealed class SlotOutlineColorPatch : ModulePatch
    {
        // The borders are public, so they are bound at compile time; only the static
        // valid-move color is private.
        private static readonly FieldInfo ValidMoveColorField =
            AccessTools.Field(typeof(SlotView), "_validMoveColor")
            ?? throw new MissingFieldException(nameof(SlotView), "_validMoveColor");

        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(SlotView), nameof(SlotView.HighlightItemViewPosition));

        [PatchPostfix]
        private static void Postfix(SlotView __instance, bool preview)
        {
            if (!Plugin.RecolorSlotOutline.Value || !preview)
            {
                return;
            }

            var driver = HoverHighlightDriver.Instance;
            if (driver == null || !driver.SessionActive)
            {
                return;
            }

            var validColor = (Color)ValidMoveColorField.GetValue(null);
            var color = Plugin.CompatibleColor.Value;
            color.a = validColor.a;

            Recolor(__instance._emptyBorder, validColor, color);
            Recolor(__instance.FullBorder, validColor, color);
            Recolor(__instance._selectedBorder, validColor, color);
        }

        // Only repaint a border the game just painted with its own compatible color, so the
        // slot's idle and error states keep the colors the game chose for them.
        private static void Recolor(Image border, Color validColor, Color color)
        {
            if (border != null && border.color == validColor)
            {
                border.color = color;
            }
        }
    }
}
