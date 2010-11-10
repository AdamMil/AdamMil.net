using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("GPG.net")]
[assembly: AssemblyDescription("A .NET library to interface with the GNU Privacy Guard.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCopyright("Copyright Adam Milazzo 2008-2010")]

[assembly: ComVisible(false)]

[assembly: AssemblyVersion("1.0.0.*")]
[assembly: AssemblyFileVersion("1.0.0.0")]
