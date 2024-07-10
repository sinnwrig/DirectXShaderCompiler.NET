namespace DirectXShaderCompiler.NET;


// The SafeHandle class in System.Runtime.InteropServices does something similar to this class, but implementing it was more annoying than making this simple wrapper.
public abstract class NativeResourceHandle : IDisposable
{
    protected IntPtr handle;

    protected abstract void ReleaseHandle();


    public void Dispose()
    {
        ReleaseHandle();
        GC.SuppressFinalize(this);
    }


    ~NativeResourceHandle()
    {
        ConsoleColor prev = Console.ForegroundColor;
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Warning: Native handle for {GetType().Name} was not properly deallocated. Ensure object is disposed by manually calling Dispose() or with a using statement.");
        Console.ForegroundColor = prev;

        Dispose();
    }
}