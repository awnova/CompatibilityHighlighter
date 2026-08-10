using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace CompatibilityHighlighter.Patches
{
    // ItemUiContext.RegisterView(ItemContext) also sets the engine's own private
    // _dragItemContext field, which other mods (e.g. UIFixes) read to detect "an item is
    // currently being dragged" and can suppress their hotkeys. The hover preview
    // registers a DragItemContext purely to reuse the game's native compatibility
    // highlighting, not because anything is actually being dragged, so these two patches
    // hide that field from everyone else for the duration of our own registration only.
    //
    // RegisterView sets _dragItemContext and runs CheckCompatibleItems() before returning,
    // and UnregisterView only clears it when it still equals the context being removed, so
    // nulling in the postfix and restoring in the prefix leaves the game's own bookkeeping
    // intact.
    internal sealed class ItemUiContextRegisterViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.RegisterView));

        [PatchPostfix]
        private static void Postfix(ItemContext itemContext, ref DragItemContext ____dragItemContext)
        {
            if (ReferenceEquals(itemContext, HoverHighlightDriver.Instance?.ForwardContext))
            {
                ____dragItemContext = null;
            }
        }
    }

    internal sealed class ItemUiContextUnregisterViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.UnregisterView));

        [PatchPrefix]
        private static void Prefix(ItemContext itemContext, ref DragItemContext ____dragItemContext)
        {
            if (ReferenceEquals(itemContext, HoverHighlightDriver.Instance?.ForwardContext))
            {
                ____dragItemContext = (DragItemContext)itemContext;
            }
        }
    }
}
