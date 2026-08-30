using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace FortniteReplayAnalyzer.ReplayProcessing
{
    /// <summary>
    /// Decompresses Oodle-compressed replay chunks using the native Oodle library
    /// (<c>liboodle-data-shared.so</c> / <c>oodle-data-shared.dll</c>).
    ///
    /// The managed decoder bundled with FortniteReplayReader (OozSharp) only implements
    /// the Mermaid codec, so replays compressed with any other Oodle codec (Kraken,
    /// Selkie, Leviathan, ...) fail with "Decoder type ... not supported". The native
    /// library understands every codec, so we prefer it and fall back to the managed
    /// decoder only when the native library cannot be loaded.
    /// </summary>
    public static class NativeOodle
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate long OodleLZ_DecompressDelegate(
            byte* compBuf, long compBufSize, byte* rawBuf, long rawLen,
            int fuzzSafe, int checkCrc, int verbosity,
            byte* decBufBase, long decBufSize,
            void* fpCallback, void* callbackUserData,
            byte* decoderMemory, long decoderMemorySize, int threadPhase);

        // Epic's redistributable Oodle build (same source CUE4Parse uses). Oodle 2.9.16.
        private const string ReleaseUrl = "https://github.com/WorkingRobot/OodleUE/releases/download/2026-06-04-1357";
        private const string LinuxZip = "gcc-x64-release.zip";
        private const string WindowsZip = "clang-cl-x64-release.zip";
        private const string LinuxLib = "liboodle-data-shared.so";
        private const string WindowsLib = "oodle-data-shared.dll";

        private static readonly object Gate = new();
        private static bool _initialized;
        private static OodleLZ_DecompressDelegate _decompress;

        public static bool IsAvailable => EnsureLoaded(null);

        /// <summary>
        /// Attempts to decompress <paramref name="compressed"/> to exactly
        /// <paramref name="decompressedSize"/> bytes using native Oodle.
        /// Returns false (without throwing) when native Oodle is unavailable or fails,
        /// so the caller can fall back to the managed decoder.
        /// </summary>
        public static unsafe bool TryDecompress(ReadOnlySpan<byte> compressed, int decompressedSize, out ReadOnlyMemory<byte> output, ILogger logger = null)
        {
            output = default;

            if (!EnsureLoaded(logger) || compressed.Length == 0 || decompressedSize <= 0)
            {
                return false;
            }

            try
            {
                // Fuzz-safe decoding needs a small safe zone past the end of the output, and for
                // multi-block streams Oodle must know the decode window (decBufBase/decBufSize).
                const int safeZone = 64;
                var raw = new byte[decompressedSize + safeZone];
                long written;
                fixed (byte* compPtr = compressed)
                fixed (byte* rawPtr = raw)
                {
                    // fuzzSafe = Yes(1), checkCRC = No(0), verbosity = None(0), threadPhase = Unthreaded(3).
                    written = _decompress(compPtr, compressed.Length, rawPtr, decompressedSize,
                        1, 0, 0, rawPtr, raw.Length, null, null, null, 0, 3);
                }

                if (written != decompressedSize)
                {
                    logger?.LogWarning("Native Oodle returned {Written} bytes, expected {Expected}; falling back.", written, decompressedSize);
                    return false;
                }

                output = new ReadOnlyMemory<byte>(raw, 0, decompressedSize);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Native Oodle decompression threw; falling back to the managed decoder.");
                return false;
            }
        }

        private static bool EnsureLoaded(ILogger logger)
        {
            if (_initialized)
            {
                return _decompress != null;
            }

            lock (Gate)
            {
                if (_initialized)
                {
                    return _decompress != null;
                }

                try
                {
                    var libraryPath = ResolveLibraryPath(logger);
                    if (libraryPath != null && NativeLibrary.TryLoad(libraryPath, out var handle)
                        && NativeLibrary.TryGetExport(handle, "OodleLZ_Decompress", out var export))
                    {
                        _decompress = Marshal.GetDelegateForFunctionPointer<OodleLZ_DecompressDelegate>(export);
                        logger?.LogInformation("Loaded native Oodle library from {Path}.", libraryPath);
                    }
                    else
                    {
                        logger?.LogWarning("Native Oodle library could not be loaded; using the managed Mermaid decoder only.");
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to initialize native Oodle; using the managed Mermaid decoder only.");
                }
                finally
                {
                    _initialized = true;
                }

                return _decompress != null;
            }
        }

        private static string ResolveLibraryPath(ILogger logger)
        {
            var libName = OperatingSystem.IsWindows() ? WindowsLib : LinuxLib;

            // 1. Explicit override.
            var overridePath = Environment.GetEnvironmentVariable("OODLE_LIBRARY_PATH");
            if (!string.IsNullOrWhiteSpace(overridePath) && File.Exists(overridePath))
            {
                return overridePath;
            }

            // 2. Alongside the application / working directory.
            foreach (var dir in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                var candidate = Path.Combine(dir, libName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            // 3. Cached download.
            var cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FortniteReplayAnalyzer", "oodle");
            var cachedLib = Path.Combine(cacheDir, libName);
            if (File.Exists(cachedLib))
            {
                return cachedLib;
            }

            if (string.Equals(Environment.GetEnvironmentVariable("OODLE_DISABLE_DOWNLOAD"), "1", StringComparison.Ordinal))
            {
                return null;
            }

            return DownloadLibrary(cacheDir, libName, logger);
        }

        private static string DownloadLibrary(string cacheDir, string libName, ILogger logger)
        {
            try
            {
                Directory.CreateDirectory(cacheDir);
                var zipName = OperatingSystem.IsWindows() ? WindowsZip : LinuxZip;
                var url = $"{ReleaseUrl}/{zipName}";
                var entryName = OperatingSystem.IsWindows() ? $"bin/{libName}" : $"lib/{libName}";

                logger?.LogInformation("Downloading native Oodle library from {Url}.", url);

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
                using var response = client.GetAsync(url).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var zipBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                using var zipStream = new MemoryStream(zipBytes);
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

                var entry = archive.GetEntry(entryName)
                    ?? throw new FileNotFoundException($"Entry '{entryName}' not found in Oodle release archive.");

                var destination = Path.Combine(cacheDir, libName);
                using (var entryStream = entry.Open())
                using (var fileStream = File.Create(destination))
                {
                    entryStream.CopyTo(fileStream);
                }

                logger?.LogInformation("Cached native Oodle library at {Path}.", destination);
                return destination;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Could not download the native Oodle library.");
                return null;
            }
        }
    }
}
