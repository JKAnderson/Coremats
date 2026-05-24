namespace Coremats;

public static partial class DCX
{
    private readonly struct DcxStruct
    {
        public bool LegacyOffsets { get; }
        public int Unk04 { get; }

        public DcxStruct(bool legacyOffsets, int unk04)
        {
            LegacyOffsets = legacyOffsets;
            Unk04 = unk04;
        }

        public DcxStruct(BexReader br)
        {
            br.AssertAscii("DCX\0");
            Unk04 = br.AssertInt32(0x10000, 0x11000);
            br.AssertInt32(0x18);
            br.AssertInt32(0x24);
            LegacyOffsets = br.AssertInt32(0x24, 0x44) == 0x24;
            br.ReadInt32();
        }

        public DcxStruct Reserve(BexWriter bw)
        {
            bw.WriteAscii("DCX\0");
            bw.WriteInt32(Unk04);
            bw.ReserveInt32("DcsOffset");
            bw.ReserveInt32("DcpOffset");
            bw.ReserveInt32("DcaOffset");
            bw.ReserveInt32("DataOffset");
            return this;
        }

        public void Fill(BexWriter bw, int dcsOffset, int dcpOffset, int dcaOffset, int dataOffset)
        {
            int offsetOffset = LegacyOffsets ? 0x20 : 0;
            bw.FillInt32("DcsOffset", dcsOffset);
            bw.FillInt32("DcpOffset", dcpOffset);
            bw.FillInt32("DcaOffset", dcaOffset - offsetOffset);
            bw.FillInt32("DataOffset", dataOffset - offsetOffset);
        }
    }
}
