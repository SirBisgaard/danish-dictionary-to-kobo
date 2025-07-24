using System.Reflection;
using System.Runtime.InteropServices;

namespace Ddtk.Cli.Helpers;

/// <summary>
/// Huge thanks to the marisa-trie project for providing the native library.
/// https://github.com/s-yata/marisa-trie
/// The marisa-trie library is a fast and memory-efficient implementation of a trie data structure.
/// The native file is a build on version 0.3.1 of the marisa-trie library.
/// </summary>
public static class MarisaNative
{
    public static void BindMarisa()
    {
        var asm = Assembly.GetExecutingAssembly();
        var path = Path.Combine(AppContext.BaseDirectory, "libmarisa.so");

        if (!File.Exists(path))
        {
            var resourceName = asm.GetManifestResourceNames().FirstOrDefault(r => r.Contains("libmarisa")) ?? "Ddtk.Cli.runtimes.linux-x64.native.libmarisa.so";
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new DllNotFoundException("Missing embedded dll: libmarisa.so");
            }

            path = Path.Combine(Path.GetTempPath(), "libmarisa.so");
            using var fs = File.OpenWrite(path);
            stream.CopyTo(fs);
        }

        NativeLibrary.Load(path);
    }

    [DllImport("marisa", EntryPoint = "marisa_builder_new", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateBuilder();

    [DllImport("marisa", EntryPoint = "marisa_builder_push", CallingConvention = CallingConvention.Cdecl)]
    public static extern int PushIntoBuilder(IntPtr builder, [In] byte[] key, int length);

    [DllImport("marisa", EntryPoint = "marisa_builder_build", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildBuilder(IntPtr builder);

    [DllImport("marisa", EntryPoint = "marisa_builder_destroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestroyBuilder(IntPtr builder);

    [DllImport("marisa", EntryPoint = "marisa_trie_save", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SaveTrie(IntPtr trie, [MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport("marisa", EntryPoint = "marisa_trie_destroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestroyTrie(IntPtr trie);
}