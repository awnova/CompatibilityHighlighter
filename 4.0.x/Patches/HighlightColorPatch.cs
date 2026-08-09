using System.Reflection;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace CompatibilityHighlighter.Patches
{
    internal sealed class HighlightColorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(ItemView), nameof(ItemView.UpdateColor));

        [PatchPostfix]
        private static void Postfix(ItemView __instance)
        {
            var driver = HoverHighlightDriver.Instance;
            if (driver == null || !driver.SessionActive)
            {
                return;
            }

            if (!ViewFields.HighlightedGlobally(__instance))
            {
                return;
            }

            var panel = ViewFields.ColorPanel(__instance);
            if (panel == null)
            {
                return;
            }

            var color = Plugin.CompatibleColor.Value;
            color.a = ViewFields.HighlightAlpha;
            panel.color = color;
        }
    }
}
