using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace DirectXShaderCompiler.NET;


public static class NativeStringUtility
{
    private static unsafe byte* Alloc(byte[] bytes, out uint len)
    {
        len = (uint)bytes.Length;

        IntPtr nativePtr = Marshal.AllocHGlobal((int)len);
        Marshal.Copy(bytes, 0, nativePtr, (int)len);

        return (byte*)nativePtr;
    }

    private static GCHandle Pin(byte[] bytes, out uint len)
    {
        len = (uint)bytes.Length;
        return GCHandle.Alloc(bytes, GCHandleType.Pinned);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Sanitize(string str, bool nullTerminate)
    {
        if (!nullTerminate)
            return str;

        return str[^1] == '\0' ? str : str + '\0';
    }

    public static byte[] GetASCIIBytes(string str, bool nullTerminate = true)
    {
        return Encoding.ASCII.GetBytes(Sanitize(str, nullTerminate));
    }

    public static byte[] GetUTF8Bytes(string str, bool nullTerminate = true)
    {
        return Encoding.UTF8.GetBytes(Sanitize(str, nullTerminate));
    }

    public static byte[] GetUTF16Bytes(string str, bool nullTerminate = true, bool bigEndian = false)
    {
        str = Sanitize(str, nullTerminate);

        if (bigEndian)
            return Encoding.BigEndianUnicode.GetBytes(str);
        
        return Encoding.Unicode.GetBytes(str);
    }

    public static byte[] GetUTF32Bytes(string str, bool nullTerminate = true)
    {
        return Encoding.UTF32.GetBytes(Sanitize(str, nullTerminate));
    }

    public static unsafe byte* GetASCIIPtr(string str, out uint len, bool nullTerminate = true) => 
        Alloc(GetASCIIBytes(str, nullTerminate), out len);

    public static unsafe byte* GetUTF8Ptr(string str, out uint len, bool nullTerminate = true) => 
        Alloc(GetUTF8Bytes(str, nullTerminate), out len);

    public static unsafe byte* GetUTF16Ptr(string str, out uint len, bool nullTerminate = true, bool bigEndian = false) => 
        Alloc(GetUTF16Bytes(str, nullTerminate, bigEndian), out len);

    public static unsafe byte* GetUTF32Ptr(string str, out uint len, bool nullTerminate = true) => 
        Alloc(GetUTF32Bytes(str, nullTerminate), out len);

        
    public static GCHandle GetASCIIHandle(string str, out uint len, bool nullTerminate = true) => 
        Pin(GetASCIIBytes(str, nullTerminate), out len);

    public static GCHandle GetUTF8Handle(string str, out uint len, bool nullTerminate = true) => 
        Pin(GetUTF8Bytes(str, nullTerminate), out len);

    public static GCHandle GetUTF16Handle(string str, out uint len, bool nullTerminate = true, bool bigEndian = false) => 
        Pin(GetUTF16Bytes(str, nullTerminate, bigEndian), out len);

    public static GCHandle GetUTF32Handle(string str, out uint len, bool nullTerminate = true) => 
        Pin(GetUTF32Bytes(str, nullTerminate), out len);
}