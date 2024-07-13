using System.Runtime.InteropServices;

namespace DirectXShaderCompiler.NET;

// Don't want to document this, so it's internal. This won't be used for shader compilation anyway, just to make loading the library easier.
internal struct PlatformInfo
{
    internal OSPlatform platform;
    internal Architecture architecture;

    internal PlatformInfo(OSPlatform platform, Architecture architecture)
    {
        this.platform = platform;
        this.architecture = architecture;
    }

    internal static OSPlatform GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) 
            return OSPlatform.Linux;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            return OSPlatform.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            return OSPlatform.FreeBSD;
        throw new Exception("Cannot determine operating system.");
    }

    internal static PlatformInfo GetCurrentPlatform()
    {
        return new PlatformInfo(GetPlatform(), RuntimeInformation.ProcessArchitecture);
    }

    public override readonly string ToString()
    {
        return $"({platform}, {architecture})";
    }
}