# Vanguard Galaxy Loot Tint (VGLootTint)

A BepInEx plugin for [Vanguard Galaxy](https://store.steampowered.com/app/3471800/) that tints the floating "+1 Item" pickup notification by item rarity. Standard items keep the vanilla colour; Enhanced and rarer drops light up in their rarity colour, so a Legendary salvage in the middle of a stream of common ore is impossible to miss.

- **Reads from the game's own rarity palette** — Enhanced / HighGrade / Exotic / Legendary use exactly the same colours the inventory tooltips and equipment cards use, via `Source.Util.RarityExtensions.GetColor`. No hardcoded hex values to drift from the game.
- **Standard rarity untouched** — the most common pickups still look identical to vanilla, so coloured popups are a real signal, not noise.
- **Pure cosmetic** — no balance changes, no game logic touched, no save data, no config file. One Harmony postfix on `FloatingInfoText.Show`.

## Install

1. **Install BepInEx 5.x** — grab `BepInEx_win_x64_5.4.x.zip` from the [BepInEx releases](https://github.com/BepInEx/BepInEx/releases) and unzip it into your Vanguard Galaxy install folder (next to `VanguardGalaxy.exe`).
2. **Launch the game once** so BepInEx creates its `BepInEx/plugins/` and `BepInEx/config/` subfolders, then close the game.
3. **Download the VGLootTint release** zip from [Releases](https://github.com/fank/vanguard-galaxy-loot-tint/releases).
4. **Unzip** into `BepInEx/plugins/`. The zip contains a single `VGLootTint/` folder that drops in cleanly:
   ```
   VanguardGalaxy/BepInEx/plugins/
     VGLootTint/
       VGLootTint.dll
       README.md
   ```
5. **Launch the game.** Open the BepInEx console — you should see a load line ending with the patch count, e.g.:
   ```
   [Info :Vanguard Galaxy Loot Tint] Vanguard Galaxy Loot Tint v0.1.0 loaded (1 patches)
   ```

## Uninstall

Delete the `BepInEx/plugins/VGLootTint/` folder. The plugin holds no config and no per-save state.

## Troubleshooting

**No load line in the BepInEx console**
- Check that `BepInEx/plugins/VGLootTint/VGLootTint.dll` exists.
- Enable the console: `BepInEx/config/BepInEx.cfg` → `[Logging.Console]` → `Enabled = true`.

**Plugin loads but pickups all stay vanilla white**
- That means every item picked up was Standard rarity (which is intentionally left untouched). Salvage a wreck or open a loot box and check Enhanced+ pickups light up.
- If even rare items stay white, check the BepInEx console for `VGLootTint:` errors — a game update may have renamed the type or method.

## How it works

The "+1 Item" popup is a `Behaviour.UI.FloatingInfoText` instance with `type = InfoType.PICKUP`. Vanilla `SetAttributesForType` paints it `xpColor` regardless of what was picked up. The plugin postfixes `FloatingInfoText.Show`: when the type is PICKUP, it looks up the postfix string (the item's `displayName`) against `InventoryItemType.allItems`, reads the matching `Rarity`, and (for Enhanced and above) overwrites `numberText.color` with `rarity.GetColor()`.

The lookup table is built lazily once and cached for the session — `InventoryItemType.allItems` is populated at game startup, so the cache stays valid.

## Build

The repo commits **publicized stubs** of three game-specific assemblies at `VGLootTint/lib/`:

- `Assembly-CSharp.dll` — for `FloatingInfoText`, `InfoType`, `InventoryItemType`, `Rarity`, `RarityExtensions`.
- `Unity.TextMeshPro.dll` — for `TextMeshPro` (the type of `FloatingInfoText.numberText`).
- `UnityEngine.UI.dll` — compile-time only; `TextMeshPro` inherits from `MaskableGraphic`, which lives in this assembly.

These are method-signature-only stubs (every IL body replaced with `throw null;` by `assembly-publicizer --strip`), legal to redistribute, and enough to compile against. The real runtime takes over in-game.

```bash
make build      # or: dotnet build VGLootTint/VGLootTint.csproj -c Debug
make deploy     # build + copy to BepInEx/plugins (WSL/Steam path; edit Makefile if yours differs)
```

To regenerate the stubs after a game update:

```bash
assembly-publicizer --strip <game>/VanguardGalaxy_Data/Managed/Assembly-CSharp.dll   -o VGLootTint/lib/Assembly-CSharp.dll
assembly-publicizer --strip <game>/VanguardGalaxy_Data/Managed/Unity.TextMeshPro.dll -o VGLootTint/lib/Unity.TextMeshPro.dll
assembly-publicizer --strip <game>/VanguardGalaxy_Data/Managed/UnityEngine.UI.dll    -o VGLootTint/lib/UnityEngine.UI.dll
```

`--strip` is required — without it, the committed DLLs would carry the proprietary IL bodies, which can't be redistributed.

## License

MIT — see [LICENSE](LICENSE).
