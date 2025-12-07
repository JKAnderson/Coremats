namespace Coremats;

public partial class MSB_NR
{
    public enum PointType : uint
    {
        EnvMapPoint = 2,
        RespawnPoint = 3,
        Sound = 4,
        Sfx = 5,
        WindSfx = 6,
        ReturnPoint = 8,
        EnvMapEffectBox = 17,
        MapConnection = 21,
        MufflingBox = 28,
        MufflingPortal = 29,
        SoundOverride = 30,
        PatrolPoint = 32,
        MapPoint = 33,
        MapInfoOverride = 35,
        MassPlacement = 37,
        HitSetting = 40,
        WeatherAssetGeneration = 42,
        MidRangeEnvMapOutput = 44,
        BigJump = 46,
        SoundDummy = 48,
        FallPreventionOverride = 49,
        SmallBaseAttach = 54,
        BirdRoute = 55,
        ClearInfo = 56,
        RespawnOverride = 57,
        UserEdgeRemovalInner = 58,
        UserEdgeRemovalOuter = 59,
        BigJumpSealable = 60,
        Other = ~0u,
    }

    public class PointParam : Param<Point>
    {
        public override string Name => "POINT_PARAM_ST";

        public PointParam() : base() { }

        internal PointParam(BinaryReaderEx br, bool lastParam) : base(br, lastParam, (br, version) => new(br, version)) { }

        internal void Postprocess(MSB_NR msb)
        {
            Entries.ForEach(p => p.Deindex(msb));
        }

        internal void PreprocessStage1()
        {
            Entries = [.. Entries.OrderBy(p => p.Type)];
            foreach (var type in Entries.GroupBy(p => p.Type))
            {
                var entries = type.ToArray();
                for (int i = 0; i < entries.Length; i++)
                    entries[i]._typeIndex = i;
            }
        }

        internal void PreprocessStage2(MSB_NR msb)
        {
            Entries.ForEach(p => p.Reindex(msb));
        }
    }

    public class Point : Entry
    {
        public string Name { get; set; }
        public PointType Type { get; set; }
        internal int _typeIndex;
        public PointFormType FormType { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Angle { get; set; }
        public int PointNo { get; set; }
        private List<short> _parentPointIndices;
        public List<Point> ParentPoints { get; set; }
        private List<short> _childPointIndices;
        public List<Point> ChildPoints { get; set; }
        public PointForm Form { get; set; }
        public PointCommon Common { get; set; }
        public PointTypeData TypeData { get; set; }
        public PointStruct98 Struct98 { get; set; }

        public Point()
        {
            Name = "";
            PointNo = -1;
            ParentPoints = [];
            ChildPoints = [];
            Common = new();
            Struct98 = new();
        }

        internal Point(BinaryReaderEx br, int version)
        {
            long start = br.Position;

            Name = br.GetUTF16(start + br.ReadInt64());
            Type = br.ReadEnum32<PointType>();
            _typeIndex = br.ReadInt32();
            FormType = br.ReadEnum32<PointFormType>();
            Position = br.ReadVector3();
            Angle = br.ReadVector3();
            PointNo = br.ReadInt32();
            long parentListOffset = br.ReadInt64();
            long childListOffset = br.ReadInt64();
            br.AssertInt32(0);
            br.AssertInt32(-1);
            long formOffset = br.ReadInt64();
            long commonOffset = br.ReadInt64();
            long typeOffset = br.ReadInt64();
            long offset98 = br.ReadInt64();

            br.Position = start + parentListOffset;
            short parentCount = br.ReadInt16();
            _parentPointIndices = [.. br.ReadInt16s(parentCount)];

            br.Position = start + childListOffset;
            short childCount = br.ReadInt16();
            _childPointIndices = [.. br.ReadInt16s(childCount)];

            if (formOffset != 0)
            {
                br.Position = start + formOffset;
                Form = PointForm.Read(br, FormType);
            }

            br.Position = start + commonOffset;
            Common = new(br);

            if (typeOffset != 0)
            {
                br.Position = start + typeOffset;
                TypeData = Type switch
                {
                    PointType.EnvMapPoint => new PointEnvMapPointData(br),
                    PointType.RespawnPoint => new PointRespawnPointData(br),
                    PointType.Sound => new PointSoundData(br),
                    PointType.Sfx => new PointSfxData(br),
                    PointType.WindSfx => new PointWindSfxData(br),
                    PointType.ReturnPoint => new PointReturnPointData(br),
                    PointType.EnvMapEffectBox => new PointEnvMapEffectBoxData(br),
                    PointType.MapConnection => new PointMapConnectionData(br),
                    PointType.MufflingBox => new PointMufflingBoxData(br),
                    PointType.MufflingPortal => new PointMufflingPortalData(br),
                    PointType.SoundOverride => new PointSoundOverrideData(br),
                    PointType.PatrolPoint => new PointPatrolPointData(br),
                    PointType.MapPoint => new PointMapPointData(br),
                    PointType.MapInfoOverride => new PointMapInfoOverrideData(br),
                    PointType.MassPlacement => new PointMassPlacementData(br),
                    PointType.HitSetting => new PointHitSettingData(br),
                    PointType.WeatherAssetGeneration => new PointWeatherAssetGenerationData(br),
                    PointType.BigJump => new PointBigJumpData(br),
                    PointType.SoundDummy => new PointSoundDummyData(br),
                    PointType.FallPreventionOverride => new PointFallPreventionOverrideData(br),
                    PointType.SmallBaseAttach => new PointSmallBaseAttachData(br),
                    PointType.BirdRoute => new PointBirdRouteData(br),
                    PointType.RespawnOverride => new PointRespawnOverrideData(br),
                    PointType.UserEdgeRemovalInner => new PointUserEdgeRemovalInnerData(br),
                    PointType.UserEdgeRemovalOuter => new PointUserEdgeRemovalOuterData(br),
                    PointType.BigJumpSealable => new PointBigJumpSealableData(br),
                    _ => throw new NotImplementedException(),
                };
            }

            br.Position = start + offset98;
            Struct98 = new(br);
        }

        internal void Deindex(MSB_NR msb)
        {
            ParentPoints = [.. FindEntriesShort(msb.Points.Entries, _parentPointIndices)];
            ChildPoints = [.. FindEntriesShort(msb.Points.Entries, _childPointIndices)];
            Form?.Deindex(msb);
            Common.Deindex(msb);
            TypeData?.Deindex(msb);
        }

        internal void Reindex(MSB_NR msb)
        {
            _parentPointIndices = [.. FindIndicesShort(msb.Points.Entries, ParentPoints)];
            _childPointIndices = [.. FindIndicesShort(msb.Points.Entries, ChildPoints)];
            Form?.Reindex(msb);
            Common.Reindex(msb);
            TypeData?.Reindex(msb);
        }

        internal override void Write(BinaryWriterEx bw, int version)
        {
            long start = bw.Position;

            bw.ReserveInt64("PointNameOffset");
            bw.WriteUInt32((uint)Type);
            bw.WriteInt32(_typeIndex);
            bw.WriteUInt32((uint)FormType);
            bw.WriteVector3(Position);
            bw.WriteVector3(Angle);
            bw.WriteInt32(PointNo);
            bw.ReserveInt64("PointParentListOffset");
            bw.ReserveInt64("PointChildListOffset");
            bw.WriteInt32(0);
            bw.WriteInt32(-1);
            bw.ReserveInt64("PointFormOffset");
            bw.ReserveInt64("PointCommonOffset");
            bw.ReserveInt64("PointTypeOffset");
            bw.ReserveInt64("PointOffset98");

            bw.FillInt64("PointNameOffset", bw.Position - start);
            bw.WriteUTF16(Name, true);

            bw.Pad(4);
            bw.FillInt64("PointParentListOffset", bw.Position - start);
            bw.WriteInt16((short)_parentPointIndices.Count);
            bw.WriteInt16s(_parentPointIndices);

            bw.Pad(4);
            bw.FillInt64("PointChildListOffset", bw.Position - start);
            bw.WriteInt16((short)_childPointIndices.Count);
            bw.WriteInt16s(_childPointIndices);

            bw.Pad(8);
            bw.FillInt64("PointFormOffset", Form == null ? 0 : bw.Position - start);
            Form?.Write(bw);

            bw.FillInt64("PointCommonOffset", bw.Position - start);
            Common.Write(bw);

            if (Type == PointType.Other || Type >= PointType.MufflingBox)
                bw.Pad(8);
            bw.FillInt64("PointTypeOffset", TypeData == null ? 0 : bw.Position - start);
            TypeData?.Write(bw);

            if (Type != PointType.Other && Type < PointType.MufflingBox)
                bw.Pad(8);
            bw.FillInt64("PointOffset98", bw.Position - start);
            Struct98.Write(bw);
        }

        public override string ToString()
            => $"Point <{Type}> [{_typeIndex}] \"{Name}\"";
    }

    public class PointCommon
    {
        private int _partIndex;
        public Part Part { get; set; }
        public uint EntityId { get; set; }
        public sbyte Unk08 { get; set; }
        public int Unk0c { get; set; }
        public int Variation { get; set; }

        public PointCommon()
        {
            Unk08 = -1;
            Variation = -1;
        }

        internal PointCommon(BinaryReaderEx br)
        {
            _partIndex = br.ReadInt32();
            EntityId = br.ReadUInt32();
            Unk08 = br.ReadSByte();
            br.AssertByte(0);
            br.AssertInt16(0);
            Unk0c = br.ReadInt32();
            Variation = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Deindex(MSB_NR msb)
        {
            Part = FindEntry(msb.Parts.Entries, _partIndex);
        }

        internal void Reindex(MSB_NR msb)
        {
            _partIndex = FindIndex(msb.Parts.Entries, Part);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(_partIndex);
            bw.WriteUInt32(EntityId);
            bw.WriteSByte(Unk08);
            bw.WriteByte(0);
            bw.WriteInt16(0);
            bw.WriteInt32(Unk0c);
            bw.WriteInt32(Variation);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PointStruct98
    {
        public int Unk00 { get; set; }
        public int Unk04 { get; set; }
        public int Unk0c { get; set; }
        public int Unk10 { get; set; }

        public PointStruct98()
        {
            Unk00 = -1;
            Unk0c = -1;
            Unk10 = -1;
        }

        internal PointStruct98(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            Unk04 = br.ReadInt32();
            br.AssertInt32(0);
            Unk0c = br.ReadInt32();
            Unk10 = br.ReadInt32();
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
            bw.WriteInt32(Unk10);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public abstract class PointTypeData
    {
        internal virtual void Deindex(MSB_NR msb) { }
        internal virtual void Reindex(MSB_NR msb) { }
        internal abstract void Write(BinaryWriterEx bw);
    }

    public class PointEnvMapPointData : PointTypeData
    {
        public float Unk00 { get; set; }
        public int Unk04 { get; set; }
        public bool Unk0d { get; set; }
        public bool Unk0e { get; set; }
        public bool Unk0f { get; set; }
        public int Unk18 { get; set; }
        public int Unk20 { get; set; }
        public int Unk24 { get; set; }
        public int Unk28 { get; set; }
        public byte Unk2c { get; set; }
        public byte Unk2d { get; set; }

        public PointEnvMapPointData()
        {
            Unk00 = 1;
            Unk04 = 4;
            Unk0d = true;
            Unk0e = true;
            Unk0f = true;
        }

        internal PointEnvMapPointData(BinaryReaderEx br)
        {
            Unk00 = br.ReadSingle();
            Unk04 = br.ReadInt32();
            br.AssertInt32(-1);
            br.AssertByte(0);
            Unk0d = br.ReadBoolean();
            Unk0e = br.ReadBoolean();
            Unk0f = br.ReadBoolean();
            br.AssertSingle(1);
            br.AssertSingle(1);
            Unk18 = br.ReadInt32();
            br.AssertInt32(0);
            Unk20 = br.ReadInt32();
            Unk24 = br.ReadInt32();
            Unk28 = br.ReadInt32();
            Unk2c = br.ReadByte();
            Unk2d = br.ReadByte();
            br.AssertInt16(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteSingle(Unk00);
            bw.WriteInt32(Unk04);
            bw.WriteInt32(-1);
            bw.WriteByte(0);
            bw.WriteBoolean(Unk0d);
            bw.WriteBoolean(Unk0e);
            bw.WriteBoolean(Unk0f);
            bw.WriteSingle(1);
            bw.WriteSingle(1);
            bw.WriteInt32(Unk18);
            bw.WriteInt32(0);
            bw.WriteInt32(Unk20);
            bw.WriteInt32(Unk24);
            bw.WriteInt32(Unk28);
            bw.WriteByte(Unk2c);
            bw.WriteByte(Unk2d);
            bw.WriteInt16(0);
        }
    }

    public class PointRespawnPointData : PointTypeData
    {
        public int Unk00 { get; set; }

        public PointRespawnPointData() { }

        internal PointRespawnPointData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteInt32(0);
        }
    }

    public class PointSoundData : PointTypeData
    {
        public int SoundId { get; set; }
        private int[] _childPointIndices;
        public Point[] ChildPoints { get; private set; }
        public bool Unk49 { get; set; }

        public PointSoundData()
        {
            ChildPoints = new Point[16];
        }

        internal PointSoundData(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            SoundId = br.ReadInt32();
            _childPointIndices = br.ReadInt32s(16);
            br.AssertByte(0);
            Unk49 = br.ReadBoolean();
            br.AssertInt16(0);
        }

        internal override void Deindex(MSB_NR msb)
        {
            ChildPoints = FindEntries(msb.Points.Entries, _childPointIndices);
        }

        internal override void Reindex(MSB_NR msb)
        {
            _childPointIndices = FindIndices(msb.Points.Entries, ChildPoints);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
            bw.WriteInt32(SoundId);
            bw.WriteInt32s(_childPointIndices);
            bw.WriteByte(0);
            bw.WriteBoolean(Unk49);
            bw.WriteInt16(0);
        }
    }

    public class PointSfxData : PointTypeData
    {
        public int EffectId { get; set; }
        public bool Unk04 { get; set; }
        public bool Unk05 { get; set; }

        public PointSfxData() { }

        internal PointSfxData(BinaryReaderEx br)
        {
            EffectId = br.ReadInt32();
            Unk04 = br.ReadBoolean();
            Unk05 = br.ReadBoolean();
            br.AssertInt16(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(EffectId);
            bw.WriteBoolean(Unk04);
            bw.WriteBoolean(Unk05);
            bw.WriteInt16(0);
        }
    }

    public class PointWindSfxData : PointTypeData
    {
        public int EffectId { get; set; }
        private int _windAreaIndex;
        public Point WindArea { get; set; }

        public PointWindSfxData()
        {
            EffectId = 808006;
        }

        internal PointWindSfxData(BinaryReaderEx br)
        {
            EffectId = br.ReadInt32();
            _windAreaIndex = br.ReadInt32();
            br.AssertSingle(-1);
        }

        internal override void Deindex(MSB_NR msb)
        {
            WindArea = FindEntry(msb.Points.Entries, _windAreaIndex);
        }

        internal override void Reindex(MSB_NR msb)
        {
            _windAreaIndex = FindIndex(msb.Points.Entries, WindArea);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(EffectId);
            bw.WriteInt32(_windAreaIndex);
            bw.WriteSingle(-1);
        }
    }

    public class PointReturnPointData : PointTypeData
    {
        public PointReturnPointData() { }

        internal PointReturnPointData(BinaryReaderEx br)
        {
            br.AssertInt32(-1);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(-1);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PointEnvMapEffectBoxData : PointTypeData
    {
        public float Unk00 { get; set; }
        public float Unk04 { get; set; }
        public bool Unk08 { get; set; }
        public short Unk0a { get; set; }
        public float Unk24 { get; set; }
        public float Unk28 { get; set; }
        public short Unk30 { get; set; }
        public bool Unk33 { get; set; }
        public short Unk34 { get; set; }
        public short Unk36 { get; set; }

        public PointEnvMapEffectBoxData()
        {
            Unk0a = -1;
            Unk24 = 1;
            Unk28 = 1;
            Unk30 = -1;
            Unk33 = true;
            Unk36 = 1;
        }

        internal PointEnvMapEffectBoxData(BinaryReaderEx br)
        {
            Unk00 = br.ReadSingle();
            Unk04 = br.ReadSingle();
            Unk08 = br.ReadBoolean();
            br.AssertByte(10);
            Unk0a = br.ReadInt16();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            Unk24 = br.ReadSingle();
            Unk28 = br.ReadSingle();
            br.AssertInt16(0);
            br.AssertByte(1);
            br.AssertByte(1);
            Unk30 = br.ReadInt16();
            br.AssertByte(0);
            Unk33 = br.ReadBoolean();
            Unk34 = br.ReadInt16();
            Unk36 = br.ReadInt16();
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteSingle(Unk00);
            bw.WriteSingle(Unk04);
            bw.WriteBoolean(Unk08);
            bw.WriteByte(10);
            bw.WriteInt16(Unk0a);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteSingle(Unk24);
            bw.WriteSingle(Unk28);
            bw.WriteInt16(0);
            bw.WriteByte(1);
            bw.WriteByte(1);
            bw.WriteInt16(Unk30);
            bw.WriteByte(0);
            bw.WriteBoolean(Unk33);
            bw.WriteInt16(Unk34);
            bw.WriteInt16(Unk36);
            bw.WriteInt32(0);
        }
    }

    public class PointMapConnectionData : PointTypeData
    {
        public sbyte[] MapId { get; private set; }

        public PointMapConnectionData()
        {
            MapId = new sbyte[4];
        }

        internal PointMapConnectionData(BinaryReaderEx br)
        {
            MapId = br.ReadSBytes(4);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteSBytes(MapId);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PointMufflingBoxData : PointTypeData
    {
        public int Unk00 { get; set; }

        public PointMufflingBoxData() { }

        internal PointMufflingBoxData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt64(0x20);

            br.AssertInt32(0);
            br.AssertSingle(100);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertSingle(100);
            br.AssertInt32(0);
            br.AssertSingle(-1);
            br.AssertSingle(-1);
            br.AssertSingle(-1);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt64(0x20);

            bw.WriteInt32(0);
            bw.WriteSingle(100);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteSingle(100);
            bw.WriteInt32(0);
            bw.WriteSingle(-1);
            bw.WriteSingle(-1);
            bw.WriteSingle(-1);
        }
    }

    public class PointMufflingPortalData : PointTypeData
    {
        public int Unk00 { get; set; }

        public PointMufflingPortalData() { }

        internal PointMufflingPortalData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt64(0x20);

            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(-1);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt64(0x20);

            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(-1);
        }
    }

    public class PointSoundOverrideData : PointTypeData
    {
        public sbyte Unk00 { get; set; }
        public byte Unk01 { get; set; }
        public byte Unk02 { get; set; }
        public sbyte Unk03 { get; set; }
        public int Unk04 { get; set; }
        public short Unk08 { get; set; }
        public short Unk0a { get; set; }

        public PointSoundOverrideData()
        {
            Unk00 = -1;
            Unk03 = -1;
            Unk04 = -1;
            Unk08 = -1;
            Unk0a = -1;
        }

        internal PointSoundOverrideData(BinaryReaderEx br)
        {
            Unk00 = br.ReadSByte();
            Unk01 = br.ReadByte();
            Unk02 = br.ReadByte();
            Unk03 = br.ReadSByte();
            Unk04 = br.ReadInt32();
            Unk08 = br.ReadInt16();
            Unk0a = br.ReadInt16();
            br.AssertSByte(-1);
            br.AssertByte(0);
            br.AssertInt16(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteSByte(Unk00);
            bw.WriteByte(Unk01);
            bw.WriteByte(Unk02);
            bw.WriteSByte(Unk03);
            bw.WriteInt32(Unk04);
            bw.WriteInt16(Unk08);
            bw.WriteInt16(Unk0a);
            bw.WriteSByte(-1);
            bw.WriteByte(0);
            bw.WriteInt16(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PointPatrolPointData : PointTypeData
    {
        public int Unk00 { get; set; }

        public PointPatrolPointData()
        {
            Unk00 = -1;
        }

        internal PointPatrolPointData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
        }
    }

    public class PointMapPointData : PointTypeData
    {
        public int Unk00 { get; set; }

        public PointMapPointData()
        {
            Unk00 = -1;
        }

        internal PointMapPointData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            br.AssertInt32(-1);
            br.AssertSingle(-1);
            br.AssertSingle(-1);
            br.AssertInt32(-1);
            br.AssertSingle(-1);
            br.AssertSingle(-1);
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteInt32(-1);
            bw.WriteSingle(-1);
            bw.WriteSingle(-1);
            bw.WriteInt32(-1);
            bw.WriteSingle(-1);
            bw.WriteSingle(-1);
            bw.WriteInt32(0);
        }
    }

    public class PointMapInfoOverrideData : PointTypeData
    {
        public int Unk00 { get; set; }

        public PointMapInfoOverrideData()
        {
            Unk00 = 4000;
        }

        internal PointMapInfoOverrideData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            br.AssertInt32(-1);
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
            bw.WriteInt32(-1);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PointMassPlacementData : PointTypeData
    {
        public int Unk20 { get; set; }
        public int Unk58 { get; set; }

        public PointMassPlacementData()
        {
            Unk20 = -1;
            Unk58 = -1;
        }

        internal PointMassPlacementData(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            Unk20 = br.ReadInt32();
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            Unk58 = br.ReadInt32();
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(Unk20);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(Unk58);
            bw.WriteInt32(0);
        }
    }

    public class PointHitSettingData : PointTypeData
    {
        public int Unk00 { get; set; }

        public PointHitSettingData()
        {
            Unk00 = -1;
        }

        internal PointHitSettingData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
        }
    }

    public class PointWeatherAssetGenerationData : PointTypeData
    {
        public PointWeatherAssetGenerationData() { }

        internal PointWeatherAssetGenerationData(BinaryReaderEx br)
        {
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
        }
    }

    public class PointBigJumpData : PointTypeData
    {
        public float Unk00 { get; set; }
        public int Unk04 { get; set; }
        public int Unk08 { get; set; }

        public PointBigJumpData()
        {
            Unk00 = 10;
            Unk04 = 807100;
            Unk08 = 200;
        }

        internal PointBigJumpData(BinaryReaderEx br)
        {
            Unk00 = br.ReadSingle();
            Unk04 = br.ReadInt32();
            Unk08 = br.ReadInt32();
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteSingle(Unk00);
            bw.WriteInt32(Unk04);
            bw.WriteInt32(Unk08);
        }
    }

    public class PointSoundDummyData : PointTypeData
    {
        public int Unk00 { get; set; }

        public PointSoundDummyData() { }

        internal PointSoundDummyData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteInt32(0);
        }
    }

    public class PointFallPreventionOverrideData : PointTypeData
    {
        public PointFallPreventionOverrideData() { }

        internal PointFallPreventionOverrideData(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PointSmallBaseAttachData : PointTypeData
    {
        public PointSmallBaseAttachData() { }

        internal PointSmallBaseAttachData(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PointBirdRouteData : PointTypeData
    {
        public int Unk00 { get; set; }

        public PointBirdRouteData() { }

        internal PointBirdRouteData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteInt32(0);
        }
    }

    public class PointRespawnOverrideData : PointTypeData
    {
        public int Unk00 { get; set; }

        public PointRespawnOverrideData() { }

        internal PointRespawnOverrideData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
        }
    }

    public class PointUserEdgeRemovalInnerData : PointTypeData
    {
        public PointUserEdgeRemovalInnerData() { }

        internal PointUserEdgeRemovalInnerData(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PointUserEdgeRemovalOuterData : PointTypeData
    {
        public PointUserEdgeRemovalOuterData() { }

        internal PointUserEdgeRemovalOuterData(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PointBigJumpSealableData : PointTypeData
    {
        public float Unk00 { get; set; }
        public int Unk04 { get; set; }
        public int Unk08 { get; set; }
        public int Unk0c { get; set; }
        public int Unk10 { get; set; }
        public int Unk14 { get; set; }

        public PointBigJumpSealableData()
        {
            Unk00 = 20;
            Unk04 = 807103;
            Unk0c = -1;
            Unk10 = 200;
            Unk14 = -1;
        }

        internal PointBigJumpSealableData(BinaryReaderEx br)
        {
            Unk00 = br.ReadSingle();
            Unk04 = br.ReadInt32();
            Unk08 = br.ReadInt32();
            Unk0c = br.ReadInt32();
            Unk10 = br.ReadInt32();
            Unk14 = br.ReadInt32();
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteSingle(Unk00);
            bw.WriteInt32(Unk04);
            bw.WriteInt32(Unk08);
            bw.WriteInt32(Unk0c);
            bw.WriteInt32(Unk10);
            bw.WriteInt32(Unk14);
        }
    }
}
