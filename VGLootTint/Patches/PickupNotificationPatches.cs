using System;
using System.Collections;
using System.Collections.Generic;
using Behaviour.Item;
using Behaviour.UI;
using HarmonyLib;
using Source.Item;
using Source.Util;
using TMPro;
using UnityEngine;

namespace VGLootTint.Patches;

[HarmonyPatch(typeof(FloatingInfoText), nameof(FloatingInfoText.Show))]
public static class PickupNotificationPatches
{
    // The publicized stub lies about access on these — the runtime DLL keeps
    // FloatingInfoText.numberText private ([SerializeField]) and likely keeps
    // InventoryItemType.allItems private too. Direct compile-time access throws
    // FieldAccessException under Mono. Reach them via AccessTools (cached
    // delegate, hot-path safe) once at static init.
    private static readonly AccessTools.FieldRef<FloatingInfoText, TextMeshPro> _numberTextRef =
        AccessTools.FieldRefAccess<FloatingInfoText, TextMeshPro>("numberText");

    // Cache: item displayName (translation key) → rarity. Built lazily on first
    // miss. allItems is loaded once at game startup, so the cache is valid for
    // the session. Multiple displayNames mapping to the same key is unusual; in
    // that case the first wins.
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

            var text = _numberTextRef(__instance);
            if (text == null) return;
            text.color = rarity.GetColor();
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
        // allItems is a static field — public on the publicized stub but
        // possibly private at runtime. Pull it via reflection to be safe; this
        // runs once per session.
        var allItemsRaw = AccessTools.Field(typeof(InventoryItemType), "allItems")?.GetValue(null);
        if (allItemsRaw is IDictionary dict)
        {
            foreach (DictionaryEntry entry in dict)
            {
                if (entry.Value is not InventoryItemType item) continue;
                if (string.IsNullOrEmpty(item.displayName)) continue;
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
