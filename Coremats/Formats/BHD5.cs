namespace Coremats;

public class BHD5 : FileFormat
{
    public enum Bhd5Format
    {
        DarkSouls,
        DarkSoulsRemastered,
        DarkSouls2,
        DarkSouls3,
        EldenRing,
    }

    public Bhd5Format Format { get; set; }
    public bool LittleEndian { get; set; }
    public bool Unk05 { get; set; }
    public string DataSalt { get; set; }
    public List<Bucket> Buckets { get; set; }

    public static bool Is(string path) => IsFile(path, Is);
    public static bool Is(byte[] bytes) => IsBytes(bytes, Is);

    public static BHD5 Read(string path, Bhd5Format format) => ReadFile(path, br => new BHD5(br, format));
    public static BHD5 Read(byte[] bytes, Bhd5Format format) => ReadBytes(bytes, br => new BHD5(br, format));

    private static bool Is(BexReader br)
    {
        if (br.Length < 4)
            return false;

        return br.ReadAscii(4) == "BHD5";
    }

    private BHD5(BexReader br, Bhd5Format format)
    {
        Format = format;

        br.AssertAscii("BHD5");
        LittleEndian = br.AssertSByte(0, -1) != 0;
        br.BigEndian = !LittleEndian;
        Unk05 = br.ReadBoolean();
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertInt32(1);
        br.ReadInt32();

        int bucketCount = br.ReadInt32();
        long bucketsOffset;
        if (format == Bhd5Format.DarkSoulsRemastered)
        {
            br.AssertInt32(0);
            bucketsOffset = br.ReadInt64();
        }
        else
        {
            bucketsOffset = br.ReadInt32();
        }

        if (format >= Bhd5Format.DarkSouls2)
        {
            int saltLength = br.ReadInt32();
            DataSalt = br.ReadAscii(saltLength);
        }

        br.Position = bucketsOffset;
        Buckets = new(bucketCount);
        for (int i = 0; i < bucketCount; i++)
            Buckets.Add(new(br, format));
    }

    public class Bucket : List<File>
    {
        internal Bucket(BexReader br, Bhd5Format format)
        {
            int fileCount = br.ReadInt32();
            long filesOffset;
            if (format == Bhd5Format.DarkSoulsRemastered)
            {
                br.AssertInt32(1);
                filesOffset = br.ReadInt64();
            }
            else
            {
                filesOffset = br.ReadInt32();
            }

            Capacity = fileCount;
            br.JumpIn(filesOffset);
            {
                for (int i = 0; i < fileCount; i++)
                    Add(new(br, format));
            }
            br.JumpOut();
        }
    }

    public class File
    {
        public ulong PathHash { get; set; }
        public int DataLength { get; set; }
        public int UnpaddedDataLength { get; set; }
        public long DataOffset { get; set; }
        public FileHash Hash { get; set; }
        public FileEncryption Encryption { get; set; }

        internal File(BexReader br, Bhd5Format format)
        {
            long hashOffset = 0, encryptionOffset = 0;
            if (format >= Bhd5Format.EldenRing)
            {
                PathHash = br.ReadUInt64();
                DataLength = br.ReadInt32();
                UnpaddedDataLength = br.ReadInt32();
                DataOffset = br.ReadInt64();
                hashOffset = br.ReadInt64();
                encryptionOffset = br.ReadInt64();
            }
            else
            {
                PathHash = br.ReadUInt32();
                DataLength = br.ReadInt32();
                DataOffset = br.ReadInt64();
                if (format >= Bhd5Format.DarkSouls2)
                {
                    hashOffset = br.ReadInt64();
                    encryptionOffset = br.ReadInt64();
                }
                if (format >= Bhd5Format.DarkSouls3)
                {
                    UnpaddedDataLength = br.ReadInt32();
                    br.AssertInt32(0);
                }
            }

            if (hashOffset != 0)
            {
                br.JumpIn(hashOffset);
                Hash = new(br);
                br.JumpOut();
            }

            if (encryptionOffset != 0)
            {
                br.JumpIn(encryptionOffset);
                Encryption = new(br);
                br.JumpOut();
            }
        }
    }

    public class FileHash
    {
        public byte[] Hash { get; }
        public List<Range> Ranges { get; set; }

        internal FileHash(BexReader br)
        {
            Hash = br.ReadBytes(32);
            int rangeCount = br.ReadInt32();

            Ranges = new(rangeCount);
            for (int i = 0; i < rangeCount; i++)
                Ranges.Add(new(br));
        }
    }

    public class FileEncryption
    {
        public byte[] Key { get; }
        public List<Range> Ranges { get; set; }

        internal FileEncryption(BexReader br)
        {
            Key = br.ReadBytes(16);
            int rangeCount = br.ReadInt32();

            Ranges = new(rangeCount);
            for (int i = 0; i < rangeCount; i++)
                Ranges.Add(new(br));
        }
    }

    public class Range
    {
        public long Start { get; set; }
        public long End { get; set; }

        internal Range(BexReader br)
        {
            Start = br.ReadInt64();
            End = br.ReadInt64();
        }
    }
}
