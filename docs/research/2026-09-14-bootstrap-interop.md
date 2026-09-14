# Bootstrap interop observations

Inspected locally using Mono.Cecil on 2026-09-14. No runtime or game binaries are copied into this repository.

- `BepInEx.Unity.IL2CPP.BasePlugin.AddComponent<T>()` forwards through `IL2CPPChainloader.AddUnityComponent<T>()` to `Il2CppUtils.AddComponent(Type)`. That helper registers the type with `ClassInjector.RegisterTypeInIl2Cpp(Type)` when needed, then attaches it to the `BepInEx_Manager` GameObject.
- `TrainerBehaviour` supplies the generated `MonoBehaviour(IntPtr)` constructor and uses `[HideFromIl2Cpp]` on its managed initialization method. Its initialization explicitly calls `DontDestroyOnLoad(gameObject)`.
- `GUI.WindowFunction` supports implicit conversion from `System.Action<int>`. `GUI.Window(int, Rect, WindowFunction, string)` and `GUI.DragWindow(Rect)` are available. GUILayout methods provide managed `GUILayoutOption[]` convenience overloads; the toolbar accepts `Il2CppStringArray`.
- Cursor lock state and visibility have read/write accessors. `Application.version` and `Application.unityVersion` are available. Steam build ID is not inferred from the application version.
- Generated `UnityEngine.CoreModule.dll` exports public `System.Runtime.CompilerServices.NullableAttribute` and `NullableContextAttribute` types without the constructors required by the C# compiler. A globally visible reference causes CS0656 when nullable plugin source is compiled. The project assigns this reference the `UnityCore` alias and imports `UnityCore::UnityEngine` in `UnityImports.cs`, preserving nullable analysis without generated metadata collisions.

Gameplay modules should register through `FeatureHost.Register` and use the returned `HostedFeature.State` for patch guards. The hosted wrapper owns gate/fault state independently of the inner feature. Native disable operations must restore only that feature's changes. `SinglePlayerGuard.Decision` is a cached, nonthrowing Unknown decision in this stage; later session discovery must contain adapter errors before publishing the decision.

These observations establish member availability and compilation only. Runtime smoke testing is tracked separately in `docs/testing/stage-1-smoke-test.md`.
