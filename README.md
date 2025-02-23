

# Playing a Replay

Currently the only way to render replay files is Blender.
## Blender Setup

> [!important]
> I do not provide any rigs or maps for you, you'll have to find/make one yourself

Prerequisites

* The [Structures](https://github.com/blankochan/RumbleReplay/blob/master/BlenderPlugin/Structures.blend?raw=true) Blend file (you could use your own file if it's formatted right, but this one has offsets and scales tuned forRumbleReplay

* A [Replay](#obtaining-a-replay) 

* And [the Plugin](BlenderPlugin/blenderplugin.py?raw=true)

---

1. Create a new Blend file.<img align="right" src=https://github.com/user-attachments/assets/3080ba76-7dd9-4749-ab9a-9458fb7dd04f>

2. In the top left corner

3. Click Link or Append (either one works; I recommend Link so when you make a change to the material, it's reflected across all replays)

4. Select the [Structures.blend](https://github.com/blankochan/RumbleReplay/blob/master/BlenderPlugin/Structures.blend?raw=true) file I supply (or your own if you decide to make your own).

5. Go over to Collections and select Structures.

6. Move over to the scripting tab. 

7. Click Open and select the `blenderplugin.py` file I give you. <img align="right" src=https://github.com/user-attachments/assets/e6f09c80-d301-4672-b7bf-40d47890c650>

8. Press the little play button ![playbutton](https://github.com/user-attachments/assets/920b4c27-2d56-461a-bbd6-d8e1403d8cf6)

9. Open the Sidebar menu (it opens itself like 50% of the time).

10. Move over to Misc; and Press `Setup Scene` <img align="right" src="https://github.com/user-attachments/assets/ace6617d-8c1d-40ca-8d93-dafdcf6ae0b7">

> [!Tip]
>  If you want to put a camera on a players view its easier todo it now by adding a camera and parenting it to Player.000 or Player.001

11. Press `Select File` and select your replay (I recommend copying it and putting it in the same folder as your blend file).

12. And then finally generate animation; Blender might freeze for a while, but it should generate an animation that you're free to do what you want with.

# Obtaining a Replay

After you install the mod, there should be a new folder in your `Melonloader\UserData` folder called RumbleReplay

Whenever you start a match, it'll automatically create a new .RR file.

With the filename structure `{localPlayerName}-Vs-{remotePlayerName} On {map}-{RandomizedName}.rr`

> [!Tip]
> If you want to find the newest file you can right click in explorer and click Sort By and then Date modified

# Building

## Project Setup

### Create a References folder in your project directory

### From RUMBLE/MelonLoader/Il2CppAssemblies copy to:References

- `Il2Cppmscorlib.dll`

- `Il2CppInterop.Runtime.dll`

- `Il2CppRUMBLE.Runtime.dll`

- `Il2CppBucketheadEntertainment.Plugins.dll` 

- `UnityEngine.dll` 

- `UnityEngine.CoreModule.dll` 

- `UnityEngine.PhysicsModule.dll`

- `UnityEngine.InputLegacyModule.dll` 

### From `RUMBLE/MelonLoader/net6` 

* `Il2CppInterop.Common.dll`

* `Il2CppInterop.Generator.dll`

* `Il2CppInterop.Runtime.dll`

### Add RumbleModdingAPI.dll (obtainable via [Thunderstore](https://thunderstore.io/c/rumble/p/UlvakSkillz/RumbleModdingAPI/) if you don't have it) to references.

### Restore NuGet packages if needed

### Add That References folder as a References Folder in your editor (or just select all of them and reference them).

> [!NOTE]
> For those who wanna make their own recorders/parsers, or prepose changes or otherwise just understand the format
> I have included an [ImHex Pattern](https://raw.githubusercontent.com/blankochan/RumbleReplay/refs/heads/master/v2RRparser.hexpat) that defines the structure of .rr files 

