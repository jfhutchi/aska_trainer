extern alias UnityCore;
using NativeObject = UnityCore::UnityEngine.Object;
using HutchASKA.Core.Features;
using HutchASKA.Plugin.Game;
using HutchASKA.Plugin.Infrastructure;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using SSSGame;
using SSSGame.InputContext;

namespace HutchASKA.Plugin.Input;

internal sealed class MenuInputFeature() : NativeFeature("ui.input", "Trainer Gameplay Input Suppression")
{
    private Context? ownedContext;
    private int? managerIdentity;
    public override CompatibilityResult ProbeCompatibility() =>
        typeof(InputManager).GetMethod("AddContext", new[] { typeof(Context) }) is not null &&
        typeof(InputManager).GetMethod("RemoveContext", new[] { typeof(Context) }) is not null
        ? CompatibilityResult.Compatible() : CompatibilityResult.Incompatible("Native input context API is unavailable.");
    public override void Tick()
    {
        var manager = GameObjectResolver.FindUnique<InputManager>();
        if (!manager) return;
        if (managerIdentity.HasValue && managerIdentity != manager!.GetInstanceID())
            throw new InvalidOperationException("Input manager changed while trainer was open.");
        if (ownedContext is not null) return;
        ownedContext = ScriptableObject.CreateInstance<Context>();
        ownedContext.inputMaps = new Il2CppStructArray<InputManager.InputMaps>(0);
        ownedContext.priority = int.MaxValue;
        ownedContext.additive = false;
        ownedContext.dontChangeCamera = true;
        ownedContext.keepCameraFov = true;
        managerIdentity = manager!.GetInstanceID();
        manager.AddContext(ownedContext);
    }
    public override void Disable()
    {
        if (ownedContext is not null)
        {
            var manager = GameObjectResolver.FindUnique<InputManager>();
            if (manager && manager!.GetInstanceID() == managerIdentity) manager.RemoveContext(ownedContext);
            NativeObject.Destroy(ownedContext);
            ownedContext = null;
            managerIdentity = null;
        }
        base.Disable();
    }
}
