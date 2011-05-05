using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("AdamMil.net Transactions")]
[assembly: AssemblyDescription("A library that simplifies the creation of transactional software.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyProduct("AdamMil.net")]
[assembly: AssemblyCopyright("Copyright © Adam Milazzo 2011")]

[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
