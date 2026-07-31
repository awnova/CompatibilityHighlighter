using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CompatibilityHighlighter.Patches;
using UnityEngine;

namespace CompatibilityHighlighter
{
    [BepInPlugin("com.awnova.compatibilityhighlighter", "CompatibilityHighlighter", "1.0.0")]
    [BepInProcess("EscapeFromTarkov.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource LOG;

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> HoverDelay;
        internal static ConfigEntry<bool> SuppressDuringDrag;
        internal static ConfigEntry<bool> IncludeContainers;
        internal static ConfigEntry<Color> CompatibleColor;

        private void Awake()
        {
            LOG = Logger;

            BindConfig();

            gameObject.AddComponent<HoverHighlightDriver>();
            gameObject.AddComponent<CellBorderOverlay>();

            new ItemViewHoverEnterPatch().Enable();
            new ItemViewHoverExitPatch().Enable();
            new ItemViewDragBeginPatch().Enable();
            new ItemViewDragEndPatch().Enable();
            new ContainerSuppressionPatch().Enable();
            new HighlightColorPatch().Enable();

            LOG.LogInfo("CompatibilityHighlighter loaded.");
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
        }
    }
}
