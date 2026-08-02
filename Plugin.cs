using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CompatibilityHighlighter.Patches;
using SPT.Reflection.Patching;
using UnityEngine;

namespace CompatibilityHighlighter
{
    [BepInPlugin("com.awnova.compatibilityhighlighter", "CompatibilityHighlighter", "1.2.0")]
    [BepInProcess("EscapeFromTarkov.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource LOG;

        private static int _failedPatches;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> HoverDelay;
        internal static ConfigEntry<bool> SuppressDuringDrag;
        internal static ConfigEntry<bool> IncludeContainers;
        internal static ConfigEntry<Color> CompatibleColor;
        internal static ConfigEntry<bool> RecolorSlotOutline;

        private void Awake()
        {
            LOG = Logger;

            BindConfig();

            gameObject.AddComponent<HoverHighlightDriver>();
            gameObject.AddComponent<CellBorderOverlay>();

            EnablePatchSafely(new ItemViewHoverEnterPatch(), "hover enter");
            EnablePatchSafely(new ItemViewHoverExitPatch(), "hover exit");
            EnablePatchSafely(new ItemViewDragBeginPatch(), "drag begin");
            EnablePatchSafely(new ItemViewDragEndPatch(), "drag end");
            EnablePatchSafely(new ContainerSuppressionPatch(), "container suppression");
            EnablePatchSafely(new HighlightColorPatch(), "highlight color");
            EnablePatchSafely(new SlotOutlineColorPatch(), "slot outline");
            EnablePatchSafely(new ItemUiContextRegisterViewPatch(), "ItemUiContext register");
            EnablePatchSafely(new ItemUiContextUnregisterViewPatch(), "ItemUiContext unregister");

            if (_failedPatches == 0)
            {
                LOG.LogInfo("CompatibilityHighlighter loaded.");
            }
            else
            {
                LOG.LogError($"CompatibilityHighlighter loaded, but {_failedPatches} patch(es) failed to apply. " +
                             "The game version is most likely newer than this build - see the errors above.");
            }
        }

        private static void EnablePatchSafely(ModulePatch patch, string name)
        {
            try
            {
                patch.Enable();
                LOG.LogInfo($"Enabled {name} patch.");
            }
            catch (System.Exception ex)
            {
                _failedPatches++;
                LOG.LogError($"Failed to enable {name} patch: {ex}");
            }
        }

        private void BindConfig()
        {
            Enabled = Config.Bind(
                "General", "Enabled", true,
                "Highlight compatible items while hovering them in the inventory.");

            HoverDelay = Config.Bind(
                "General", "Hover Delay", 0.1f,
                new ConfigDescription(
                    "Seconds the cursor must rest on an item before the highlight appears. Keeps a quick sweep across the stash from strobing.",
                    new AcceptableValueRange<float>(0f, 1f)));

            SuppressDuringDrag = Config.Bind(
                "General", "Suppress During Drag", true,
                "While an item is actually picked up (click and hold), stand down and let the base game's own drag highlight do its thing. Turn off to keep the mod's own highlight, in your chosen color, for the whole drag: it freezes on the item you picked up rather than re-targeting cells you drag across.");

            IncludeContainers = Config.Bind(
                "General", "Include Containers", false,
                "Also highlight storage containers (backpacks, rigs, cases) and stacks the hovered item could merge into. Off by default: this mod is about what attaches to what, not about where an item physically fits.");

            CompatibleColor = Config.Bind(
                "General", "Compatible Color", new Color(0.06f, 0.38f, 0.06f),
                "Color used to highlight compatible items. Defaults to the same green EFT itself uses for a valid drag-and-drop placement (GridView.ValidMoveColor).");

            RecolorSlotOutline = Config.Bind(
                "General", "Recolor Equipment Slots", false,
                "Recolor the game's native compatible-slot outline (shown on equipment/mod slots while the mod's hover preview is active) to match Compatible Color. Off keeps the game's default green outline.");
        }
    }
}
