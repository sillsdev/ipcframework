# IPCFramework

This project provides a simple C# interprocess communication mechanism that
works on both Windows/.Net and Linux/Mono.  The Windows side is implemented
using the WCF NetNamedPipe feature.  The Linux side is implemented with Unix
Domain Sockets.

## Build

On Windows or Linux, build the solution `IPCFramework.sln` in Visual Studio/
VSCode/Rider, or use the command line:

```bash
msbuild /t:restore,build IPCFramework.sln
```
