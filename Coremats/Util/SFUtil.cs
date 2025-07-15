using System.IO.Compression;

namespace Coremats;

internal static class SFUtil
{
    public static BinaryReaderEx GetDecompressedBR(BinaryReaderEx br, out DCX.Type compression)
    {
        if (DCX.Is(br))
        {
            byte[] bytes = DCX.Decompress(br, out compression);
            return new BinaryReaderEx(false, bytes);
        }
        else
        {
            compression = DCX.Type.None;
            return br;
        }
    }

    public static int WriteZlib(BinaryWriterEx bw, byte formatByte, byte[] input)
    {
        long start = bw.Position;
        bw.WriteByte(0x78);
        bw.WriteByte(formatByte);

        using (var deflateStream = new DeflateStream(bw.Stream, CompressionMode.Compress, true))
        {
            deflateStream.Write(input, 0, input.Length);
        }

        bw.WriteUInt32(Adler32(input));
        return (int)(bw.Position - start);
    }

    public static byte[] ReadZlib(BinaryReaderEx br, int compressedSize)
    {
        br.AssertByte(0x78);
        br.AssertByte(0x01, 0x5E, 0x9C, 0xDA);
        byte[] compressed = br.ReadBytes(compressedSize - 2);

        using (var decompressedStream = new MemoryStream())
        {
            using (var compressedStream = new MemoryStream(compressed))
            using (var deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, true))
            {
                deflateStream.CopyTo(decompressedStream);
            }
            return decompressedStream.ToArray();
        }
    }

    public static uint Adler32(byte[] data)
    {
        uint adlerA = 1;
        uint adlerB = 0;

        foreach (byte b in data)
        {
            adlerA = (adlerA + b) % 65521;
            adlerB = (adlerB + adlerA) % 65521;
        }

        return (adlerB << 16) | adlerA;
    }
}
