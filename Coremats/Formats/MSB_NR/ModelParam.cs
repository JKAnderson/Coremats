namespace Coremats;

public partial class MSB_NR
{
    public enum ModelType : uint
    {
        Map = 0,
        Ene = 2,
        Player = 4,
        Hit = 5,
        DummyEne = 8,
        Invalid = 9,
        Geom = 10,
    }

    public class ModelParam : Param<Model>
    {
        public override string Name => "MODEL_PARAM_ST";

        public ModelParam() : base() { }

        internal ModelParam(BinaryReaderEx br, bool lastParam) : base(br, lastParam, (br, version) => new(br, version)) { }

        internal void PreprocessStage1()
        {
            Entries = [.. Entries.OrderBy(m => m.Type)];
            foreach (var type in Entries.GroupBy(m => m.Type))
            {
                var entries = type.ToArray();
                for (int i = 0; i < entries.Length; i++)
                    entries[i]._typeIndex = i;
            }
        }
    }

    public class Model : Entry
    {
        public string Name { get; set; }
        public ModelType Type { get; set; }
        internal int _typeIndex;
        public string File { get; set; }
        public int InstanceCount { get; set; }

        public Model()
        {
            Name = "";
            File = "";
        }

        internal Model(BinaryReaderEx br, int version)
        {
            long start = br.Position;

            Name = br.GetUTF16(start + br.ReadInt64());
            Type = br.ReadEnum32<ModelType>();
            _typeIndex = br.ReadInt32();
            File = br.GetUTF16(start + br.ReadInt64());
            InstanceCount = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw, int version)
        {
            long start = bw.Position;

            bw.ReserveInt64("ModelNameOffset");
            bw.WriteUInt32((uint)Type);
            bw.WriteInt32(_typeIndex);
            bw.ReserveInt64("ModelFileOffset");
            bw.WriteInt32(InstanceCount);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);

            bw.FillInt64("ModelNameOffset", bw.Position - start);
            bw.WriteUTF16(Name, true);

            bw.FillInt64("ModelFileOffset", bw.Position - start);
            bw.WriteUTF16(File, true);
        }

        public override string ToString()
            => $"Model <{Type}> [{_typeIndex}] \"{Name}\"";
    }
}
