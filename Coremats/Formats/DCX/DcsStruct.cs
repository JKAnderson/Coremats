namespace Coremats;

public static partial class DCX
{
    private struct DcsStruct
    {
        public int DataLengthUncompressed { get; }
        public int DataLengthCompressed { get; set; }

        public DcsStruct(int dataLengthUncompressed)
        {
            DataLengthUncompressed = dataLengthUncompressed;
        }

        public DcsStruct(int dataLengthUncompressed, int dataLengthCompressed)
        {
            DataLengthUncompressed = dataLengthUncompressed;
            DataLengthCompressed = dataLengthCompressed;
        }

        public DcsStruct(BexReader br)
        {
            br.AssertAscii("DCS\0");
            DataLengthUncompressed = br.ReadInt32();
            DataLengthCompressed = br.ReadInt32();
        }

        public readonly DcsStruct Write(BexWriter bw)
        {
            bw.WriteAscii("DCS\0");
            bw.WriteInt32(DataLengthUncompressed);
            bw.WriteInt32(DataLengthCompressed);
            return this;
        }

        public readonly DcsStruct Reserve(BexWriter bw)
        {
            bw.WriteAscii("DCS\0");
            bw.WriteInt32(DataLengthUncompressed);
            bw.ReserveInt32("DataLengthCompressed");
            return this;
        }

        public readonly void Fill(BexWriter bw)
        {
            bw.FillInt32("DataLengthCompressed", DataLengthCompressed);
        }
    }
}
