using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace CompatibilityHighlighter.Patches
{
    internal sealed class ContainerSuppressionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            AccessTools.Method(typeof(GridItemView), nameof(GridItemView.CheckAcceptHandler));

        [PatchPostfix]
        private static void Postfix(GridItemView __instance)
        {
            if (Plugin.IncludeContainers.Value)
            {
                return;
            }

            var driver = HoverHighlightDriver.Instance;
            if (driver == null || !driver.SessionActive)
            {
                return;
            }

            if (!ViewFields.HighlightedGlobally(__instance))
            {
                return;
            }

            var item = __instance.Item;
            if (item == null)
            {
                return;
            }

            var isContainer = item is CompoundItem compound && compound.Grids.Length > 0;
            var isMergeTarget = item.StackMaxSize > 1;

            if (isContainer || isMergeTarget)
            {
                ViewFields.ClearHighlight(__instance);
            }
        }
    }
}
