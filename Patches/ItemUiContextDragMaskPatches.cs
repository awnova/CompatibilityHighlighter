using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace CompatibilityHighlighter.Patches
{
    // ItemUiContext.RegisterView(ItemContextClass) also sets the engine's own private
    // itemContextClass field, which other mods (e.g. UIFixes) read to detect "an item is
    // currently being dragged" and can suppress their hotkeys. The hover preview
    // registers an ItemContextClass purely to reuse the game's native compatibility
    // highlighting, not because anything is actually being dragged, so these two patches
    // hide that field from everyone else for the duration of our own registration only.
    internal sealed class ItemUiContextRegisterViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.RegisterView));

        [PatchPostfix]
        private static void Postfix(ItemContextAbstractClass itemContext, ref ItemContextClass ___itemContextClass)
        {
            if (ReferenceEquals(itemContext, HoverHighlightDriver.Instance?.ForwardContext))
            {
                ___itemContextClass = null;
            }
        }
    }

    internal sealed class ItemUiContextUnregisterViewPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.UnregisterView));

        [PatchPrefix]
        private static void Prefix(ItemContextAbstractClass itemContext, ref ItemContextClass ___itemContextClass)
        {
            if (ReferenceEquals(itemContext, HoverHighlightDriver.Instance?.ForwardContext))
            {
                ___itemContextClass = (ItemContextClass)itemContext;
            }
        }
    }
}
