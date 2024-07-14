namespace DirectXShaderCompiler.NET;

public enum ShaderType : ushort
{
    Pixel, 
    Vertex,
    Geometry,
    Hull,
    Domain,
    Compute,
    Library,
    Mesh,
    Amplification,
}


public static class ShaderTypeExtensions
{  
    public static bool SupportsVersion(this ShaderType type, ushort version, ushort subVersion)
    {
        return type.MinimumVersion() <= (ushort)(version * 10 + subVersion);
    }


    // Sadly DXC only supports shader model 6.0+
    public static ushort MinimumVersion(this ShaderType type)
    {
        return type switch
        {
            ShaderType.Pixel => 60,
            ShaderType.Vertex => 60,
            ShaderType.Geometry => 60,
            ShaderType.Hull => 60,
            ShaderType.Domain => 60,
            ShaderType.Compute => 60,
            ShaderType.Library => 61,
            ShaderType.Mesh => 65,
            ShaderType.Amplification => 65,
            _ => throw new ArgumentException($"Invalid ShaderType: {type}"),
        };
    }


    public static string Abbreviation(this ShaderType type)
    {
        return type switch 
        {
            ShaderType.Vertex        => "vs",
            ShaderType.Pixel         => "ps",
            ShaderType.Domain        => "ds",
            ShaderType.Hull          => "hs",
            ShaderType.Mesh          => "ms",
            ShaderType.Amplification => "as",
            ShaderType.Library       => "lib",
            ShaderType.Geometry      => "gs",
            ShaderType.Compute       => "cs",
            _ => throw new ArgumentException($"Invalid ShaderType: {type}"),
        };
    }
}




public class ShaderProfile
{
    private ShaderType type;
    private ushort version = 6;
    private ushort subVersion = 0;


    public ShaderProfile(ShaderType type, int version = 6, int subVersion = 0)
    {
        this.type = type;
        this.version = (ushort)version;
        this.subVersion = (ushort)subVersion;
    }


    public bool IsValid() => type.SupportsVersion(version, subVersion);

    public void Validate() 
    {
        if (!IsValid())
        {
            float minVersion = (float)type.MinimumVersion() / 10;
            throw new InvalidProfileException($"{type} shader is not compatible with shader model {version}.{subVersion}. Shader model must be a minimum of {minVersion:0.0}");
        }
    }


    public ShaderType Type
    {
        get => type;
        set => type = value;
    }


    public int Version
    {
        get => version;
        set => version = (ushort)Math.Clamp(value, 6, 10);
    }


    public int SubVersion
    {
        get => subVersion;
        set => subVersion = (ushort)Math.Clamp(value, 0, 8);
    }


    public override string ToString() => $"{type.Abbreviation()}_{version}_{subVersion}";  
}


public class InvalidProfileException : Exception
{
    public InvalidProfileException() : base() { }

    public InvalidProfileException(string message) : base(message) { }

    public InvalidProfileException(string message, Exception innerException) : base(message, innerException) { }
}