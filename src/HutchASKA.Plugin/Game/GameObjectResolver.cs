extern alias UnityCore;
using NativeObject = UnityCore::UnityEngine.Object;

namespace HutchASKA.Plugin.Game;

internal static class GameObjectResolver
{
    // Resolve each time: cached interop wrappers can outlive the scene's native object.
    internal static T? FindUnique<T>() where T : NativeObject
    {
        var matches = NativeObject.FindObjectsOfType<T>();
        return matches.Length == 1 && matches[0] ? matches[0] : null;
    }
}
