using System.Reflection;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace CompatibilityHighlighter.Patches
{
    internal sealed class ItemViewHoverEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(ItemView), nameof(ItemView.OnPointerEnter));

        [PatchPostfix]
        private static void Postfix(ItemView __instance) =>
            HoverHighlightDriver.Instance?.OnHoverEnter(__instance);
    }

    internal sealed class ItemViewHoverExitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(ItemView), nameof(ItemView.OnPointerExit));

        [PatchPostfix]
        private static void Postfix(ItemView __instance) =>
            HoverHighlightDriver.Instance?.OnHoverExit(__instance);
    }

    internal sealed class ItemViewDragBeginPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(ItemView), nameof(ItemView.OnBeginDrag));

        [PatchPrefix]
        private static void Prefix() =>
            HoverHighlightDriver.Instance?.OnDragBegin();
    }

    internal sealed class ItemViewDragEndPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(ItemView), nameof(ItemView.OnEndDrag));

        [PatchPostfix]
        private static void Postfix() =>
            HoverHighlightDriver.Instance?.OnDragEnd();
    }
}
