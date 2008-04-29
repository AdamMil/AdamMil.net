using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("AdamMil.net Test Helpers")]
[assembly: AssemblyDescription("Helpers for implementing unit tests.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyProduct("AdamMil.net")]
[assembly: AssemblyCopyright("Copyright © Adam Milazzo 2007-2008")]

[assembly: ComVisible(false)]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
