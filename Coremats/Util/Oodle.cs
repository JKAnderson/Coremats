using static Noodle.Oodle2_9.OodleLZ;

namespace Coremats;

internal static class Oodle
{
    public static byte[] Compress(byte[] source, OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level)
    {
        OodleLZ_CompressOptions options = OodleLZ_CompressOptions_GetDefault(compressor, level);
        // Required for the game to not crash
        options.seekChunkReset = true;
        // This is already the default but I am including it for authenticity to game code
        options.seekChunkLen = 0x40000;

        long compressedBufferSizeNeeded = OodleLZ_GetCompressedBufferSizeNeeded(compressor, (nint)source.LongLength);
        byte[] compBuf = new byte[compressedBufferSizeNeeded];
        long compLen = OodleLZ_Compress(compressor, source, (nint)source.LongLength, compBuf, level, options);
        Array.Resize(ref compBuf, (int)compLen);
        return compBuf;
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
