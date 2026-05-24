namespace Coremats;

public static partial class DCX
{
    private struct DcaStruct
    {
        public int TrailingBlockLengthUncompressed { get; set; }
        public DcaBlockInfo[] BlockInfo { get; set; }

        public DcaStruct() { }

        public DcaStruct(int trailingBlockLengthUncompressed)
        {
            TrailingBlockLengthUncompressed = trailingBlockLengthUncompressed;
        }

        public DcaStruct(DcaBlockInfo[] blockInfo)
        {
            BlockInfo = blockInfo;
        }

        public DcaStruct(int trailingBlockLengthUncompressed, DcaBlockInfo[] blockInfo)
        {
            TrailingBlockLengthUncompressed = trailingBlockLengthUncompressed;
            BlockInfo = blockInfo;
        }

        public DcaStruct(BexReader br, DcpStruct dcp, bool isDcxFormat)
        {
            br.AssertAscii("DCA\0");
            br.ReadInt32();
            if (dcp.Algorithm == "EDGE")
            {
                br.AssertAscii("EgdT");
                br.AssertInt32(isDcxFormat ? 0x10100 : 0x10000);
                br.AssertInt32(isDcxFormat ? 0x24 : 0x20);
                br.AssertInt32(0x10);
                br.AssertInt32(0x10000);
                if (isDcxFormat)
                {
                    TrailingBlockLengthUncompressed = br.ReadInt32();
                }
                br.ReadInt32();
                int blockCount = br.ReadInt32();
                br.AssertInt32(0x100000);

                BlockInfo = new DcaBlockInfo[blockCount];
                for (int i = 0; i < blockCount; i++)
                    BlockInfo[i] = new(br);
            }
        }

        private readonly DcaStruct WriteReserve(bool reserve, BexWriter bw, DcpStruct dcp, bool isDcxFormat, int? blockCount)
        {
            long dcaStart = bw.Position;
            bw.WriteAscii("DCA\0");
            bw.ReserveInt32("DcaSize");
            if (dcp.Algorithm == "EDGE")
            {
                long egdtStart = bw.Position;
                bw.WriteAscii("EgdT");
                bw.WriteInt32(isDcxFormat ? 0x10100 : 0x10000);
                bw.WriteInt32(isDcxFormat ? 0x24 : 0x20);
                bw.WriteInt32(0x10);
                bw.WriteInt32(0x10000);
                if (isDcxFormat)
                {
                    if (reserve)
                        bw.ReserveInt32(nameof(TrailingBlockLengthUncompressed));
                    else
                        bw.WriteInt32(TrailingBlockLengthUncompressed);
                }
                bw.ReserveInt32("EgdtSize");
                bw.WriteInt32(blockCount.Value);
                bw.WriteInt32(0x100000);

                for (int i = 0; i < blockCount.Value; i++)
                {
                    if (reserve)
                        DcaBlockInfo.Reserve(bw, i);
                    else
                        BlockInfo[i].Write(bw);
                }

                bw.FillInt32("EgdtSize", (int)(bw.Position - egdtStart));
            }
            bw.FillInt32("DcaSize", (int)(bw.Position - dcaStart));
            return this;
        }

        public readonly DcaStruct Write(BexWriter bw, DcpStruct dcp, bool isDcxFormat)
            => WriteReserve(false, bw, dcp, isDcxFormat, BlockInfo?.Length);

        public readonly DcaStruct Reserve(BexWriter bw, DcpStruct dcp, bool isDcxFormat, int blockCount)
            => WriteReserve(true, bw, dcp, isDcxFormat, blockCount);

        public readonly void Fill(BexWriter bw)
        {
            bw.FillInt32(nameof(TrailingBlockLengthUncompressed), TrailingBlockLengthUncompressed);
            for (int i = 0; i < BlockInfo.Length; i++)
                BlockInfo[i].Fill(bw, i);
        }
    }

    private struct DcaBlockInfo
    {
        public int DataOffset { get; set; }
        public int DataLengthCompressed { get; set; }
        public bool Compressed { get; set; }

        public DcaBlockInfo(int dataOffset, int dataLengthCompressed, bool compressed)
        {
            DataOffset = dataOffset;
            DataLengthCompressed = dataLengthCompressed;
            Compressed = compressed;
        }

        public DcaBlockInfo(BexReader br)
        {
            br.AssertInt32(0);
            DataOffset = br.ReadInt32();
            DataLengthCompressed = br.ReadInt32();
            Compressed = br.AssertInt32(0, 1) == 1;
        }

        public void Write(BexWriter bw)
        {
            bw.WriteInt32(0);
            bw.WriteInt32(DataOffset);
            bw.WriteInt32(DataLengthCompressed);
            bw.WriteInt32(Compressed ? 1 : 0);
        }

        public static void Reserve(BexWriter bw, int index)
        {
            bw.WriteInt32(0);
            bw.ReserveInt32($"{nameof(DcaBlockInfo)}[{index}].{nameof(DataOffset)}");
            bw.ReserveInt32($"{nameof(DcaBlockInfo)}[{index}].{nameof(DataLengthCompressed)}");
            bw.ReserveInt32($"{nameof(DcaBlockInfo)}[{index}].{nameof(Compressed)}");
        }

        public void Fill(BexWriter bw, int index)
        {
            bw.FillInt32($"{nameof(DcaBlockInfo)}[{index}].{nameof(DataOffset)}", DataOffset);
            bw.FillInt32($"{nameof(DcaBlockInfo)}[{index}].{nameof(DataLengthCompressed)}", DataLengthCompressed);
            bw.FillInt32($"{nameof(DcaBlockInfo)}[{index}].{nameof(Compressed)}", Compressed ? 1 : 0);
        }
    }
}
