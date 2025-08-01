﻿using System.IO.Compression;

namespace Coremats;

/// <summary>
/// A general-purpose single compressed file wrapper used in most modern FromSoftware games.
/// </summary>
public static class DCX
{
    public static bool Is(BinaryReaderEx br)
    {
        if (br.Stream.Length < 4)
            return false;

        string magic = br.GetASCII(0, 4);
        return magic == "DCP\0" || magic == "DCX\0";
    }

    /// <summary>
    /// Returns true if the bytes appear to be a DCX file.
    /// </summary>
    public static bool Is(byte[] bytes)
    {
        var br = new BinaryReaderEx(true, bytes);
        return Is(br);
    }

    /// <summary>
    /// Returns true if the file appears to be a DCX file.
    /// </summary>
    public static bool Is(string path)
    {
        using (FileStream stream = File.OpenRead(path))
        {
            var br = new BinaryReaderEx(true, stream);
            return Is(br);
        }
    }

    #region Public Decompress
    /// <summary>
    /// Decompress a DCX file from an array of bytes and return the detected DCX type.
    /// </summary>
    public static byte[] Decompress(byte[] data, out Type type)
    {
        BinaryReaderEx br = new BinaryReaderEx(true, data);
        return Decompress(br, out type);
    }

    /// <summary>
    /// Decompress a DCX file from an array of bytes.
    /// </summary>
    public static byte[] Decompress(byte[] data)
    {
        return Decompress(data, out _);
    }

    /// <summary>
    /// Decompress a DCX file from the specified path and return the detected DCX type.
    /// </summary>
    public static byte[] Decompress(string path, out Type type)
    {
        using (FileStream stream = File.OpenRead(path))
        {
            BinaryReaderEx br = new BinaryReaderEx(true, stream);
            return Decompress(br, out type);
        }
    }

    /// <summary>
    /// Decompress a DCX file from the specified path.
    /// </summary>
    public static byte[] Decompress(string path)
    {
        return Decompress(path, out _);
    }
    #endregion

    public static byte[] Decompress(BinaryReaderEx br, out Type type)
    {
        br.BigEndian = true;
        string magic = br.GetASCII(0, 4);
        if (magic == "DCP\0")
        {
            string format = br.GetASCII(4, 4);
            return format switch
            {
                "DFLT" => DecompressDCPDFLT(br, out type),
                "EDGE" => DecompressDCPEDGE(br, out type),
                _ => throw new NotImplementedException($"Unimplemented DCP compression format \"{format}\".")
            };
        }
        else if (magic == "DCX\0")
        {
            string format = br.GetASCII(0x28, 4);
            return format switch
            {
                "DFLT" => DecompressDCXDFLT(br, out type),
                "EDGE" => DecompressDCXEDGE(br, out type),
                "KRAK" => DecompressDCXKRAK(br, out type),
                "ZSTD" => DecompressDCXZSTD(br, out type),
                _ => throw new NotImplementedException($"Unimplemented DCX compression format \"{format}\".")
            };
        }
        else
        {
            byte b0 = br.GetByte(0);
            byte b1 = br.GetByte(1);
            if (b0 == 0x78 && (b1 == 0x01 || b1 == 0x5E || b1 == 0x9C || b1 == 0xDA))
            {
                type = Type.Zlib;
                return SFUtil.ReadZlib(br, (int)br.Length);
            }
            else
            {
                throw new InvalidDataException($"Could not determine compression format.");
            }
        }
    }

    private static byte[] DecompressDCPDFLT(BinaryReaderEx br, out Type type)
    {
        br.AssertASCII("DCP\0");
        br.AssertASCII("DFLT");
        br.AssertInt32(0x20);
        br.AssertByte(9);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertInt32(0);
        br.AssertInt32(0);
        br.AssertInt32(0);
        br.AssertInt32(0x00010100);

        br.AssertASCII("DCS\0");
        int uncompressedSize = br.ReadInt32();
        int compressedSize = br.ReadInt32();

        byte[] decompressed = SFUtil.ReadZlib(br, compressedSize);

        br.AssertASCII("DCA\0");
        br.AssertInt32(8);

        type = Type.DCP_DFLT;
        return decompressed;
    }

    private static byte[] DecompressDCPEDGE(BinaryReaderEx br, out Type type)
    {
        br.AssertASCII("DCP\0");
        br.AssertASCII("EDGE");
        br.AssertInt32(0x20);
        br.AssertByte(9);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertInt32(0x10000);
        br.AssertInt32(0x0);
        br.AssertInt32(0x0);
        br.AssertInt32(0x00100100);

        br.AssertASCII("DCS\0");
        int uncompressedSize = br.ReadInt32();
        int compressedSize = br.ReadInt32();
        br.AssertInt32(0);
        long dataStart = br.Position;
        br.Skip(compressedSize);

        br.AssertASCII("DCA\0");
        int dcaSize = br.ReadInt32();
        br.AssertASCII("EgdT");
        br.AssertInt32(0x00010000);
        br.AssertInt32(0x20);
        br.AssertInt32(0x10);
        br.AssertInt32(0x10000);
        int egdtSize = br.ReadInt32();
        int chunkCount = br.ReadInt32();
        br.AssertInt32(0x100000);

        if (egdtSize != 0x20 + chunkCount * 0x10)
            throw new InvalidDataException("Unexpected EgdT size in EDGE DCX.");

        byte[] decompressed = new byte[uncompressedSize];
        using (MemoryStream dcmpStream = new MemoryStream(decompressed))
        {
            for (int i = 0; i < chunkCount; i++)
            {
                br.AssertInt32(0);
                int offset = br.ReadInt32();
                int size = br.ReadInt32();
                bool compressed = br.AssertInt32(0, 1) == 1;

                byte[] chunk = br.GetBytes(dataStart + offset, size);

                if (compressed)
                {
                    using (MemoryStream cmpStream = new MemoryStream(chunk))
                    using (DeflateStream dfltStream = new DeflateStream(cmpStream, CompressionMode.Decompress))
                        dfltStream.CopyTo(dcmpStream);
                }
                else
                {
                    dcmpStream.Write(chunk, 0, chunk.Length);
                }
            }
        }

        type = Type.DCP_EDGE;
        return decompressed;
    }

    private static byte[] DecompressDCXEDGE(BinaryReaderEx br, out Type type)
    {
        br.AssertASCII("DCX\0");
        br.AssertInt32(0x10000);
        br.AssertInt32(0x18);
        br.AssertInt32(0x24);
        br.AssertInt32(0x24);
        int unk14 = br.ReadInt32();

        br.AssertASCII("DCS\0");
        int uncompressedSize = br.ReadInt32();
        int compressedSize = br.ReadInt32();

        br.AssertASCII("DCP\0");
        br.AssertASCII("EDGE");
        br.AssertInt32(0x20);
        br.AssertByte(9);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertInt32(0x10000);
        br.AssertInt32(0x0);
        br.AssertInt32(0x0);
        br.AssertInt32(0x00100100);

        long dcaStart = br.Position;
        br.AssertASCII("DCA\0");
        int dcaSize = br.ReadInt32();
        // ???
        br.AssertASCII("EgdT");
        br.AssertInt32(0x00010100);
        br.AssertInt32(0x24);
        br.AssertInt32(0x10);
        br.AssertInt32(0x10000);
        // Uncompressed size of last block
        int trailingUncompressedSize = br.AssertInt32(uncompressedSize % 0x10000, 0x10000);
        int egdtSize = br.ReadInt32();
        int chunkCount = br.ReadInt32();
        br.AssertInt32(0x100000);

        if (unk14 != 0x50 + chunkCount * 0x10)
            throw new InvalidDataException("Unexpected unk1 value in EDGE DCX.");

        if (egdtSize != 0x24 + chunkCount * 0x10)
            throw new InvalidDataException("Unexpected EgdT size in EDGE DCX.");

        byte[] decompressed = new byte[uncompressedSize];
        using (MemoryStream dcmpStream = new MemoryStream(decompressed))
        {
            for (int i = 0; i < chunkCount; i++)
            {
                br.AssertInt32(0);
                int offset = br.ReadInt32();
                int size = br.ReadInt32();
                bool compressed = br.AssertInt32(0, 1) == 1;

                byte[] chunk = br.GetBytes(dcaStart + dcaSize + offset, size);

                if (compressed)
                {
                    using (MemoryStream cmpStream = new MemoryStream(chunk))
                    using (DeflateStream dfltStream = new DeflateStream(cmpStream, CompressionMode.Decompress))
                        dfltStream.CopyTo(dcmpStream);
                }
                else
                {
                    dcmpStream.Write(chunk, 0, chunk.Length);
                }
            }
        }

        type = Type.DCX_EDGE;
        return decompressed;
    }

    private static byte[] DecompressDCXDFLT(BinaryReaderEx br, out Type type)
    {
        br.AssertASCII("DCX\0");
        int unk04 = br.AssertInt32(0x10000, 0x11000);
        br.AssertInt32(0x18);
        br.AssertInt32(0x24);
        int unk10 = br.AssertInt32(0x24, 0x44);
        br.AssertInt32(unk10 == 0x24 ? 0x2c : 0x4c);

        br.AssertASCII("DCS\0");
        int uncompressedSize = br.ReadInt32();
        int compressedSize = br.ReadInt32();

        br.AssertASCII("DCP\0");
        br.AssertASCII("DFLT");
        br.AssertInt32(0x20);
        byte level = br.AssertByte(8, 9);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertInt32(0x0);
        byte unk38 = br.AssertByte(0, 15);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertInt32(0x0);
        br.AssertInt32(0x00010100);

        br.AssertASCII("DCA\0");
        int compressedHeaderLength = br.ReadInt32();

        if (unk04 == 0x10000 && unk10 == 0x24 && level == 9 && unk38 == 0)
            type = Type.DCX_DFLT_10000_24_9;
        else if (unk04 == 0x10000 && unk10 == 0x44 && level == 9 && unk38 == 0)
            type = Type.DCX_DFLT_10000_44_9;
        else if (unk04 == 0x11000 && unk10 == 0x44 && level == 8 && unk38 == 0)
            type = Type.DCX_DFLT_11000_44_8;
        else if (unk04 == 0x11000 && unk10 == 0x44 && level == 9 && unk38 == 0)
            type = Type.DCX_DFLT_11000_44_9;
        else if (unk04 == 0x11000 && unk10 == 0x44 && level == 9 && unk38 == 15)
            type = Type.DCX_DFLT_11000_44_9_15;
        else
            throw new NotImplementedException($"Unimplemented DCX DFLT permutation.");

        return SFUtil.ReadZlib(br, compressedSize);
    }

    private static byte[] DecompressDCXKRAK(BinaryReaderEx br, out Type type)
    {
        br.AssertASCII("DCX\0");
        br.AssertInt32(0x11000);
        br.AssertInt32(0x18);
        br.AssertInt32(0x24);
        br.AssertInt32(0x44);
        br.AssertInt32(0x4C);

        br.AssertASCII("DCS\0");
        uint uncompressedSize = br.ReadUInt32();
        uint compressedSize = br.ReadUInt32();

        br.AssertASCII("DCP\0");
        br.AssertASCII("KRAK");
        br.AssertInt32(0x20);
        byte level = br.AssertByte(6, 9);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertInt32(0);
        br.AssertInt32(0);
        br.AssertInt32(0);
        br.AssertInt32(0x10100);

        br.AssertASCII("DCA\0");
        br.AssertInt32(8);

        if (level == 6)
            type = Type.DCX_KRAK_6;
        else if (level == 9)
            type = Type.DCX_KRAK_9;
        else
            throw new NotImplementedException($"Unimplemented DCX KRAK permutation.");

        byte[] compressed = br.ReadBytes((int)compressedSize);
        return Oodle.Decompress(compressed, uncompressedSize);
    }

    private static byte[] DecompressDCXZSTD(BinaryReaderEx br, out Type type)
    {
        br.AssertASCII("DCX\0");
        br.AssertInt32(0x11000);
        br.AssertInt32(0x18);
        br.AssertInt32(0x24);
        br.AssertInt32(0x44);
        br.AssertInt32(0x4C);

        br.AssertASCII("DCS\0");
        uint uncompressedSize = br.ReadUInt32();
        uint compressedSize = br.ReadUInt32();

        br.AssertASCII("DCP\0");
        br.AssertASCII("ZSTD");
        br.AssertInt32(0x20);
        br.AssertByte(21);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertInt32(0);
        br.AssertInt32(0);
        br.AssertInt32(0);
        br.AssertInt32(0x10100);

        br.AssertASCII("DCA\0");
        br.AssertInt32(8);

        type = Type.DCX_ZSTD;
        byte[] compressed = br.ReadBytes((int)compressedSize);
        var decompressor = new ZstdSharp.Decompressor();
        return decompressor.Unwrap(compressed).ToArray();
    }

    #region Public Compress
    /// <summary>
    /// Compress a DCX file to an array of bytes using the specified DCX type.
    /// </summary>
    public static byte[] Compress(byte[] data, Type type)
    {
        BinaryWriterEx bw = new BinaryWriterEx(true);
        Compress(data, bw, type);
        return bw.FinishBytes();
    }

    /// <summary>
    /// Compress a DCX file to the specified path using the specified DCX type.
    /// </summary>
    public static void Compress(byte[] data, Type type, string path)
    {
        using (FileStream stream = File.Create(path))
        {
            BinaryWriterEx bw = new BinaryWriterEx(true, stream);
            Compress(data, bw, type);
            bw.Finish();
        }
    }
    #endregion

    public static void Compress(byte[] data, BinaryWriterEx bw, Type type)
    {
        bw.BigEndian = true;
        if (type == Type.Zlib)
            SFUtil.WriteZlib(bw, 0xDA, data);
        else if (type == Type.DCP_DFLT)
            CompressDCPDFLT(data, bw);
        else if (type == Type.DCX_EDGE)
            CompressDCXEDGE(data, bw);
        else if (type == Type.DCX_DFLT_10000_24_9
            || type == Type.DCX_DFLT_10000_44_9
            || type == Type.DCX_DFLT_11000_44_8
            || type == Type.DCX_DFLT_11000_44_9
            || type == Type.DCX_DFLT_11000_44_9_15)
            CompressDCXDFLT(data, bw, type);
        else if (type == Type.DCX_KRAK_6
            || type == Type.DCX_KRAK_9)
            CompressDCXKRAK(data, bw, type);
        else if (type == Type.DCX_ZSTD)
            CompressDCXZSTD(data, bw);
        else if (type == Type.Unknown)
            throw new ArgumentException("You cannot compress a DCX with an unknown type.");
        else
            throw new NotImplementedException("Compression for the given type is not implemented.");
    }

    private static void CompressDCPDFLT(byte[] data, BinaryWriterEx bw)
    {
        bw.WriteASCII("DCP\0");
        bw.WriteASCII("DFLT");
        bw.WriteInt32(0x20);
        bw.WriteByte(9);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0x00010100);

        bw.WriteASCII("DCS\0");
        bw.WriteInt32(data.Length);
        bw.ReserveInt32("CompressedSize");

        int compressedSize = SFUtil.WriteZlib(bw, 0xDA, data);
        bw.FillInt32("CompressedSize", compressedSize);

        bw.WriteASCII("DCA\0");
        bw.WriteInt32(8);
    }

    private static void CompressDCXEDGE(byte[] data, BinaryWriterEx bw)
    {
        int chunkCount = data.Length / 0x10000;
        if (data.Length % 0x10000 > 0)
            chunkCount++;

        bw.WriteASCII("DCX\0");
        bw.WriteInt32(0x10000);
        bw.WriteInt32(0x18);
        bw.WriteInt32(0x24);
        bw.WriteInt32(0x24);
        bw.WriteInt32(0x50 + chunkCount * 0x10);

        bw.WriteASCII("DCS\0");
        bw.WriteInt32(data.Length);
        bw.ReserveInt32("CompressedSize");

        bw.WriteASCII("DCP\0");
        bw.WriteASCII("EDGE");
        bw.WriteInt32(0x20);
        bw.WriteByte(9);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteInt32(0x10000);
        bw.WriteInt32(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0x00100100);

        long dcaStart = bw.Position;
        bw.WriteASCII("DCA\0");
        bw.ReserveInt32("DCASize");
        long egdtStart = bw.Position;
        bw.WriteASCII("EgdT");
        bw.WriteInt32(0x00010100);
        bw.WriteInt32(0x24);
        bw.WriteInt32(0x10);
        bw.WriteInt32(0x10000);
        bw.WriteInt32(data.Length % 0x10000);
        bw.ReserveInt32("EGDTSize");
        bw.WriteInt32(chunkCount);
        bw.WriteInt32(0x100000);

        for (int i = 0; i < chunkCount; i++)
        {
            bw.WriteInt32(0);
            bw.ReserveInt32($"ChunkOffset{i}");
            bw.ReserveInt32($"ChunkSize{i}");
            bw.ReserveInt32($"ChunkCompressed{i}");
        }

        bw.FillInt32("DCASize", (int)(bw.Position - dcaStart));
        bw.FillInt32("EGDTSize", (int)(bw.Position - egdtStart));
        long dataStart = bw.Position;

        int compressedSize = 0;
        for (int i = 0; i < chunkCount; i++)
        {
            int chunkSize = 0x10000;
            if (i == chunkCount - 1)
                chunkSize = data.Length % 0x10000;

            byte[] chunk;
            using (MemoryStream cmpStream = new MemoryStream())
            using (MemoryStream dcmpStream = new MemoryStream(data, i * 0x10000, chunkSize))
            {
                DeflateStream dfltStream = new DeflateStream(cmpStream, CompressionMode.Compress);
                dcmpStream.CopyTo(dfltStream);
                dfltStream.Close();
                chunk = cmpStream.ToArray();
            }

            if (chunk.Length < chunkSize)
                bw.FillInt32($"ChunkCompressed{i}", 1);
            else
            {
                bw.FillInt32($"ChunkCompressed{i}", 0);
                chunk = data;
            }

            compressedSize += chunk.Length;
            bw.FillInt32($"ChunkOffset{i}", (int)(bw.Position - dataStart));
            bw.FillInt32($"ChunkSize{i}", chunk.Length);
            bw.WriteBytes(chunk);
            bw.Pad(0x10);
        }

        bw.FillInt32("CompressedSize", compressedSize);
    }

    private static void CompressDCXDFLT(byte[] data, BinaryWriterEx bw, Type type)
    {
        int unk04 = (type == Type.DCX_DFLT_10000_24_9 || type == Type.DCX_DFLT_10000_44_9) ? 0x10000 : 0x11000;
        int unk10 = type == Type.DCX_DFLT_10000_24_9 ? 0x24 : 0x44;
        int unk14 = type == Type.DCX_DFLT_10000_24_9 ? 0x2C : 0x4C;
        byte level = (byte)(type == Type.DCX_DFLT_11000_44_8 ? 8 : 9);
        byte unk38 = (byte)(type == Type.DCX_DFLT_11000_44_9_15 ? 15 : 0);

        bw.WriteASCII("DCX\0");
        bw.WriteInt32(unk04);
        bw.WriteInt32(0x18);
        bw.WriteInt32(0x24);
        bw.WriteInt32(unk10);
        bw.WriteInt32(unk14);

        bw.WriteASCII("DCS\0");
        bw.WriteInt32(data.Length);
        bw.ReserveInt32("CompressedSize");

        bw.WriteASCII("DCP\0");
        bw.WriteASCII("DFLT");
        bw.WriteInt32(0x20);
        bw.WriteByte(level);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteInt32(0);
        bw.WriteByte(unk38);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0x00010100);

        bw.WriteASCII("DCA\0");
        bw.WriteInt32(8);

        long compressedStart = bw.Position;
        SFUtil.WriteZlib(bw, 0xDA, data);
        bw.FillInt32("CompressedSize", (int)(bw.Position - compressedStart));
    }

    private static void CompressDCXKRAK(byte[] data, BinaryWriterEx bw, Type type)
    {
        byte level = (byte)(type == Type.DCX_KRAK_6 ? 6 : 9);
        byte[] compressed = Oodle.Compress(data, Noodle.Oodle2_9.OodleLZ.OodleLZ_Compressor.OodleLZ_Compressor_Kraken, (Noodle.Oodle2_9.OodleLZ.OodleLZ_CompressionLevel)level);

        bw.WriteASCII("DCX\0");
        bw.WriteInt32(0x11000);
        bw.WriteInt32(0x18);
        bw.WriteInt32(0x24);
        bw.WriteInt32(0x44);
        bw.WriteInt32(0x4C);

        bw.WriteASCII("DCS\0");
        bw.WriteUInt32((uint)data.Length);
        bw.WriteUInt32((uint)compressed.Length);

        bw.WriteASCII("DCP\0");
        bw.WriteASCII("KRAK");
        bw.WriteInt32(0x20);
        bw.WriteByte(level);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0x10100);

        bw.WriteASCII("DCA\0");
        bw.WriteInt32(8);

        bw.WriteBytes(compressed);
        bw.Pad(0x10);
    }

    private static void CompressDCXZSTD(byte[] data, BinaryWriterEx bw)
    {
        var compressor = new ZstdSharp.Compressor(21);
        compressor.SetParameter(ZstdSharp.Unsafe.ZSTD_cParameter.ZSTD_c_contentSizeFlag, 0);
        compressor.SetParameter(ZstdSharp.Unsafe.ZSTD_cParameter.ZSTD_c_windowLog, 16);
        byte[] compressed = compressor.Wrap(data).ToArray();

        bw.WriteASCII("DCX\0");
        bw.WriteInt32(0x11000);
        bw.WriteInt32(0x18);
        bw.WriteInt32(0x24);
        bw.WriteInt32(0x44);
        bw.WriteInt32(0x4C);

        bw.WriteASCII("DCS\0");
        bw.WriteUInt32((uint)data.Length);
        bw.WriteUInt32((uint)compressed.Length);

        bw.WriteASCII("DCP\0");
        bw.WriteASCII("ZSTD");
        bw.WriteInt32(0x20);
        bw.WriteByte(21);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0);
        bw.WriteInt32(0x10100);

        bw.WriteASCII("DCA\0");
        bw.WriteInt32(8);

        bw.WriteBytes(compressed);
        bw.Pad(0x10);
    }

    /// <summary>
    /// Specific compression format used for a certain file.
    /// </summary>
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
        DCX_ZSTD,
    }

    /// <summary>
    /// Standard compression types used by various games; may be cast directly to DCX.Type.
    /// </summary>
    public enum DefaultType
    {
        /// <summary>
        /// Most common compression format for Demon's Souls.
        /// </summary>
        DemonsSouls = Type.DCX_EDGE,

        /// <summary>
        /// Most common compression format for Dark Souls 1.
        /// </summary>
        DarkSouls1 = Type.DCX_DFLT_10000_24_9,

        /// <summary>
        /// Most common compression format for Dark Souls 2.
        /// </summary>
        DarkSouls2 = Type.DCX_DFLT_10000_24_9,

        /// <summary>
        /// Most common compression format for Bloodborne.
        /// </summary>
        Bloodborne = Type.DCX_DFLT_10000_44_9,

        /// <summary>
        /// Most common compression format for Dark Souls 3.
        /// </summary>
        DarkSouls3 = Type.DCX_DFLT_10000_44_9,

        /// <summary>
        /// Most common compression format for Sekiro.
        /// </summary>
        Sekiro = Type.DCX_KRAK_6,

        /// <summary>
        /// Most common compression format for Elden Ring.
        /// </summary>
        EldenRing = Type.DCX_KRAK_6,

        /// <summary>
        /// Most common compression format for Armored Core VI.
        /// </summary>
        ArmoredCore6 = Type.DCX_KRAK_9,
    }
}
