namespace Coremats;

public static partial class DCX
{
    private readonly struct DcpStruct
    {
        public string Algorithm
        {
            get;
            private init
            {
                ArgumentOutOfRangeException.ThrowIfNotEqual(value.Length, 4);
                field = value;
            }
        }
        public byte Level { get; }
        public int BlockSize { get; }
        public byte Header { get; }
        public short AlignSize { get; }

        public DcpStruct(string algorithm, byte level, int blockSize, byte header, short alignSize)
        {
            Algorithm = algorithm;
            Level = level;
            BlockSize = blockSize;
            Header = header;
            AlignSize = alignSize;
        }

        public DcpStruct(BexReader br)
        {
            br.AssertAscii("DCP\0");
            Algorithm = br.ReadAscii(4);
            br.AssertInt32(0x20);
            Level = br.ReadByte();
            br.AssertByte(0);
            br.AssertByte(0);
            br.AssertByte(0);
            BlockSize = br.AssertInt32(0, 0x10000);
            Header = br.AssertByte(0, 15);
            br.AssertByte(0);
            br.AssertByte(0);
            br.AssertByte(0);
            br.AssertInt32(0);
            AlignSize = br.AssertInt16(1, 0x10);
            br.AssertInt16(0x0100);
        }

        public DcpStruct Write(BexWriter bw)
        {
            bw.WriteAscii("DCP\0");
            bw.WriteAscii(Algorithm);
            bw.WriteInt32(0x20);
            bw.WriteByte(Level);
            bw.WriteByte(0);
            bw.WriteByte(0);
            bw.WriteByte(0);
            bw.WriteInt32(BlockSize);
            bw.WriteByte(Header);
            bw.WriteByte(0);
            bw.WriteByte(0);
            bw.WriteByte(0);
            bw.WriteInt32(0);
            bw.WriteInt16(AlignSize);
            bw.WriteInt16(0x0100);
            return this;
        }
    }
}
