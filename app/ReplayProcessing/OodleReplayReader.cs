using System;
using FortniteReplayReader;
using Microsoft.Extensions.Logging;
using Unreal.Core;
using Unreal.Core.Models.Enums;
using Unreal.Encryption;

namespace FortniteReplayAnalyzer.ReplayProcessing
{
    /// <summary>
    /// A <see cref="ReplayReader"/> that decompresses replay chunks with the native Oodle
    /// library (which supports every Oodle codec) and only falls back to the bundled
    /// managed decoder (Mermaid-only) when the native library is unavailable.
    /// </summary>
    public class OodleReplayReader : ReplayReader
    {
        public OodleReplayReader(ILogger logger = null, ParseMode parseMode = ParseMode.Minimal)
            : base(logger, parseMode)
        {
        }

        protected override FArchive Decompress(FArchive archive)
        {
            if (!Replay.Info.IsCompressed)
            {
                return archive;
            }

            var decompressedSize = archive.ReadInt32();
            var compressedSize = archive.ReadInt32();
            var compressedBuffer = archive.ReadBytes(compressedSize);

            ReadOnlyMemory<byte> output;
            if (!NativeOodle.TryDecompress(compressedBuffer, decompressedSize, out output, _logger))
            {
                // Managed fallback: only handles Mermaid-compressed chunks.
                output = Oodle.DecompressReplayData(compressedBuffer, decompressedSize);
            }

            return new Unreal.Core.BinaryReader(output)
            {
                EngineNetworkVersion = archive.EngineNetworkVersion,
                NetworkVersion = archive.NetworkVersion,
                ReplayHeaderFlags = archive.ReplayHeaderFlags,
                ReplayVersion = archive.ReplayVersion
            };
        }
    }
}
