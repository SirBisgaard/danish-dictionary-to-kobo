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

    // Builder functions
    [DllImport("marisa", EntryPoint = "marisa_builder_new", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateBuilder();

    [DllImport("marisa", EntryPoint = "marisa_builder_push", CallingConvention = CallingConvention.Cdecl)]
    public static extern int PushIntoBuilder(IntPtr builder, [In] byte[] key, int length);

    [DllImport("marisa", EntryPoint = "marisa_builder_build", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildBuilder(IntPtr builder);

    [DllImport("marisa", EntryPoint = "marisa_builder_destroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestroyBuilder(IntPtr builder);

    // Trie functions
    [DllImport("marisa", EntryPoint = "marisa_trie_save", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SaveTrie(IntPtr trie, [MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport("marisa", EntryPoint = "marisa_trie_load", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr LoadTrie([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport("marisa", EntryPoint = "marisa_trie_num_keys", CallingConvention = CallingConvention.Cdecl)]
    public static extern UIntPtr GetNumKeys(IntPtr trie);

    [DllImport("marisa", EntryPoint = "marisa_trie_lookup", CallingConvention = CallingConvention.Cdecl)]
    public static extern int LookupKey(IntPtr trie, IntPtr agent);

    [DllImport("marisa", EntryPoint = "marisa_trie_reverse_lookup", CallingConvention = CallingConvention.Cdecl)]
    public static extern int ReverseLookup(IntPtr trie, IntPtr agent, UIntPtr keyId);

    [DllImport("marisa", EntryPoint = "marisa_trie_destroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestroyTrie(IntPtr trie);

    // Agent functions
    [DllImport("marisa", EntryPoint = "marisa_agent_new", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateAgent();

    [DllImport("marisa", EntryPoint = "marisa_agent_set_query", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetAgentQuery(IntPtr agent, [In] byte[] key, int length);

    [DllImport("marisa", EntryPoint = "marisa_agent_get_key", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetAgentKey(IntPtr agent, [Out] byte[] buffer, int bufferSize, out int outLength);

    [DllImport("marisa", EntryPoint = "marisa_agent_destroy", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestroyAgent(IntPtr agent);
}