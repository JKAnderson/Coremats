using System.Runtime.InteropServices;
using static Noodle.Oodle2_9.OodleLZ;

namespace Coremats;

internal static class Oodle
{
    public static byte[] Compress(byte[] source, OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level)
    {
        IntPtr pOptions = OodleLZ_CompressOptions_GetDefault(compressor, level);
        OodleLZ_CompressOptions options = Marshal.PtrToStructure<OodleLZ_CompressOptions>(pOptions);
        // Required for the game to not crash
        options.seekChunkReset = true;
        // This is already the default but I am including it for authenticity to game code
        options.seekChunkLen = 0x40000;
        pOptions = Marshal.AllocHGlobal(Marshal.SizeOf<OodleLZ_CompressOptions>());

        try
        {
            Marshal.StructureToPtr(options, pOptions, false);
            long compressedBufferSizeNeeded = OodleLZ_GetCompressedBufferSizeNeeded(compressor, (nint)source.LongLength);
            byte[] compBuf = new byte[compressedBufferSizeNeeded];
            long compLen = OodleLZ_Compress(compressor, source, (nint)source.LongLength, compBuf, level, pOptions);
            Array.Resize(ref compBuf, (int)compLen);
            return compBuf;
        }
        finally
        {
            Marshal.FreeHGlobal(pOptions);
        }
    }

    public static byte[] Decompress(byte[] source, long uncompressedSize)
    {
        long decodeBufferSize = OodleLZ_GetDecodeBufferSize(OodleLZ_Compressor.OodleLZ_Compressor_Invalid, (nint)uncompressedSize, true);
        byte[] rawBuf = new byte[decodeBufferSize];
        long rawLen = OodleLZ_Decompress(source, (nint)source.LongLength, rawBuf, (nint)uncompressedSize);
        Array.Resize(ref rawBuf, (int)rawLen);
        return rawBuf;
    }
}
