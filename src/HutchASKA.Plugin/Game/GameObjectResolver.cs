extern alias UnityCore;
using NativeObject = UnityCore::UnityEngine.Object;
using HarmonyLib;
using HutchASKA.Core.Compatibility;
using SSSGame;
using SSSGame.Network;

namespace HutchASKA.Plugin.Game;

internal static class GameObjectResolver
{
    private static readonly List<Action> invalidators = new();
    private static readonly Harmony harmony = new(Plugin.PluginGuid + ".discovery");

    internal static void Initialize(Action<string> reportWarning)
    {
        Register<NetworkSession>(reportWarning);
        Register<PlayerManager>(reportWarning);
        Register<InputManager>(reportWarning);
        Register<PopulationManager>(reportWarning);
        Register<Settlement>(reportWarning);
    }

    private static void Register<T>(Action<string> reportWarning) where T : Component
    {
        var awake = AccessTools.DeclaredMethod(typeof(T), "Awake", Type.EmptyTypes);
        var destroy = AccessTools.DeclaredMethod(typeof(T), "OnDestroy", Type.EmptyTypes);
        if (awake is null || destroy is null)
        {
            reportWarning($"Discovery cache unavailable for {typeof(T).Name}: lifecycle hooks missing; using live searches.");
            return;
        }
        try
        {
            var invalidate = new HarmonyMethod(typeof(GameObjectResolver), nameof(InvalidateAll));
            harmony.Patch(awake, prefix: invalidate);
            harmony.Patch(destroy, prefix: invalidate);
        }
        catch (Exception error)
        {
            // A partial invalidation hook is harmless; never enable caching unless both hooks install.
            reportWarning($"Discovery cache unavailable for {typeof(T).Name}; using live searches. {error}");
            return;
        }
        var index = new LiveObjectIndex<T>(DiscoverIncludingInactive<T>, candidate => candidate && candidate.gameObject.activeInHierarchy);
        Cache<T>.Index = index;
        invalidators.Add(index.Invalidate);
    }

    public static void InvalidateAll()
    {
        foreach (var invalidate in invalidators) invalidate();
    }

    private static T[] DiscoverIncludingInactive<T>() where T : NativeObject
    {
        var native = NativeObject.FindObjectsOfType<T>(true);
        var result = new T[native.Length];
        for (var i = 0; i < result.Length; i++) result[i] = native[i];
        return result;
    }

    private static class Cache<T> where T : NativeObject
    {
        internal static LiveObjectIndex<T>? Index;
    }

    internal static T? FindUnique<T>() where T : NativeObject
    {
        // The cached set includes inactive objects. Native liveness and active state are read on
        // every lookup; Awake/OnDestroy invalidate all sets before a manager can be replaced.
        if (Cache<T>.Index is { } index) return index.FindUnique();
        var matches = NativeObject.FindObjectsOfType<T>();
        return matches.Length == 1 && matches[0] ? matches[0] : null;
    }
}
