This directory should contain the library dependencies required to build this RimWorld mod.

Besides the .NET Core libraries this solution has dependencies on the following libraries:

- From RimWorld core Game:
    - Assembly-CSharp (RimWorld game logic)
    - UnityEngine (Unity Game Engine)
- Third Party:
    - [Harmony](https://github.com/pardeike/Harmony) by [@pardeike](https://github.com/pardeike)
        - since 1.1 it is a distributed as a mod, see: https://github.com/pardeike/HarmonyRimWorld
    - [HugsLib](https://github.com/UnlimitedHugs/RimworldHugsLib) by [@UnlimitedHugs](https://github.com/UnlimitedHugs)
    
List of libraries used:

- 0Harmony.dll
- Assembly-CSharp.dll
- Assembly-CSharp-firstpass.dll (optional)
- HugsLib.dll
- UnityEngine.dll
- [new in 1.1] UnityEngine.CoreModule.dll
- [new in 1.1] UnityEngine.IMGUIModule.dll
- [new in 1.1] UnityEngine.InputLegacyModule.dll
- [new in 1.1] UnityEngine.TextRenderingModule.dll


The `[new in 1.1]` is due to the changes in the Unity engine where the latter has been split in different modules.

Use the `packages.config` file to install the necessary packages for both `Harmony` and `Hugslib`.