using System.Reflection;
using System.Runtime.InteropServices;
using RumbleReplay;
using MelonLoader;

// The namespace of your mod class

// ...
[assembly: MelonInfo(typeof(RumbleReplayModClass), "RumbleReplay", "1.1.0", "blank")]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]
[assembly: MelonAdditionalDependencies("RumbleModdingAPI")]
[assembly: MelonColor(255, 255, 170, 238)] // #FAE pink :3
// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("RumbleReplay")]
[assembly: AssemblyDescription("Generates Replay Files for use in blender or other supported programs")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("RumbleReplay")]
[assembly: AssemblyCopyright("Copyright ©  2025")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("7eebd5a9-aa24-4565-9bca-aad698946213")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
