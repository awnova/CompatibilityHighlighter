using System.Reflection;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine.EventSystems;

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

        // OnBeginDrag fires for any press-and-move, but the game only starts a drag when
        // CanDrag allows it. Mirroring that check keeps the driver from flagging a drag that
        // never happens (and whose OnEndDrag some subclasses never forward to the base).
        [PatchPrefix]
        private static void Prefix(ItemView __instance, PointerEventData eventData)
        {
            if (__instance.CanDrag(eventData))
            {
                HoverHighlightDriver.Instance?.OnDragBegin();
            }
        }
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
