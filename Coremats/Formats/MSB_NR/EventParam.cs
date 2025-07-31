namespace Coremats;

public partial class MSB_NR
{
    public enum EventType : uint
    {
        Treasure = 4,
        Generator = 5,
        ObjAct = 7,
        PlatoonInfo = 15,
        PatrolRoute = 20,
        Riding = 21,
        BirdRoute = 25,
        TalkInfo = 26,
        TeamFight = 27,
        Other = ~0u,
    }

    public class EventParam : Param<Event>
    {
        public override string Name => "EVENT_PARAM_ST";

        public EventParam() : base() { }

        internal EventParam(BinaryReaderEx br, bool lastParam) : base(br, lastParam, br => new(br)) { }

        internal void Postprocess(MSB_NR msb)
        {
            Entries.ForEach(e => e.Deindex(msb));
        }

        internal void PreprocessStage1()
        {
            Entries = [.. Entries.OrderBy(e => e.Type)];
            foreach (var type in Entries.GroupBy(e => e.Type))
            {
                var entries = type.ToArray();
                for (int i = 0; i < entries.Length; i++)
                    entries[i]._typeIndex = i;
            }
        }

        internal void PreprocessStage2(MSB_NR msb)
        {
            Entries.ForEach(e => e.Reindex(msb));
        }
    }

    public class Event : Entry
    {
        public string Name { get; set; }
        public int EventNo { get; set; }
        public EventType Type { get; set; }
        internal int _typeIndex;
        public EventCommon Common { get; set; }
        public EventTypeData TypeData { get; set; }
        public EventStruct28 Struct28 { get; set; }

        public Event()
        {
            Name = "";
            EventNo = -1;
            Common = new();
            Struct28 = new();
        }

        internal Event(BinaryReaderEx br)
        {
            long start = br.Position;

            Name = br.GetUTF16(start + br.ReadInt64());
            EventNo = br.ReadInt32();
            Type = br.ReadEnum32<EventType>();
            _typeIndex = br.ReadInt32();
            br.AssertInt32(0);
            long commonOffset = br.ReadInt64();
            long typeOffset = br.ReadInt64();
            long offset28 = br.ReadInt64();

            br.Position = start + commonOffset;
            Common = new(br);

            if (typeOffset != 0)
            {
                br.Position = start + typeOffset;
                TypeData = Type switch
                {
                    EventType.Treasure => new EventTreasureData(br),
                    EventType.Generator => new EventGeneratorData(br),
                    EventType.ObjAct => new EventObjActData(br),
                    EventType.PlatoonInfo => new EventPlatoonInfoData(br),
                    EventType.PatrolRoute => new EventPatrolRouteData(br),
                    EventType.Riding => new EventRidingData(br),
                    EventType.BirdRoute => new EventBirdRouteData(br),
                    EventType.TalkInfo => new EventTalkInfoData(br),
                    EventType.TeamFight => new EventTeamFightData(br),
                    _ => throw new NotImplementedException(),
                };
            }

            br.Position = start + offset28;
            Struct28 = new(br);
        }

        internal void Deindex(MSB_NR msb)
        {
            Common.Deindex(msb);
            TypeData?.Deindex(msb);
        }

        internal void Reindex(MSB_NR msb)
        {
            Common.Reindex(msb);
            TypeData?.Reindex(msb);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            long start = bw.Position;

            bw.ReserveInt64("EventNameOffset");
            bw.WriteInt32(EventNo);
            bw.WriteUInt32((uint)Type);
            bw.WriteInt32(_typeIndex);
            bw.WriteInt32(0);
            bw.ReserveInt64("EventCommonOffset");
            bw.ReserveInt64("EventTypeOffset");
            bw.ReserveInt64("EventOffset28");

            bw.FillInt64("EventNameOffset", bw.Position - start);
            bw.WriteUTF16(Name, true);

            bw.Pad(8);
            bw.FillInt64("EventCommonOffset", bw.Position - start);
            Common.Write(bw);

            bw.FillInt64("EventTypeOffset", TypeData == null ? 0 : bw.Position - start);
            TypeData?.Write(bw);

            bw.FillInt64("EventOffset28", bw.Position - start);
            Struct28.Write(bw);
        }

        public override string ToString()
            => $"Event <{Type}> [{_typeIndex}] \"{Name}\"";
    }

    public class EventCommon
    {
        private int _pointIndex;
        public Point Point { get; set; }
        public uint EntityId { get; set; }
        public sbyte Unk0c { get; set; }

        public EventCommon()
        {
            Unk0c = -1;
        }

        internal EventCommon(BinaryReaderEx br)
        {
            br.AssertInt32(-1);
            _pointIndex = br.ReadInt32();
            EntityId = br.ReadUInt32();
            Unk0c = br.ReadSByte();
            br.AssertByte(0);
            br.AssertInt16(0);
        }

        internal void Deindex(MSB_NR msb)
        {
            Point = FindEntry(msb.Points.Entries, _pointIndex);
        }

        internal void Reindex(MSB_NR msb)
        {
            _pointIndex = FindIndex(msb.Points.Entries, Point);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(-1);
            bw.WriteInt32(_pointIndex);
            bw.WriteUInt32(EntityId);
            bw.WriteSByte(Unk0c);
            bw.WriteByte(0);
            bw.WriteInt16(0);
        }
    }

    public class EventStruct28
    {
        public int Unk00 { get; set; }
        public int Unk04 { get; set; }
        public int Unk0c { get; set; }

        public EventStruct28()
        {
            Unk00 = -1;
            Unk0c = -1;
        }

        internal EventStruct28(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            Unk04 = br.ReadInt32();
            br.AssertInt32(0);
            Unk0c = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteInt32(Unk04);
            bw.WriteInt32(0);
            bw.WriteInt32(Unk0c);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public abstract class EventTypeData
    {
        internal virtual void Deindex(MSB_NR msb) { }
        internal virtual void Reindex(MSB_NR msb) { }
        internal abstract void Write(BinaryWriterEx bw);
    }

    public class EventTreasureData : EventTypeData
    {
        private int _partIndex;
        public Part Part { get; set; }
        public int ItemLotParamId { get; set; }
        public byte Unk40 { get; set; }
        public bool Unk41 { get; set; }
        public int Unk44 { get; set; }

        public EventTreasureData()
        {
            ItemLotParamId = -1;
            Unk40 = 1;
            Unk44 = -1;
        }

        internal EventTreasureData(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            br.AssertInt32(0);
            _partIndex = br.ReadInt32();
            br.AssertInt32(0);
            ItemLotParamId = br.ReadInt32();
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(0);
            br.AssertInt32(-1);
            Unk40 = br.ReadByte();
            Unk41 = br.ReadBoolean();
            br.AssertInt16(0);
            Unk44 = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Deindex(MSB_NR msb)
        {
            Part = FindEntry(msb.Parts.Entries, _partIndex);
        }

        internal override void Reindex(MSB_NR msb)
        {
            _partIndex = FindIndex(msb.Parts.Entries, Part);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(_partIndex);
            bw.WriteInt32(0);
            bw.WriteInt32(ItemLotParamId);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(0);
            bw.WriteInt32(-1);
            bw.WriteByte(Unk40);
            bw.WriteBoolean(Unk41);
            bw.WriteInt16(0);
            bw.WriteInt32(Unk44);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class EventGeneratorData : EventTypeData
    {
        public byte MaxNum { get; set; }
        public byte GenType { get; set; }
        public short LimitNum { get; set; }
        public short MinGenNum { get; set; }
        public short MaxGenNum { get; set; }
        public float MinInterval { get; set; }
        public float MaxInterval { get; set; }
        public sbyte InitialSpawnCount { get; set; }
        public float Unk14 { get; set; }
        public float Unk18 { get; set; }
        private int[] _pointIndices;
        public Point[] Points { get; private set; }
        private int[] _partIndices;
        public Part[] Parts { get; private set; }

        public EventGeneratorData()
        {
            MaxNum = 1;
            LimitNum = -1;
            MinGenNum = 1;
            MaxGenNum = 1;
            InitialSpawnCount = -1;
            Points = new Point[8];
            Parts = new Part[32];
        }

        internal EventGeneratorData(BinaryReaderEx br)
        {
            MaxNum = br.ReadByte();
            GenType = br.ReadByte();
            LimitNum = br.ReadInt16();
            MinGenNum = br.ReadInt16();
            MaxGenNum = br.ReadInt16();
            MinInterval = br.ReadSingle();
            MaxInterval = br.ReadSingle();
            InitialSpawnCount = br.ReadSByte();
            br.AssertByte(0);
            br.AssertInt16(0);
            Unk14 = br.ReadSingle();
            Unk18 = br.ReadSingle();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            _pointIndices = br.ReadInt32s(8);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            _partIndices = br.ReadInt32s(32);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Deindex(MSB_NR msb)
        {
            Points = FindEntries(msb.Points.Entries, _pointIndices);
            Parts = FindEntries(msb.Parts.Entries, _partIndices);
        }

        internal override void Reindex(MSB_NR msb)
        {
            _pointIndices = FindIndices(msb.Points.Entries, Points);
            _partIndices = FindIndices(msb.Parts.Entries, Parts);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteByte(MaxNum);
            bw.WriteByte(GenType);
            bw.WriteInt16(LimitNum);
            bw.WriteInt16(MinGenNum);
            bw.WriteInt16(MaxGenNum);
            bw.WriteSingle(MinInterval);
            bw.WriteSingle(MaxInterval);
            bw.WriteSByte(InitialSpawnCount);
            bw.WriteByte(0);
            bw.WriteInt16(0);
            bw.WriteSingle(Unk14);
            bw.WriteSingle(Unk18);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32s(_pointIndices);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32s(_partIndices);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class EventObjActData : EventTypeData
    {
        public uint EntityId { get; set; }
        private int _partIndex;
        public Part Part { get; set; }
        public int ObjActParamId { get; set; }
        public int Unk0c { get; set; }
        public uint EventFlagId { get; set; }

        public EventObjActData()
        {
            Unk0c = 5;
        }

        internal EventObjActData(BinaryReaderEx br)
        {
            EntityId = br.ReadUInt32();
            _partIndex = br.ReadInt32();
            ObjActParamId = br.ReadInt32();
            Unk0c = br.ReadInt32();
            EventFlagId = br.ReadUInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Deindex(MSB_NR msb)
        {
            Part = FindEntry(msb.Parts.Entries, _partIndex);
        }

        internal override void Reindex(MSB_NR msb)
        {
            _partIndex = FindIndex(msb.Parts.Entries, Part);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteUInt32(EntityId);
            bw.WriteInt32(_partIndex);
            bw.WriteInt32(ObjActParamId);
            bw.WriteInt32(Unk0c);
            bw.WriteUInt32(EventFlagId);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class EventPlatoonInfoData : EventTypeData
    {
        public int Unk00 { get; set; }
        public bool Unk04 { get; set; }
        private int[] _partIndices;
        public Part[] Parts { get; private set; }

        public EventPlatoonInfoData()
        {
            Unk00 = -1;
            Parts = new Part[32];
        }

        internal EventPlatoonInfoData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            Unk04 = br.ReadBoolean();
            br.AssertByte(0);
            br.AssertInt16(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            _partIndices = br.ReadInt32s(32);
        }

        internal override void Deindex(MSB_NR msb)
        {
            Parts = FindEntries(msb.Parts.Entries, _partIndices);
        }

        internal override void Reindex(MSB_NR msb)
        {
            _partIndices = FindIndices(msb.Parts.Entries, Parts);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteBoolean(Unk04);
            bw.WriteByte(0);
            bw.WriteInt16(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32s(_partIndices);
        }
    }

    public class EventPatrolRouteData : EventTypeData
    {
        public byte Unk00 { get; set; }
        private short[] _pointIndices;
        public Point[] Points { get; private set; }

        public EventPatrolRouteData()
        {
            Points = new Point[64];
        }

        internal EventPatrolRouteData(BinaryReaderEx br)
        {
            Unk00 = br.ReadByte();
            br.AssertByte(0);
            br.AssertByte(0);
            br.AssertByte(1);
            br.AssertInt32(-1);
            br.AssertInt32(0);
            br.AssertInt32(0);
            _pointIndices = br.ReadInt16s(64);
        }

        internal override void Deindex(MSB_NR msb)
        {
            Points = FindEntriesShort(msb.Points.Entries, _pointIndices);
        }

        internal override void Reindex(MSB_NR msb)
        {
            _pointIndices = FindIndicesShort(msb.Points.Entries, Points);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteByte(Unk00);
            bw.WriteByte(0);
            bw.WriteByte(0);
            bw.WriteByte(1);
            bw.WriteInt32(-1);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt16s(_pointIndices);
        }
    }

    public class EventRidingData : EventTypeData
    {
        private int _partIndex0;
        public Part Part0 { get; set; }
        private int _partIndex1;
        public Part Part1 { get; set; }

        public EventRidingData() { }

        internal EventRidingData(BinaryReaderEx br)
        {
            _partIndex0 = br.ReadInt32();
            _partIndex1 = br.ReadInt32();
        }

        internal override void Deindex(MSB_NR msb)
        {
            Part0 = FindEntry(msb.Parts.Entries, _partIndex0);
            Part1 = FindEntry(msb.Parts.Entries, _partIndex1);
        }

        internal override void Reindex(MSB_NR msb)
        {
            _partIndex0 = FindIndex(msb.Parts.Entries, Part0);
            _partIndex1 = FindIndex(msb.Parts.Entries, Part1);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(_partIndex0);
            bw.WriteInt32(_partIndex1);
        }
    }

    public class EventBirdRouteData : EventTypeData
    {
        public int Unk04 { get; set; }
        public int Unk08 { get; set; }
        public int Unk0c { get; set; }
        private short[] _pointIndices;
        public Point[] Points { get; private set; }

        public EventBirdRouteData()
        {
            Points = new Point[32];
        }

        internal EventBirdRouteData(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            Unk04 = br.ReadInt32();
            Unk08 = br.ReadInt32();
            Unk0c = br.ReadInt32();
            _pointIndices = br.ReadInt16s(32);
        }

        internal override void Deindex(MSB_NR msb)
        {
            Points = FindEntriesShort(msb.Points.Entries, _pointIndices);
        }

        internal override void Reindex(MSB_NR msb)
        {
            _pointIndices = FindIndicesShort(msb.Points.Entries, Points);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
            bw.WriteInt32(Unk04);
            bw.WriteInt32(Unk08);
            bw.WriteInt32(Unk0c);
            bw.WriteInt16s(_pointIndices);
        }
    }

    public class EventTalkInfoData : EventTypeData
    {
        public int Unk00 { get; set; }
        public int Unk04 { get; set; }
        public int Unk08 { get; set; }
        public int Unk0c { get; set; }
        public int Unk24 { get; set; }
        public int Unk28 { get; set; }
        public int Unk2c { get; set; }
        public byte Unk44 { get; set; }
        public bool Unk45 { get; set; }

        public EventTalkInfoData()
        {
            Unk0c = -1;
        }

        internal EventTalkInfoData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            Unk04 = br.ReadInt32();
            Unk08 = br.ReadInt32();
            Unk0c = br.ReadInt32();
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            Unk24 = br.ReadInt32();
            Unk28 = br.ReadInt32();
            Unk2c = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            Unk44 = br.ReadByte();
            Unk45 = br.ReadBoolean();
            br.AssertInt16(0);
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
            bw.WriteInt32(Unk00);
            bw.WriteInt32(Unk04);
            bw.WriteInt32(Unk08);
            bw.WriteInt32(Unk0c);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(Unk24);
            bw.WriteInt32(Unk28);
            bw.WriteInt32(Unk2c);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteByte(Unk44);
            bw.WriteBoolean(Unk45);
            bw.WriteInt16(0);
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
        }
    }

    public class EventTeamFightData : EventTypeData
    {
        public int Unk00 { get; set; }
        public int Unk04 { get; set; }
        public int Unk08 { get; set; }
        public int Unk0c { get; set; }

        public EventTeamFightData()
        {
            Unk08 = -1;
        }

        internal EventTeamFightData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            Unk04 = br.ReadInt32();
            Unk08 = br.ReadInt32();
            Unk0c = br.ReadInt32();
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteInt32(Unk04);
            bw.WriteInt32(Unk08);
            bw.WriteInt32(Unk0c);
        }
    }
}
