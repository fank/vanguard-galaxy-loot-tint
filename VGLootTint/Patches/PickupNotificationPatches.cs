using System;
using System.Collections.Generic;
using Behaviour.Item;
using Behaviour.UI;
using HarmonyLib;
using Source.Item;
using Source.Util;

namespace VGLootTint.Patches;

[HarmonyPatch(typeof(FloatingInfoText), nameof(FloatingInfoText.Show))]
public static class PickupNotificationPatches
{
    // Cache: item displayName (translation key) → rarity. Built lazily on first
    // miss; InventoryItemType.allItems is loaded once at game startup, so the
    // cache stays valid for the session. Multiple displayNames mapping to the
    // same key would be unusual; in that case the first one wins.
    private static Dictionary<string, Rarity>? _rarityByDisplayName;

    public static void Postfix(FloatingInfoText __instance)
    {
        try
        {
            if (__instance.type != InfoType.PICKUP) return;

            string postfix = __instance.postfix;
            if (string.IsNullOrEmpty(postfix)) return;

            if (!TryResolveRarity(postfix, out Rarity rarity)) return;

            // Standard rarity stays vanilla (whatever color SetAttributesForType
            // already assigned, typically xpColor) — only tint Enhanced+ so the
            // common case is byte-identical to vanilla and rarer drops stand out.
            if (rarity == Rarity.Standard) return;

            __instance.numberText.color = rarity.GetColor();
        }
        catch (Exception e)
        {
            Plugin.Log.LogError($"VGLootTint: {e}");
        }
    }

    private static bool TryResolveRarity(string displayName, out Rarity rarity)
    {
        var cache = _rarityByDisplayName ?? BuildCache();
        return cache.TryGetValue(displayName, out rarity);
    }

    private static Dictionary<string, Rarity> BuildCache()
    {
        var cache = new Dictionary<string, Rarity>(StringComparer.Ordinal);
        // InventoryItemType.allItems is keyed by identifier; we need lookup by
        // displayName because that's what AbstractUnitData passes to
        // ShowPickupText(item.displayName, count).
        if (InventoryItemType.allItems != null)
        {
            foreach (var kvp in InventoryItemType.allItems)
            {
                var item = kvp.Value;
                if (item == null || string.IsNullOrEmpty(item.displayName)) continue;
                // First write wins on the rare chance two items share a displayName.
                if (!cache.ContainsKey(item.displayName))
                {
                    cache[item.displayName] = item.rarity;
                }
            }
        }
        _rarityByDisplayName = cache;
        return cache;
    }
}
