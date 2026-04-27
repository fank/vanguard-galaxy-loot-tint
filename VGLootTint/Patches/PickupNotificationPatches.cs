using System;
using Behaviour.Item;
using Behaviour.UI;
using HarmonyLib;
using Source.Data;
using Source.Item;
using Source.Util;
using TMPro;
using UnityEngine;

namespace VGLootTint.Patches;

// Two-stage patch:
//
//   1. AbstractUnitData.AddCargo runs with the actual InventoryItemType ref
//      (which carries the rarity). It then calls UIInfoTextParent.ShowPickupText
//      with item.displayName — collapsing the rich item ref down to a string
//      before the floating text is instantiated. So we PREFIX AddCargo to stash
//      the item's rarity in a static field.
//
//   2. FloatingInfoText.Show is the next ring out: it builds the actual UI row.
//      We POSTFIX it, read the stashed rarity, and tint the TMP text's color
//      for InfoType.PICKUP only.
//
// Stash → consume → clear, all on the Unity main thread, so a static field is
// safe. We clear in a finally on the AddCargo postfix in case the call chain
// short-circuits and Show never fires (e.g. SpaceStationInterior is open),
// otherwise the next pickup would inherit the previous item's rarity.
//
// We can't just look item.displayName up against InventoryItemType.allItems
// because equipment names like "Railgun Mk.VII" are computed dynamically from
// base + level + manufacturer; the displayName captured at static-init time
// doesn't match what the player actually sees at pickup.
public static class PickupNotificationPatches
{
    // Publicized stub exposes FloatingInfoText.numberText as a public field, but
    // it's [SerializeField] and private at runtime — direct access throws
    // FieldAccessException under Mono. Cached delegate reaches it without
    // hitting the per-call reflection cost.
    private static readonly AccessTools.FieldRef<FloatingInfoText, TextMeshPro> _numberTextRef =
        AccessTools.FieldRefAccess<FloatingInfoText, TextMeshPro>("numberText");

    private static Rarity? _pendingRarity;

    [HarmonyPatch(typeof(AbstractUnitData), nameof(AbstractUnitData.AddCargo))]
    public static class AddCargoPatch
    {
        public static void Prefix(InventoryItemType item)
        {
            try
            {
                _pendingRarity = item != null ? item.rarity : (Rarity?)null;
            }
            catch (Exception e)
            {
                _pendingRarity = null;
                Plugin.Log.LogError($"VGLootTint AddCargoPatch.Prefix: {e}");
            }
        }

        public static void Postfix()
        {
            // If the cargo add ran but the pickup notification never fired
            // (e.g. station interior, off-screen units), clear the stash so the
            // next genuine pickup doesn't inherit a stale rarity.
            _pendingRarity = null;
        }
    }

    [HarmonyPatch(typeof(FloatingInfoText), nameof(FloatingInfoText.Show))]
    public static class FloatingInfoTextShowPatch
    {
        public static void Postfix(FloatingInfoText __instance)
        {
            try
            {
                if (__instance.type != InfoType.PICKUP) return;
                if (_pendingRarity is not Rarity rarity) return;

                // Consume the stash regardless of branch so a non-tinted pickup
                // (Standard) doesn't bleed onto the next call.
                _pendingRarity = null;

                if (rarity == Rarity.Standard) return;

                var text = _numberTextRef(__instance);
                if (text == null) return;

                Color color = rarity.GetColor();
                text.color = color;
                // Show() already snapshotted numberText.color into textColor
                // BEFORE this postfix ran (see FloatingInfoText.Show body).
                // The Update fade loop drives numberText.color from textColor
                // every frame once life > lifetime - 0.75s — if we don't
                // overwrite textColor too, the fade resurrects the original
                // xpColor and our tint vanishes mid-animation.
                __instance.textColor = color;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"VGLootTint Show.Postfix: {e}");
            }
        }
    }
}
