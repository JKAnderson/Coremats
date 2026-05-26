using System.IO.Compression;

namespace Coremats;

public static partial class DCX
{
    public static bool Is(BexReader br)
    {
        if (br.Stream.Length < 4)
            return false;

        string magic = br.PeekAscii(0, 4);
        return magic == "DCP\0" || magic == "DCX\0";
    }

    public static bool Is(byte[] bytes)
    {
        var br = new BexReader(bytes, true);
        return Is(br);
    }

    public static bool Is(string path)
    {
        using var fs = File.OpenRead(path);
        var br = new BexReader(fs, true);
        return Is(br);
    }

    #region Public Decompress
    public static byte[] Decompress(byte[] data, out Type type)
    {
        var br = new BexReader(data, true);
        return Decompress(br, out type);
    }

    public static byte[] Decompress(byte[] data)
    {
        return Decompress(data, out _);
    }

    public static byte[] Decompress(string path, out Type type)
    {
        using var fs = File.OpenRead(path);
        var br = new BexReader(fs, true);
        return Decompress(br, out type);
    }

    public static byte[] Decompress(string path)
    {
        return Decompress(path, out _);
    }
    #endregion

    public static byte[] Decompress(BexReader br, out Type type)
    {
        br.BigEndian = true;
        string magic = br.PeekAscii(0, 4);
        if (magic == "DCP\0")
        {
            string format = br.PeekAscii(4, 4);
            return format switch
            {
                "DFLT" => DecompressDcpDflt(br, out type),
                "EDGE" => DecompressDcpEdge(br, out type),
                _ => throw new NotImplementedException($"Unimplemented DCP compression format \"{format}\".")
            };
        }
        else if (magic == "DCX\0")
        {
            string format = br.PeekAscii(0x28, 4);
            return format switch
            {
                "DFLT" => DecompressDcxDflt(br, out type),
                "EDGE" => DecompressDcxEdge(br, out type),
                "KRAK" => DecompressDcxKrak(br, out type),
                "ZSTD" => DecompressDcxZstd(br, out type),
                _ => throw new NotImplementedException($"Unimplemented DCX compression format \"{format}\".")
            };
        }
        else
        {
            byte b0 = br.PeekByte(0);
            byte b1 = br.PeekByte(1);
            if (b0 == 0x78 && (b1 == 0x01 || b1 == 0x5E || b1 == 0x9C || b1 == 0xDA))
            {
                type = Type.Zlib;
                return Zlib.Decompress(br, (int)br.Length);
            }
            else
            {
                throw new InvalidDataException($"Could not determine compression format.");
            }
        }
    }

    private static byte[] ReadEdgeData(BexReader br, DcsStruct dcs, DcaStruct dca, long dataStart)
    {
        var decompressed = new byte[dcs.DataLengthUncompressed];
        for (int i = 0; i < dca.BlockInfo.Length; i++)
        {
            var blockInfo = dca.BlockInfo[i];
            int uncompressedOffset = 0x10000 * i;
            int uncompressedLength = Math.Min(0x10000, dcs.DataLengthUncompressed - uncompressedOffset);
            var output = decompressed.AsSpan(uncompressedOffset, uncompressedLength);

            if (blockInfo.Compressed)
            {
                byte[] block = br.PeekBytes(dataStart + blockInfo.DataOffset, blockInfo.DataLengthCompressed);
                using var inStream = new MemoryStream(block);
                using var ds = new DeflateStream(inStream, CompressionMode.Decompress);
                ds.ReadExactly(output);
            }
            else
            {
                br.PeekBytes(dataStart + blockInfo.DataOffset, output);
            }
        }
        return decompressed;
    }

    private static byte[] DecompressDcpDflt(BexReader br, out Type type)
    {
        var dcp = new DcpStruct(br);
        if (dcp.Algorithm != "DFLT") throw new InvalidDataException();
        if (dcp.Level != 9) throw new InvalidDataException();
        if (dcp.BlockSize != 0) throw new InvalidDataException();
        if (dcp.Header != 0) throw new InvalidDataException();
        if (dcp.AlignSize != 1) throw new InvalidDataException();

        var dcs = new DcsStruct(br);

        byte[] decompressed = Zlib.Decompress(br, dcs.DataLengthCompressed, dcs.DataLengthUncompressed);

        _ = new DcaStruct(br, dcp, false);

        type = Type.DCP_DFLT;
        return decompressed;
    }

    private static byte[] DecompressDcpEdge(BexReader br, out Type type)
    {
        var dcp = new DcpStruct(br);
        if (dcp.Algorithm != "EDGE") throw new InvalidDataException();
        if (dcp.Level != 9) throw new InvalidDataException();
        if (dcp.BlockSize != 0x10000) throw new InvalidDataException();
        if (dcp.Header != 0) throw new InvalidDataException();
        if (dcp.AlignSize != 0x10) throw new InvalidDataException();

        var dcs = new DcsStruct(br);

        br.AssertInt32(0);
        long dataStart = br.Position;
        br.Skip(dcs.DataLengthCompressed);

        var dca = new DcaStruct(br, dcp, false);

        type = Type.DCP_EDGE;
        return ReadEdgeData(br, dcs, dca, dataStart);
    }

    private static byte[] DecompressDcxDflt(BexReader br, out Type type)
    {
        var dcx = new DcxStruct(br);
        if (dcx.Unk04 != 0x10000 && dcx.Unk04 != 0x11000) throw new InvalidDataException();

        var dcs = new DcsStruct(br);

        var dcp = new DcpStruct(br);
        if (dcp.Algorithm != "DFLT") throw new InvalidDataException();
        if (dcp.Level != 8 && dcp.Level != 9) throw new InvalidDataException();
        if (dcp.BlockSize != 0) throw new InvalidDataException();
        if (dcp.Header != 0 && dcp.Header != 15) throw new InvalidDataException();
        if (dcp.AlignSize != 1) throw new InvalidDataException();

        _ = new DcaStruct(br, dcp, true);

        if (dcx.LegacyOffsets && dcx.Unk04 == 0x10000 && dcp.Level == 9 && dcp.Header == 0)
            type = Type.DCX_DFLT_10000_24_9;
        else if (!dcx.LegacyOffsets && dcx.Unk04 == 0x10000 && dcp.Level == 9 && dcp.Header == 0)
            type = Type.DCX_DFLT_10000_44_9;
        else if (!dcx.LegacyOffsets && dcx.Unk04 == 0x11000 && dcp.Level == 8 && dcp.Header == 0)
            type = Type.DCX_DFLT_11000_44_8;
        else if (!dcx.LegacyOffsets && dcx.Unk04 == 0x11000 && dcp.Level == 9 && dcp.Header == 0)
            type = Type.DCX_DFLT_11000_44_9;
        else if (!dcx.LegacyOffsets && dcx.Unk04 == 0x11000 && dcp.Level == 9 && dcp.Header == 15)
            type = Type.DCX_DFLT_11000_44_9_15;
        else
            throw new NotImplementedException($"Unimplemented DCX DFLT permutation.");

        return Zlib.Decompress(br, dcs.DataLengthCompressed, dcs.DataLengthUncompressed);
    }

    private static byte[] DecompressDcxEdge(BexReader br, out Type type)
    {
        var dcx = new DcxStruct(br);
        if (!dcx.LegacyOffsets) throw new InvalidDataException();
        if (dcx.Unk04 != 0x10000) throw new InvalidDataException();

        var dcs = new DcsStruct(br);

        var dcp = new DcpStruct(br);
        if (dcp.Algorithm != "EDGE") throw new InvalidDataException();
        if (dcp.Level != 9) throw new InvalidDataException();
        if (dcp.BlockSize != 0x10000) throw new InvalidDataException();
        if (dcp.Header != 0) throw new InvalidDataException();
        if (dcp.AlignSize != 0x10) throw new InvalidDataException();

        var dca = new DcaStruct(br, dcp, true);

        type = Type.DCX_EDGE;
        long dataStart = br.Position;
        return ReadEdgeData(br, dcs, dca, dataStart);
    }

    private static byte[] DecompressDcxKrak(BexReader br, out Type type)
    {
        var dcx = new DcxStruct(br);
        if (dcx.LegacyOffsets) throw new InvalidDataException();
        if (dcx.Unk04 != 0x11000) throw new InvalidDataException();

        var dcs = new DcsStruct(br);

        var dcp = new DcpStruct(br);
        if (dcp.Algorithm != "KRAK") throw new InvalidDataException();
        if (dcp.Level != 6 && dcp.Level != 9) throw new InvalidDataException();
        if (dcp.BlockSize != 0) throw new InvalidDataException();
        if (dcp.Header != 0) throw new InvalidDataException();
        if (dcp.AlignSize != 1) throw new InvalidDataException();

        _ = new DcaStruct(br, dcp, true);

        if (dcp.Level == 6)
            type = Type.DCX_KRAK_6;
        else if (dcp.Level == 9)
            type = Type.DCX_KRAK_9;
        else
            throw new NotImplementedException($"Unimplemented DCX KRAK permutation.");

        byte[] compressed = br.ReadBytes(dcs.DataLengthCompressed);
        return Oodle.Decompress(compressed, dcs.DataLengthUncompressed);
    }

    private static byte[] DecompressDcxZstd(BexReader br, out Type type)
    {
        var dcx = new DcxStruct(br);
        if (dcx.LegacyOffsets) throw new InvalidDataException();
        if (dcx.Unk04 != 0x11000) throw new InvalidDataException();

        var dcs = new DcsStruct(br);

        var dcp = new DcpStruct(br);
        if (dcp.Algorithm != "ZSTD") throw new InvalidDataException();
        if (dcp.Level != 21 && dcp.Level != 22) throw new InvalidDataException();
        if (dcp.BlockSize != 0) throw new InvalidDataException();
        if (dcp.Header != 0) throw new InvalidDataException();
        if (dcp.AlignSize != 1) throw new InvalidDataException();

        _ = new DcaStruct(br, dcp, true);

        if (dcp.Level == 21)
            type = Type.DCX_ZSTD_21;
        else if (dcp.Level == 22)
            type = Type.DCX_ZSTD_22;
        else
            throw new NotImplementedException($"Unimplemented DCX ZSTD permutation.");

        byte[] compressed = br.ReadBytes(dcs.DataLengthCompressed);
        return Zstd.Decompress(compressed, dcs.DataLengthUncompressed);
    }

    #region Public Compress
    public static byte[] Compress(byte[] data, Type type)
    {
        var bw = new BexWriter(true);
        Compress(data, bw, type);
        return bw.FinishBytes();
    }

    public static void Compress(byte[] data, Type type, string path)
    {
        using var fs = File.Create(path);
        var bw = new BexWriter(fs, true);
        Compress(data, bw, type);
        bw.Finish();
    }
    #endregion

    public static void Compress(byte[] data, BexWriter bw, Type type)
    {
        bw.BigEndian = true;
        if (type == Type.Zlib)
            Zlib.Compress(bw, data, -1);
        else if (type == Type.DCP_DFLT)
            CompressDcpDflt(data, bw);
        else if (type == Type.DCP_EDGE)
            CompressDcpEdge(data, bw);
        else if (type == Type.DCX_DFLT_10000_24_9
            || type == Type.DCX_DFLT_10000_44_9
            || type == Type.DCX_DFLT_11000_44_8
            || type == Type.DCX_DFLT_11000_44_9
            || type == Type.DCX_DFLT_11000_44_9_15)
            CompressDcxDflt(data, bw, type);
        else if (type == Type.DCX_EDGE)
            CompressDcxEdge(data, bw);
        else if (type == Type.DCX_KRAK_6
            || type == Type.DCX_KRAK_9)
            CompressDcxKrak(data, bw, type);
        else if (type == Type.DCX_ZSTD_21
            || type == Type.DCX_ZSTD_22)
            CompressDcxZstd(data, bw, type);
        else if (type == Type.Unknown)
            throw new ArgumentException("You cannot compress a DCX with an unknown type.");
        else
            throw new NotImplementedException("Compression for the given type is not implemented.");
    }

    private static DcaBlockInfo[] WriteEdgeData(BexWriter bw, DcpStruct dcp, byte[] data, int blockCount, long dataOffset)
    {
        var zlibOptions = new ZLibCompressionOptions() { CompressionLevel = dcp.Level };
        var blockInfo = new DcaBlockInfo[blockCount];
        for (int i = 0; i < blockCount; i++)
        {
            int blockOffsetUncompressed = i * 0x10000;
            int blockLengthUncompressed = Math.Min(0x10000, data.Length - blockOffsetUncompressed);

            var blockDataUncompressed = data.AsSpan(blockOffsetUncompressed, blockLengthUncompressed);
            byte[] blockDataCompressed;
            using (var ms = new MemoryStream())
            {
                using (var ds = new DeflateStream(ms, zlibOptions))
                {
                    ds.Write(blockDataUncompressed);
                }
                blockDataCompressed = ms.ToArray();
            }

            bool blockInfoCompressed;
            if (blockDataCompressed.Length < blockLengthUncompressed)
            {
                blockInfoCompressed = true;
            }
            else
            {
                blockInfoCompressed = false;
                blockDataCompressed = blockDataUncompressed.ToArray();
            }
            int blockInfoOffset = (int)(bw.Position - dataOffset);
            int blockInfoLength = blockDataCompressed.Length;

            blockInfo[i] = new(blockInfoOffset, blockInfoLength, blockInfoCompressed);
            bw.WriteBytes(blockDataCompressed);
            bw.Align(0x10);
        }
        return blockInfo;
    }

    private static void CompressDcpDflt(byte[] data, BexWriter bw)
    {
        var dcp = new DcpStruct("DFLT", 9, 0, 0, 1).Write(bw);

        var dcs = new DcsStruct(data.Length).Reserve(bw);

        dcs.DataLengthCompressed = Zlib.Compress(bw, data, dcp.Level);

        new DcaStruct().Write(bw, dcp, false);

        dcs.Fill(bw);
    }

    private static void CompressDcpEdge(byte[] data, BexWriter bw)
    {
        int blockCount = (int)Math.Ceiling((float)data.Length / 0x10000);

        var dcp = new DcpStruct("EDGE", 9, 0x10000, 0, 0x10).Write(bw);

        var dcs = new DcsStruct(data.Length).Reserve(bw);

        bw.WriteInt32(0);
        long dataOffset = bw.Position;
        var blockInfo = WriteEdgeData(bw, dcp, data, blockCount, dataOffset);
        dcs.DataLengthCompressed = (int)(bw.Position - dataOffset);

        new DcaStruct(blockInfo).Write(bw, dcp, false);

        dcs.Fill(bw);
    }

    private static void CompressDcxDflt(byte[] data, BexWriter bw, Type type)
    {
        bool dcxLegacyOffsets = type == Type.DCX_DFLT_10000_24_9;
        int dcxUnk04 = (type == Type.DCX_DFLT_10000_24_9 || type == Type.DCX_DFLT_10000_44_9) ? 0x10000 : 0x11000;
        byte dcpLevel = (byte)(type == Type.DCX_DFLT_11000_44_8 ? 8 : 9);
        byte dcpHeader = (byte)(type == Type.DCX_DFLT_11000_44_9_15 ? 15 : 0);

        var dcx = new DcxStruct(dcxLegacyOffsets, dcxUnk04).Reserve(bw);

        long dcsOffset = bw.Position;
        var dcs = new DcsStruct(data.Length).Reserve(bw);

        long dcpOffset = bw.Position;
        var dcp = new DcpStruct("DFLT", dcpLevel, 0, dcpHeader, 1).Write(bw);

        long dcaOffset = bw.Position;
        new DcaStruct().Write(bw, dcp, true);

        long dataOffset = bw.Position;
        dcs.DataLengthCompressed = Zlib.Compress(bw, data, dcp.Level);

        dcx.Fill(bw, (int)dcsOffset, (int)dcpOffset, (int)dcaOffset, (int)dataOffset);
        dcs.Fill(bw);
    }

    private static void CompressDcxEdge(byte[] data, BexWriter bw)
    {
        int trailingBlockLengthUncompressed = data.Length % 0x10000;
        if (trailingBlockLengthUncompressed == 0)
            trailingBlockLengthUncompressed = 0x10000;
        int blockCount = (int)Math.Ceiling((float)data.Length / 0x10000);

        var dcx = new DcxStruct(true, 0x10000).Reserve(bw);

        long dcsOffset = bw.Position;
        var dcs = new DcsStruct(data.Length).Reserve(bw);

        long dcpOffset = bw.Position;
        var dcp = new DcpStruct("EDGE", 9, 0x10000, 0, 0x10).Write(bw);

        long dcaOffset = bw.Position;
        var dca = new DcaStruct().Reserve(bw, dcp, true, blockCount);

        long dataOffset = bw.Position;
        var blockInfo = WriteEdgeData(bw, dcp, data, blockCount, dataOffset);
        dcs.DataLengthCompressed = (int)(bw.Position - dataOffset);
        dca.TrailingBlockLengthUncompressed = trailingBlockLengthUncompressed;
        dca.BlockInfo = blockInfo;

        dcx.Fill(bw, (int)dcsOffset, (int)dcpOffset, (int)dcaOffset, (int)dataOffset);
        dcs.Fill(bw);
        dca.Fill(bw);
    }

    private static void CompressDcxKrak(byte[] data, BexWriter bw, Type type)
    {
        byte dcpLevel = (byte)(type == Type.DCX_KRAK_6 ? 6 : 9);
        byte[] compressed = Oodle.Compress(data, Noodle.Oodle2_9.OodleLZ.OodleLZ_Compressor.OodleLZ_Compressor_Kraken, (Noodle.Oodle2_9.OodleLZ.OodleLZ_CompressionLevel)dcpLevel);

        var dcx = new DcxStruct(false, 0x11000).Reserve(bw);

        long dcsOffset = bw.Position;
        new DcsStruct(data.Length, compressed.Length).Write(bw);

        long dcpOffset = bw.Position;
        var dcp = new DcpStruct("KRAK", dcpLevel, 0, 0, 1).Write(bw);

        long dcaOffset = bw.Position;
        new DcaStruct().Write(bw, dcp, true);

        long dataOffset = bw.Position;
        bw.WriteBytes(compressed);
        bw.Align(0x10);

        dcx.Fill(bw, (int)dcsOffset, (int)dcpOffset, (int)dcaOffset, (int)dataOffset);
    }

    private static void CompressDcxZstd(byte[] data, BexWriter bw, Type type)
    {
        byte dcpLevel = (byte)(type == Type.DCX_ZSTD_21 ? 21 : 22);
        byte[] compressed = Zstd.Compress(data, dcpLevel).ToArray();

        var dcx = new DcxStruct(false, 0x11000).Reserve(bw);

        long dcsOffset = bw.Position;
        new DcsStruct(data.Length, compressed.Length).Write(bw);

        long dcpOffset = bw.Position;
        var dcp = new DcpStruct("ZSTD", dcpLevel, 0, 0, 1).Write(bw);

        long dcaOffset = bw.Position;
        new DcaStruct().Write(bw, dcp, true);

        long dataOffset = bw.Position;
        bw.WriteBytes(compressed);
        bw.Align(0x10);

        dcx.Fill(bw, (int)dcsOffset, (int)dcpOffset, (int)dcaOffset, (int)dataOffset);
    }

    public enum Type
    {
        /// <summary>
        /// DCX type could not be detected.
        /// </summary>
        Unknown,

        /// <summary>
        /// The file is not compressed.
        /// </summary>
        None,

        /// <summary>
        /// Plain zlib-wrapped data; not really DCX, but it's convenient to support it here.
        /// </summary>
        Zlib,

        /// <summary>
        /// DCP header, chunked deflate compression. Used in ACE:R TPFs.
        /// </summary>
        DCP_EDGE,

        /// <summary>
        /// DCP header, deflate compression. Used in DeS test maps.
        /// </summary>
        DCP_DFLT,

        /// <summary>
        /// DCX header, chunked deflate compression. Primarily used in DeS.
        /// </summary>
        DCX_EDGE,

        /// <summary>
        /// DCX header, deflate compression. Primarily used in DS1 and DS2.
        /// </summary>
        DCX_DFLT_10000_24_9,

        /// <summary>
        /// DCX header, deflate compression. Primarily used in BB and DS3.
        /// </summary>
        DCX_DFLT_10000_44_9,

        /// <summary>
        /// DCX header, deflate compression. Used for the backup regulation in DS3 save files.
        /// </summary>
        DCX_DFLT_11000_44_8,

        /// <summary>
        /// DCX header, deflate compression. Used in Sekiro.
        /// </summary>
        DCX_DFLT_11000_44_9,

        /// <summary>
        /// DCX header, deflate compression. Used in the ER regulation.
        /// </summary>
        DCX_DFLT_11000_44_9_15,

        /// <summary>
        /// DCX header, Oodle compression. Used in Sekiro and Elden Ring.
        /// </summary>
        DCX_KRAK_6,

        /// <summary>
        /// DCX header, Oodle compression. Used in Armored Core VI.
        /// </summary>
        DCX_KRAK_9,

        /// <summary>
        /// DCX header, Zstandard compression. Used in Elden Ring since DLC release.
        /// </summary>
        DCX_ZSTD_21,

        /// <summary>
        /// DCX header, Zstandard compression. Used in Nightreign DLC.
        /// </summary>
        DCX_ZSTD_22,
    }
}
