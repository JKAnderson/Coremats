namespace Coremats.PSP;

public class UMDL : FileFormat
{
    public int DataLength { get; set; }
    public List<File> Files { get; set; }

    public static bool Is(string path) => IsFile(path, Is);
    public static bool Is(byte[] bytes) => IsBytes(bytes, Is);

    public static UMDL Read(string path) => ReadFile(path, br => new UMDL(br));
    public static UMDL Read(byte[] bytes) => ReadBytes(bytes, br => new UMDL(br));

    private static bool Is(BexReader br)
    {
        if (br.Length < 4)
            return false;

        return br.ReadAscii(4) == "LDMU";
    }

    private UMDL(BexReader br)
    {
        br.AssertAscii("LDMU");
        br.AssertInt32(0x210);
        int fileCount = br.ReadInt32();
        DataLength = br.ReadInt32();
        br.AssertInt32(0);
        br.AssertInt32(0);
        br.AssertInt32(0);

        Files = new(fileCount);
        for (int i = 0; i < fileCount; i++)
            Files.Add(new(br));
    }

    public class File
    {
        public int Unk04 { get; set; }
        public int DataBlock { get; set; }
        public int LengthUncompressed { get; set; }
        public int LengthCompressed { get; set; }
        public int Unk10 { get; set; }
        public int Unk14 { get; set; }
        public int Unk18 { get; set; }
        public int Unk1c { get; set; }
        public int Unk20 { get; set; }
        public int Unk24 { get; set; }

        internal File(BexReader br)
        {
            Unk04 = br.ReadInt32();
            DataBlock = br.ReadInt32();
            LengthUncompressed = br.ReadInt32();
            LengthCompressed = br.ReadInt32();
            Unk10 = br.ReadInt32();
            Unk14 = br.ReadInt32();
            Unk18 = br.ReadInt32();
            Unk1c = br.ReadInt32();
            Unk20 = br.ReadInt32();
            Unk24 = br.ReadInt32();
        }
    }
}
