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
        private static readonly AccessTools.FieldRef<SlotView, Image> EmptyBorder =
            AccessTools.FieldRefAccess<SlotView, Image>("_emptyBorder");

        private static readonly AccessTools.FieldRef<SlotView, Image> FullBorder =
            AccessTools.FieldRefAccess<SlotView, Image>("FullBorder");

        private static readonly AccessTools.FieldRef<SlotView, Image> SelectedBorder =
            AccessTools.FieldRefAccess<SlotView, Image>("_selectedBorder");

        private static readonly FieldInfo ValidColorField = AccessTools.Field(typeof(SlotView), "color_1");

        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(SlotView), "HighlightItemViewPosition");

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

            var validColor = (Color)ValidColorField.GetValue(null);
            var color = Plugin.CompatibleColor.Value;
            color.a = validColor.a;

            Recolor(EmptyBorder(__instance), validColor, color);
            Recolor(FullBorder(__instance), validColor, color);
            Recolor(SelectedBorder(__instance), validColor, color);
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
