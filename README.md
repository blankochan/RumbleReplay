
# Playing a Replay

Currently the only way to render replay files is Blender, i intend to make a Godot version with support for Realtime tournament play


## Blender Setup

> [!Note]
> I do not provide any rigs or maps for you, you'll have to find/make one yourself

Prerequisites
* The [Structures](replace-with-a-working-link-when-i-have-one) Blend file (you could use your own file if its formatted right but this one has offsets and scales tuned for `RumbleReplay`)
* A Replay 
* And [The Plugin](BlenderPlugin/blenderplugin.py?raw=true)
---
1. Create a new Blend file
2. In the Top Left corner

![Step 2](https://github.com/user-attachments/assets/110a556e-5e66-4261-9ec6-317d9b3c1414)

4.  Click **Link** or **Append** (either one works i recommend **link** so when you make a change to the material its reflected across all replays)
5. Select The `Structures.blend` file i supply (or your own if you decided to make your own)
If you have a map this is when I suggest you link it in
1. Go over to Collections and select Structures
	Now we have to add the plugin, my plugin is added on a per blend file basis
6. Move over to the scripting tab

![Step 6](https://github.com/user-attachments/assets/7ecca53a-6ac6-4f85-9336-5d09f2c7a941)

7. Click open and select the `blenderplugin.py` file I give you
8. Press the little play button
9. Open the Sidebar menu (it opens itself like 50% of the time)
10. Move over to Misc and Press `Setup Scene`
11. If you want to put a camera on a players view its easier todo it now by adding a camera and parenting it to Player.001 or Player.002 (yes i know it starts at 000, but its inconsistently on my system so the code starts animating with 001 through however many players are in the file)
12. Press Select File and select your replay (I recommend copying it and putting it in the same folder as your blend file)
13. And then finally Generate Animation, blender might freeze for awhile but it should generate a animation that you're free todo what you want with
# Obtaining a Replay

After you install the mod there should be a new folder in your Melonloader UserData folder called `RumbleReplay`

Whenever you start a match it'll automatically create a new `.RR` file
With the filename structure `{localPlayerName}-Vs-{remotePlayerName} On {map}-{RandomizedName}.rr`

If you want to find the newest file you can right click in explorer and click `Sort By` and then `Date modified`

# Building
## Project Setup

### Create a `References` folder in your project directory

- Copy `RumbleModdingAPI.dll` ( you can get it from its Thunderstore page) to `References`

- Copy `Il2Cppmscorlib.dll` from `RUMBLE/MelonLoader/Il2CppAssemblies` to `References`

- Copy `Il2CppInterop.Runtime.dll` from `RUMBLE/MelonLoader/Il2CppAssemblies` to `References`

- Copy `Il2CppRUMBLE.Runtime.dll` from `RUMBLE/MelonLoader/Il2CppAssemblies` to `References`

- Copy `Il2CppBucketheadEntertainment.Plugins.dll` from `RUMBLE/MelonLoader/Il2CppAssemblies` to `References`

- Copy `UnityEngine.dll` from `RUMBLE/MelonLoader/Il2CppAssemblies` to `References`

- Copy `UnityEngine.CoreModule.dll` from `RUMBLE/MelonLoader/Il2CppAssemblies` to `References`

- Copy `UnityEngine.PhysicsModule.dll` from `RUMBLE/MelonLoader/Il2CppAssemblies` to `References`

- Copy `UnityEngine.InputLegacyModule.dll` from `RUMBLE/MelonLoader/Il2CppAssemblies` to `References`

- Restore NuGet packages if needed
### Add That folder as a reference in your editor (or just select all of them and reference them)


### For those who wanna make their own recorders/parsers
I have included an ImHex pattern that defines the structure of `.rr` files 
