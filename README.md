= IPCFramework =

This project provides a simple C# interprocess communication mechanism that
works on both Windows/.Net and Linux/Mono.  The Windows side is implemented
using the WCF NetNamedPipe feature.  The Linux side is implemented with Unix
Domain Sockets.

== Build ==

In *Windows*, build solution IPCFramework.sln from Visual Studio.

In *Linux*, install package mono5-sil and run

    PATH="/opt/mono5-sil/bin:$PATH" xbuild /t:rebuild
