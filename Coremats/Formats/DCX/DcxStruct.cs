namespace Coremats;

public static partial class DCX
{
    private readonly struct DcxStruct
    {
        public int Unk04 { get; }

        public DcxStruct(int unk04)
        {
            Unk04 = unk04;
        }

        public DcxStruct(BexReader br)
        {
            br.AssertAscii("DCX\0");
            Unk04 = br.AssertInt32(0x10000, 0x11000);
            br.ReadInt32();
            br.ReadInt32();
            // In older games (before BB, approximately), these last two offsets were wrong
            // Since they're not used at runtime anyways, it's not worth preserving
            br.ReadInt32();
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
            bw.FillInt32("DcsOffset", dcsOffset);
            bw.FillInt32("DcpOffset", dcpOffset);
            bw.FillInt32("DcaOffset", dcaOffset);
            bw.FillInt32("DataOffset", dataOffset);
        }
    }
}
