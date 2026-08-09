# CompatibilityHighlighter

A BepInEx mod for SPT that highlights compatible items on **hover** — without clicking or dragging.

Point at ammo and compatible magazines light up. Point at a magazine and the guns that take it light up. Point at a gun and its mags, ammo, and attachments light up. Containers (backpacks, rigs, cases) are excluded by default.

## Configuration

`BepInEx\config\com.awnova.compatibilityhighlighter.cfg`, or via F12 in-game.

| Setting | Default | Purpose |
| --- | --- | --- |
| `Enabled` | `true` | Master toggle. |
| `Hover Delay` | `0.15` | Delay before highlight appears (seconds). |
| `Suppress During Drag` | `true` | Use vanilla highlight when dragging an item. |
| `Include Containers` | `false` | Highlight backpacks, rigs, cases, and stackable items. |
| `Compatible Color` | Green | Color for highlighting compatible items. |


## Build

Set `SPTBaseDir` in `CompatibilityHighlighter.csproj` (default: `C:\SPT`), then build Release:

```
dotnet build -c Release -p:SPTBaseDir=C:\SPT
```

Output: `Build\BepInEx\plugins\CompatibilityHighlighter.dll` → drop into SPT

## Credits

Inspired by [AC's - Attachment Compatibility Highlight](https://forge.sp-tarkov.com/mod/2823/acs-attachment-compatibility-highlight).
