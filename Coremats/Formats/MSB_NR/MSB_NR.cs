namespace Coremats;
public partial class MSB_NR : CompressibleFileFormat
{
    public ModelParam Models { get; set; }
    public EventParam Events { get; set; }
    public PointParam Points { get; set; }
    public RouteParam Routes { get; set; }
    public LayerParam Layers { get; set; }
    public PartsParam Parts { get; set; }

    public static MSB_NR Read(string path) => ReadFile(path, br => new MSB_NR(br));
    public static MSB_NR Read(byte[] bytes) => ReadBytes(bytes, br => new MSB_NR(br));

    public void Write(string path) => WriteFile(path, Write);
    public void Write(string path, DCX.Type compression) => WriteFile(path, Write, compression);
    public byte[] Write() => WriteBytes(Write);
    public byte[] Write(DCX.Type compression) => WriteBytes(Write, compression);

    public MSB_NR()
    {
        Models = new();
        Events = new();
        Points = new();
        Routes = new();
        Layers = new();
        Parts = new();
    }

    private MSB_NR(BinaryReaderEx br)
    {
        br.AssertASCII("MSB ");
        br.AssertInt32(1);
        br.AssertInt32(0x10);
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertByte(1);
        br.AssertSByte(-1);

        Models = new(br, false);
        Events = new(br, false);
        Points = new(br, false);
        Routes = new(br, false);
        Layers = new(br, false);
        Parts = new(br, true);

        Events.Postprocess(this);
        Points.Postprocess(this);
        Parts.Postprocess(this);
    }

    private void Write(BinaryWriterEx bw)
    {
        Models.PreprocessStage1();
        Events.PreprocessStage1();
        Points.PreprocessStage1();
        Routes.PreprocessStage1();
        Parts.PreprocessStage1();

        Events.PreprocessStage2(this);
        Points.PreprocessStage2(this);
        Parts.PreprocessStage2(this);

        bw.WriteASCII("MSB ");
        bw.WriteInt32(1);
        bw.WriteInt32(0x10);
        bw.WriteByte(0);
        bw.WriteByte(0);
        bw.WriteByte(1);
        bw.WriteSByte(-1);

        Models.Write(bw, false);
        Events.Write(bw, false);
        Points.Write(bw, false);
        Routes.Write(bw, false);
        Layers.Write(bw, false);
        Parts.Write(bw, true);
    }

    public abstract class Entry
    {
        internal abstract void Write(BinaryWriterEx bw);
    }

    public abstract class Param<T> where T : Entry
    {
        public int Version { get; set; }
        public abstract string Name { get; }
        public List<T> Entries { get; set; }

        protected Param()
        {
            Version = 78;
            Entries = [];
        }

        protected Param(BinaryReaderEx br, bool lastParam, Func<BinaryReaderEx, T> readEntry)
        {
            Version = br.AssertInt32(75, 78);
            int entryCount = br.ReadInt32() - 1;
            string name = br.GetUTF16(br.ReadInt64());
            long[] offsets = br.ReadInt64s(entryCount);
            long nextParamOffset = br.ReadInt64();

            if (name != Name)
                throw new InvalidDataException($"Unexpected param name: {name}");
            if (lastParam && nextParamOffset != 0)
                throw new InvalidDataException($"Unexpected param offset: 0x{nextParamOffset:x}");

            Entries = new(entryCount);
            for (int i = 0; i < entryCount; i++)
            {
                br.Position = offsets[i];
                Entries.Add(readEntry(br));
            }

            if (!lastParam)
                br.Position = nextParamOffset;
        }

        internal void Write(BinaryWriterEx bw, bool lastParam)
        {
            bw.WriteInt32(Version);
            bw.WriteInt32(Entries.Count + 1);
            bw.ReserveInt64("ParamNameOffset");
            for (int i = 0; i < Entries.Count; i++)
                bw.ReserveInt64($"EntryOffset[{i}]");
            bw.ReserveInt64("NextParamOffset");

            bw.FillInt64("ParamNameOffset", bw.Position);
            bw.WriteUTF16(Name, true);

            for (int i = 0; i < Entries.Count; i++)
            {
                bw.Pad(8);
                bw.FillInt64($"EntryOffset[{i}]", bw.Position);
                Entries[i].Write(bw);
            }

            bw.Pad(8);
            if (lastParam)
                bw.FillInt64("NextParamOffset", 0);
            else
                bw.FillInt64("NextParamOffset", bw.Position);
        }

        public override string ToString()
            => $"{GetType().Name}({nameof(Version)}={Version}, {nameof(Name)}=\"{Name}\", {nameof(Entries)}.Count={Entries.Count})";
    }

    private static T FindEntry<T>(IList<T> search, int index) where T : class
    {
        if (index == -1)
            return null;

        return search[index];
    }

    private static T[] FindEntries<T>(IList<T> search, IList<int> indices) where T : class
    {
        var entries = new T[indices.Count];
        for (int i = 0; i < indices.Count; i++)
            entries[i] = FindEntry(search, indices[i]);
        return entries;
    }

    private static T[] FindEntriesShort<T>(IList<T> search, IList<short> indices) where T : class
    {
        var entries = new T[indices.Count];
        for (int i = 0; i < indices.Count; i++)
            entries[i] = FindEntry(search, indices[i]);
        return entries;
    }

    private static int FindIndex<T>(IList<T> search, T entry) where T : class
    {
        if (entry == null)
            return -1;

        int index = search.IndexOf(entry);
        if (index == -1)
            throw new InvalidDataException();
        return index;
    }

    private static short FindIndexShort<T>(IList<T> search, T entry) where T : class
        => (short)FindIndex(search, entry);

    private static int[] FindIndices<T>(IList<T> search, IList<T> entries) where T : class
    {
        var indices = new int[entries.Count];
        for (int i = 0; i < entries.Count; i++)
            indices[i] = FindIndex(search, entries[i]);
        return indices;
    }

    private static short[] FindIndicesShort<T>(IList<T> search, IList<T> entries) where T : class
    {
        var indices = new short[entries.Count];
        for (int i = 0; i < entries.Count; i++)
            indices[i] = FindIndexShort(search, entries[i]);
        return indices;
    }
}
