namespace Coremats;

internal class Zstd
{
    public static byte[] Decompress(ReadOnlySpan<byte> compressed, int uncompressedLength)
    {
        var uncompressed = new byte[uncompressedLength];
        var decompressor = new ZstdSharp.Decompressor();
        decompressor.Unwrap(compressed, uncompressed);
        return uncompressed;
    }

    public static ReadOnlySpan<byte> Compress(byte[] uncompressed, int level)
    {
        using var compressor = new ZstdSharp.Compressor(level);
        compressor.SetParameter(ZstdSharp.Unsafe.ZSTD_cParameter.ZSTD_c_contentSizeFlag, 0);
        compressor.SetParameter(ZstdSharp.Unsafe.ZSTD_cParameter.ZSTD_c_windowLog, 16);
        compressor.SetParameter(ZstdSharp.Unsafe.ZSTD_cParameter.ZSTD_c_nbWorkers, Environment.ProcessorCount);
        return compressor.Wrap(uncompressed);
    }
}
