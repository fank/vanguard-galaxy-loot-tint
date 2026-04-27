using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace VGLootTint;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("VanguardGalaxy.exe")]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "vglootint";
    public const string PluginName = "Vanguard Galaxy Loot Tint";
    // BepInEx parses PluginVersion through System.Version which rejects SemVer
    // pre-release suffixes, so stick to the plain N.N.N form.
    public const string PluginVersion = "0.1.0";

    internal static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;

    private Harmony _harmony = null!;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(Patches.PickupNotificationPatches));

        Log.LogInfo($"{PluginName} v{PluginVersion} loaded ({_harmony.GetPatchedMethods().Count()} patches)");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
