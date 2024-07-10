using System.Text;
using System.Runtime.InteropServices;

namespace DirectXShaderCompiler.NET;


public static class NativeStringUtility
{
    private static IntPtr AllocBytePtr(byte[] bytes, out uint len)
    {
        len = (uint)bytes.Length;

        IntPtr nativePtr = Marshal.AllocHGlobal((int)len);
        Marshal.Copy(bytes, 0, nativePtr, (int)len);

        return nativePtr;
    }


    private static string Sanitize(string str, bool nullTerminate)
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


    public static IntPtr GetASCIIPtr(string str, out uint len, bool nullTerminate = true) => 
        AllocBytePtr(GetASCIIBytes(str, nullTerminate), out len);

    public static IntPtr GetUTF8Ptr(string str, out uint len, bool nullTerminate = true) => 
        AllocBytePtr(GetUTF8Bytes(str, nullTerminate), out len);

    public static IntPtr GetUTF16Ptr(string str, out uint len, bool nullTerminate = true, bool bigEndian = false) => 
        AllocBytePtr(GetUTF16Bytes(str, nullTerminate, bigEndian), out len);

    public static IntPtr GetUTF32Ptr(string str, out uint len, bool nullTerminate = true) => 
        AllocBytePtr(GetUTF32Bytes(str, nullTerminate), out len);
}