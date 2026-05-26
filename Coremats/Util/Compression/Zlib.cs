using System.IO.Compression;

namespace Coremats;

internal static class Zlib
{
    public static byte[] Decompress(BexReader br, int compressedLength)
    {
        // It's a bit tragic to copy all this out, but ZLibStream unavoidably (as far as I know)
        // reads all the way to the end of the stream, which seems like a bad idea for various reasons
        byte[] input = br.ReadBytes(compressedLength);
        using var inStream = new MemoryStream(input);
        using var outStream = new MemoryStream();
        using (var zs = new ZLibStream(inStream, CompressionMode.Decompress))
        {
            zs.CopyTo(outStream);
        }
        return outStream.ToArray();
    }

    public static byte[] Decompress(BexReader br, int compressedLength, int uncompressedLength)
    {
        byte[] input = br.ReadBytes(compressedLength);
        var output = new byte[uncompressedLength];
        using var inStream = new MemoryStream(input);
        using var zs = new ZLibStream(inStream, CompressionMode.Decompress);
        zs.ReadExactly(output);
        return output;
    }

    public static void Decompress(BexReader br, int compressedLength, Span<byte> output)
    {
        byte[] input = br.ReadBytes(compressedLength);
        using var inStream = new MemoryStream(input);
        using var zs = new ZLibStream(inStream, CompressionMode.Decompress);
        zs.ReadExactly(output);
    }

    public static int Compress(BexWriter bw, byte[] uncompressed, int compressionLevel)
    {
        long start = bw.Position;
        var options = new ZLibCompressionOptions() { CompressionLevel = compressionLevel };
        using (var zs = new ZLibStream(bw.Stream, options, true))
        {
            zs.Write(uncompressed);
        }
        return (int)(bw.Position - start);
    }
}
