# DirectXShaderCompiler.NET: cross-platform C# wrapper for DirectXShaderCompiler 

A cross-platform .NET 8.0 wrapper for Microsoft's DirectX Shader Compiler, written in C#. 

[![NuGet](https://img.shields.io/nuget/v/DirectXShaderCompiler.NET.svg)](https://www.nuget.org/packages/DirectXShaderCompiler.NET)

# Usage

This project wraps functionality from DXC into a static DirectXShaderCompiler class, which can be used to easily compile shader code with various options, and get parsed and formatted outputs from the compilation process. For an example of how to compile a basic shader, refer to [Example.cs](Example/Example.cs). There is no proper documentation for this wrapper, but most functionality is reasonably well-documented with typical XML comments.

### Note on CompilerOptions
While CompilerOptions does a generally good job of passing in command-line arguments to DXC in the correct format and providing only working options, specific combinations of options can cause a segmentation fault in native code. It is best to test out combinations of more obscure or under-utilized options in an isolated environment beforehand, as they can potentially cause a program crash.

# Native Details
 
Due to the nature of the official DirectXShaderCompiler repository's build system, it is difficult to easily acquire or build binaries of DXC that meets the following criteria:
 
- Exposes a cross-platform c-style interface for DXC.
- Runs on all major desktop operating systems.
- Can be compiled for all major desktop operating systems from any system.
 
As such, DirectXShaderCompiler.NET uses a fork of [DXC, built with zig](https://github.com/sinnwrig/DirectXShaderCompiler-zig) instead of CMake. As Zig's compiler supports cross-compilation out of the box, it allows DXC to build easily from most desktop platforms, for most platforms. The libraries produced by building this repository are what DirectXShaderCompiler.NET uses in its releases.
 
## Building Native Libraries
 
To build native libraries, run the `BuildNative.cs` file inside the _Native_ folder, specicying your target architecture [x64, arm64, all] with -A and your target platform [windows, linux, macos, all] with -P.
 
Native build requirements:
- Zig compiler version present on your `PATH` of at least version 0.14.0+. You can get the compiler from [Zig's download page](https://ziglang.org/download/) or [from a package manager](https://github.com/ziglang/zig/wiki/Install-Zig-from-a-Package-Manager)
 
Pre-built binaries are bundled in the NuGet package for the following operating systems:
- Windows x64
- Windows arm64
- OSX x64
- OSX arm64 (Apple silicon)
- Linux x64
- Linux arm64
