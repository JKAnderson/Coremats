namespace Coremats;

public partial class MSB_NR
{
    public enum RouteType : uint
    {
        Type3 = 3,
        Type4 = 4,
    }

    public class RouteParam : Param<Route>
    {
        public override string Name => "ROUTE_PARAM_ST";

        public RouteParam() : base() { }

        internal RouteParam(BinaryReaderEx br, bool lastParam) : base(br, lastParam, br => new(br)) { }

        internal void PreprocessStage1()
        {
            Entries = [.. Entries.OrderBy(r => r.Type)];
            foreach (var type in Entries.GroupBy(r => r.Type))
            {
                var entries = type.ToArray();
                for (int i = 0; i < entries.Length; i++)
                    entries[i]._typeIndex = i;
            }
        }
    }

    public class Route : Entry
    {
        public string Name { get; set; }
        public int ParentPointNo { get; set; }
        public int ChildPointNo { get; set; }
        public RouteType Type { get; set; }
        internal int _typeIndex;

        public Route()
        {
            Name = "";
        }

        internal Route(BinaryReaderEx br)
        {
            long start = br.Position;

            Name = br.GetUTF16(start + br.ReadInt64());
            ParentPointNo = br.ReadInt32();
            ChildPointNo = br.ReadInt32();
            Type = br.ReadEnum32<RouteType>();
            _typeIndex = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            long start = bw.Position;

            bw.ReserveInt64("RouteNameOffset");
            bw.WriteInt32(ParentPointNo);
            bw.WriteInt32(ChildPointNo);
            bw.WriteUInt32((uint)Type);
            bw.WriteInt32(_typeIndex);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);

            bw.FillInt64("RouteNameOffset", bw.Position - start);
            bw.WriteUTF16(Name, true);
        }

        public override string ToString()
            => $"Route <{Type}> [{_typeIndex}] \"{Name}\"";
    }
}
