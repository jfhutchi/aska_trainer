# Third-party notices

Verified against the installed assembly metadata, package metadata and the upstream license files on 2026-09-14. HutchASKA-authored code is MIT, copyright 2026 jfhutchi. Dependencies keep their own terms; HutchASKA's license does not replace them.

## Runtime dependencies obtained separately

| Dependency used | Version inspected | License and source |
| --- | --- | --- |
| BepInEx Core, Unity.IL2CPP and Unity.Common | 6.0.0-be.755, commit 3fab71a1914132a1ce3a545caf3192da603f2258 | [LGPL 2.1 license at inspected commit](https://github.com/BepInEx/BepInEx/blob/3fab71a1914132a1ce3a545caf3192da603f2258/LICENSE); [source](https://github.com/BepInEx/BepInEx/tree/3fab71a1914132a1ce3a545caf3192da603f2258) |
| HarmonyX (0Harmony.dll / HarmonyLib) | 2.10.2 | [MIT license and copyright notices](https://github.com/BepInEx/HarmonyX/blob/v2.10.2/LICENSE); [source](https://github.com/BepInEx/HarmonyX/tree/v2.10.2) |
| Il2CppInterop Runtime and Common | 1.5.1-ci.829, commit 6d9007c18cc8440830379c5e1d5714085e7ec577 | [LGPL 3 license, including incorporated GPL text](https://github.com/BepInEx/Il2CppInterop/blob/6d9007c18cc8440830379c5e1d5714085e7ec577/LICENSE); [source](https://github.com/BepInEx/Il2CppInterop/tree/6d9007c18cc8440830379c5e1d5714085e7ec577) |
| .NET runtime | ASKA loader 6.0.7; local core tests 6.0.36 | MIT: [6.0.7 license](https://github.com/dotnet/runtime/blob/v6.0.7/LICENSE.TXT), [6.0.36 license](https://github.com/dotnet/runtime/blob/v6.0.36/LICENSE.TXT) |

These libraries are dynamically referenced, not merged into the HutchASKA DLLs and not included in its ZIP. Obtain the matching runtime distribution separately. Their license texts are also included under `licenses/`. HutchASKA's source and build scripts allow rebuilding against compatible modified libraries; no restriction on modification, replacement or debugging of those libraries is imposed here. Upstream projects supply their corresponding source and additional notices.

## Build and test tools (not shipped in the plugin ZIP)

| Tool/package used | Version | License verification |
| --- | --- | --- |
| .NET SDK | 8.0.423 | [MIT](https://github.com/dotnet/sdk/blob/v8.0.423/LICENSE.TXT) |
| Microsoft.NET.Test.Sdk | 17.8.0 | [MIT](https://github.com/microsoft/vstest/blob/v17.8.0/LICENSE); local NuGet LICENSE_MIT.txt declaration |
| xUnit | 2.5.3 | [Apache 2.0 with its listed third-party notices](https://github.com/xunit/xunit/blob/v2-2.5.3/license.txt); local NuGet Apache-2.0 declaration |
| xunit.runner.visualstudio | 2.5.3 | [Apache 2.0 and included MIT notices](https://github.com/xunit/visualstudio.xunit/blob/2.5.3/License.txt); local NuGet Apache-2.0 declaration |
| coverlet.collector | 6.0.0 | [MIT](https://github.com/coverlet-coverage/coverlet/blob/v6.0.0/LICENSE); local NuGet MIT declaration |

## ASKA, Unity and other game-owned references

ASKA, its assets and trademarks belong to their respective owners. Unity and Photon/Fusion components supplied with ASKA keep their respective proprietary licenses. Generated Assembly-CSharp, SandSailorStudio, Unity, Fusion and Il2Cppmscorlib assemblies are local reference inputs obtained from the user's installed game. They are not HutchASKA-authored or covered by its MIT license. They must never be copied into the public source tree, committed or redistributed in the release.

HutchASKA is not affiliated with Sand Sailor Studio, Thunderful, Valve, Steam or WeMod. No WeMod code is used. No game assets, save data, account data, BepInEx configuration or third-party runtime DLLs are packaged.
