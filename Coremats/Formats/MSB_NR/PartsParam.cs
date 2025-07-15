namespace Coremats;

public partial class MSB_NR
{
    public enum PartType : uint
    {
        Map = 0,
        Ene = 2,
        Player = 4,
        Hit = 5,
        DummyObj = 9,
        DummyEne = 10,
        ConnectHit = 11,
        Geom = 13,
    }

    public class PartsParam : Param<Part>
    {
        public override int Version => 75;
        public override string Name => "PARTS_PARAM_ST";

        public PartsParam() : base() { }

        internal PartsParam(BinaryReaderEx br, bool lastParam) : base(br, lastParam, br => new(br)) { }

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

    public class Part : Entry
    {
        public string Name { get; set; }
        public int PartNo { get; set; }
        public PartType Type { get; set; }
        internal int _typeIndex;
        private int _modelIndex;
        public Model Model { get; set; }
        public string File { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Angle { get; set; }
        public Vector3 Scale { get; set; }
        public int Unk44 { get; set; }
        public uint MapStudioLayer { get; set; }
        public PartStruct50 Struct50 { get; set; }
        public PartStruct58 Struct58 { get; set; }
        public PartCommon Common { get; set; }
        public PartTypeData TypeData { get; set; }
        public PartGparam Gparam { get; set; }
        public PartSceneGparam SceneGparam { get; set; }
        public PartGrass Grass { get; set; }
        public PartStruct88 Struct88 { get; set; }
        public PartStruct90 Struct90 { get; set; }
        public PartStruct98 Struct98 { get; set; }
        public PartStructa0 Structa0 { get; set; }
        public PartStructa8 Structa8 { get; set; }

        public Part()
        {
            Name = "";
            PartNo = -1;
            File = "";
            Scale = Vector3.One;
            MapStudioLayer = ~0u;
            Struct50 = new();
            Common = new();
            Struct88 = new();
            Struct98 = new();
        }

        public Part DeepClone()
        {
            var clone = (Part)MemberwiseClone();
            clone.Struct50 = Struct50.DeepClone();
            clone.Struct58 = Struct58?.DeepClone();
            clone.Common = Common.DeepClone();
            clone.TypeData = TypeData.DeepClone();
            clone.Gparam = Gparam?.DeepClone();
            clone.SceneGparam = SceneGparam?.DeepClone();
            clone.Grass = Grass?.DeepClone();
            clone.Struct88 = Struct88.DeepClone();
            clone.Struct90 = Struct90?.DeepClone();
            clone.Struct98 = Struct98.DeepClone();
            clone.Structa0 = Structa0?.DeepClone();
            clone.Structa8 = Structa8?.DeepClone();
            return clone;
        }

        internal Part(BinaryReaderEx br)
        {
            long start = br.Position;

            Name = br.GetUTF16(start + br.ReadInt64());
            PartNo = br.ReadInt32();
            Type = br.ReadEnum32<PartType>();
            _typeIndex = br.ReadInt32();
            _modelIndex = br.ReadInt32();
            File = br.GetUTF16(start + br.ReadInt64());
            Position = br.ReadVector3();
            Angle = br.ReadVector3();
            Scale = br.ReadVector3();
            Unk44 = br.ReadInt32();
            MapStudioLayer = br.ReadUInt32();
            br.AssertInt32(0);
            long offset50 = br.ReadInt64();
            long offset58 = br.ReadInt64();
            long commonOffset = br.ReadInt64();
            long typeOffset = br.ReadInt64();
            long gparamOffset = br.ReadInt64();
            long sceneGparamOffset = br.ReadInt64();
            long grassOffset = br.ReadInt64();
            long offset88 = br.ReadInt64();
            long offset90 = br.ReadInt64();
            long offset98 = br.ReadInt64();
            long offseta0 = br.ReadInt64();
            long offseta8 = br.ReadInt64();
            br.AssertInt64(0);
            br.AssertInt64(0);

            br.Position = start + offset50;
            Struct50 = new(br);

            if (offset58 != 0)
            {
                br.Position = start + offset58;
                Struct58 = new(br);
            }

            br.Position = start + commonOffset;
            Common = new(br);

            br.Position = start + typeOffset;
            TypeData = Type switch
            {
                PartType.Map => new PartMapData(br),
                PartType.Ene => new PartEneData(br),
                PartType.Player => new PartPlayerData(br),
                PartType.Hit => new PartHitData(br),
                PartType.DummyObj => new PartDummyObjData(br),
                PartType.DummyEne => new PartEneData(br),
                PartType.ConnectHit => new PartConnectHitData(br),
                PartType.Geom => new PartGeomData(br),
                _ => throw new NotImplementedException(),
            };

            if (gparamOffset != 0)
            {
                br.Position = start + gparamOffset;
                Gparam = new(br);
            }

            if (sceneGparamOffset != 0)
            {
                br.Position = start + sceneGparamOffset;
                SceneGparam = new(br);
            }

            if (grassOffset != 0)
            {
                br.Position = start + grassOffset;
                Grass = new(br);
            }

            br.Position = start + offset88;
            Struct88 = new(br);

            if (offset90 != 0)
            {
                br.Position = start + offset90;
                Struct90 = new(br);
            }

            br.Position = start + offset98;
            Struct98 = new(br);

            if (offseta0 != 0)
            {
                br.Position = start + offseta0;
                Structa0 = new(br);
            }

            if (offseta8 != 0)
            {
                br.Position = start + offseta8;
                Structa8 = new(br);
            }
        }

        internal void Deindex(MSB_NR msb)
        {
            Model = FindEntry(msb.Models.Entries, _modelIndex);
            TypeData?.Deindex(msb);
        }

        internal void Reindex(MSB_NR msb)
        {
            _modelIndex = FindIndex(msb.Models.Entries, Model);
            TypeData?.Reindex(msb);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            long start = bw.Position;

            bw.ReserveInt64("PartNameOffset");
            bw.WriteInt32(PartNo);
            bw.WriteUInt32((uint)Type);
            bw.WriteInt32(_typeIndex);
            bw.WriteInt32(_modelIndex);
            bw.ReserveInt64("PartFileOffset");
            bw.WriteVector3(Position);
            bw.WriteVector3(Angle);
            bw.WriteVector3(Scale);
            bw.WriteInt32(Unk44);
            bw.WriteUInt32(MapStudioLayer);
            bw.WriteInt32(0);
            bw.ReserveInt64("PartOffset50");
            bw.ReserveInt64("PartOffset58");
            bw.ReserveInt64("PartCommonOffset");
            bw.ReserveInt64("PartTypeOffset");
            bw.ReserveInt64("PartGparamOffset");
            bw.ReserveInt64("PartSceneGparamOffset");
            bw.ReserveInt64("PartGrassOffset");
            bw.ReserveInt64("PartOffset88");
            bw.ReserveInt64("PartOffset90");
            bw.ReserveInt64("PartOffset98");
            bw.ReserveInt64("PartOffseta0");
            bw.ReserveInt64("PartOffseta8");
            bw.WriteInt64(0);
            bw.WriteInt64(0);

            bw.FillInt64("PartNameOffset", bw.Position - start);
            bw.WriteUTF16(Name, true);

            bw.FillInt64("PartFileOffset", bw.Position - start);
            bw.WriteUTF16(File, true);

            bw.Pad(8);
            bw.FillInt64("PartOffset50", bw.Position - start);
            Struct50.Write(bw);

            bw.FillInt64("PartOffset58", Struct58 == null ? 0 : bw.Position - start);
            Struct58?.Write(bw);

            bw.FillInt64("PartCommonOffset", bw.Position - start);
            Common.Write(bw);

            bw.FillInt64("PartTypeOffset", bw.Position - start);
            TypeData?.Write(bw);

            bw.FillInt64("PartGparamOffset", Gparam == null ? 0 : bw.Position - start);
            Gparam?.Write(bw);

            bw.FillInt64("PartSceneGparamOffset", SceneGparam == null ? 0 : bw.Position - start);
            SceneGparam?.Write(bw);

            bw.FillInt64("PartGrassOffset", Grass == null ? 0 : bw.Position - start);
            Grass?.Write(bw);

            bw.FillInt64("PartOffset88", bw.Position - start);
            Struct88.Write(bw);

            bw.FillInt64("PartOffset90", Struct90 == null ? 0 : bw.Position - start);
            Struct90?.Write(bw);

            bw.FillInt64("PartOffset98", bw.Position - start);
            Struct98.Write(bw);

            bw.FillInt64("PartOffseta0", Structa0 == null ? 0 : bw.Position - start);
            Structa0?.Write(bw);

            bw.FillInt64("PartOffseta8", Structa8 == null ? 0 : bw.Position - start);
            Structa8?.Write(bw);
        }

        public override string ToString()
            => $"Part <{Type}> [{_typeIndex}] \"{Name}\"";
    }

    public class PartStruct50
    {
        public uint[] DispGroups { get; private set; }
        public uint[] DrawGroups { get; private set; }
        public uint[] HitMask { get; private set; }
        public bool Unkc0 { get; set; }
        public bool Unkc1 { get; set; }

        public PartStruct50()
        {
            DispGroups = new uint[8];
            DrawGroups = new uint[8];
            HitMask = new uint[32];
        }

        public PartStruct50 DeepClone()
        {
            var clone = (PartStruct50)MemberwiseClone();
            clone.DispGroups = (uint[])DispGroups.Clone();
            clone.DrawGroups = (uint[])DrawGroups.Clone();
            clone.HitMask = (uint[])HitMask.Clone();
            return clone;
        }

        internal PartStruct50(BinaryReaderEx br)
        {
            DispGroups = br.ReadUInt32s(8);
            DrawGroups = br.ReadUInt32s(8);
            HitMask = br.ReadUInt32s(32);
            Unkc0 = br.ReadBoolean();
            Unkc1 = br.ReadBoolean();
            br.AssertInt16(0);
            br.AssertInt16(-1);
            br.AssertInt16(0);
            for (int i = 0; i < 48; i++)
                br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteUInt32s(DispGroups);
            bw.WriteUInt32s(DrawGroups);
            bw.WriteUInt32s(HitMask);
            bw.WriteBoolean(Unkc0);
            bw.WriteBoolean(Unkc1);
            bw.WriteInt16(0);
            bw.WriteInt16(-1);
            bw.WriteInt16(0);
            for (int i = 0; i < 48; i++)
                bw.WriteInt32(0);
        }
    }

    public class PartStruct58
    {
        public int Unk00 { get; set; }
        public uint[] DispGroups { get; private set; }

        public PartStruct58()
        {
            Unk00 = -1;
            DispGroups = new uint[8];
        }

        public PartStruct58 DeepClone()
        {
            var clone = (PartStruct58)MemberwiseClone();
            clone.DispGroups = (uint[])DispGroups.Clone();
            return clone;
        }

        internal PartStruct58(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            DispGroups = br.ReadUInt32s(8);
            br.AssertInt16(0);
            br.AssertInt16(-1);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteUInt32s(DispGroups);
            bw.WriteInt16(0);
            bw.WriteInt16(-1);
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

    public class PartCommon
    {
        public uint EntityId { get; set; }
        public bool Unk04 { get; set; }
        public bool Unk05 { get; set; }
        public int Unk08 { get; set; }
        public bool Unk0c { get; set; }
        public bool Unk0d { get; set; }
        public bool Unk0e { get; set; }
        public bool Unk0f { get; set; }
        public bool Unk10 { get; set; }
        public bool Unk11 { get; set; }
        public bool Unk14 { get; set; }
        public bool Unk15 { get; set; }
        public bool Unk16 { get; set; }
        public bool Unk17 { get; set; }
        public bool Unk1a { get; set; }
        public byte Unk1b { get; set; }
        public uint[] EntityGroupIds { get; private set; }
        public short Unk3c { get; set; }
        public short Unk3e { get; set; }
        public int Unk40 { get; set; }
        public int Variation { get; set; }

        public PartCommon()
        {
            Unk05 = true;
            Unk11 = true;
            EntityGroupIds = new uint[8];
            Unk3c = -1;
            Variation = -1;
        }

        public PartCommon DeepClone()
        {
            var clone = (PartCommon)MemberwiseClone();
            clone.EntityGroupIds = (uint[])EntityGroupIds.Clone();
            return clone;
        }

        internal PartCommon(BinaryReaderEx br)
        {
            EntityId = br.ReadUInt32();
            Unk04 = br.ReadBoolean();
            Unk05 = br.ReadBoolean();
            br.AssertInt16(0);
            Unk08 = br.ReadInt32();
            Unk0c = br.ReadBoolean();
            Unk0d = br.ReadBoolean();
            Unk0e = br.ReadBoolean();
            Unk0f = br.ReadBoolean();
            Unk10 = br.ReadBoolean();
            Unk11 = br.ReadBoolean();
            br.AssertInt16(0);
            Unk14 = br.ReadBoolean();
            Unk15 = br.ReadBoolean();
            Unk16 = br.ReadBoolean();
            Unk17 = br.ReadBoolean();
            br.AssertInt16(0);
            Unk1a = br.ReadBoolean();
            Unk1b = br.ReadByte();
            EntityGroupIds = br.ReadUInt32s(8);
            Unk3c = br.ReadInt16();
            Unk3e = br.ReadInt16();
            Unk40 = br.ReadInt32();
            Variation = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteUInt32(EntityId);
            bw.WriteBoolean(Unk04);
            bw.WriteBoolean(Unk05);
            bw.WriteInt16(0);
            bw.WriteInt32(Unk08);
            bw.WriteBoolean(Unk0c);
            bw.WriteBoolean(Unk0d);
            bw.WriteBoolean(Unk0e);
            bw.WriteBoolean(Unk0f);
            bw.WriteBoolean(Unk10);
            bw.WriteBoolean(Unk11);
            bw.WriteInt16(0);
            bw.WriteBoolean(Unk14);
            bw.WriteBoolean(Unk15);
            bw.WriteBoolean(Unk16);
            bw.WriteBoolean(Unk17);
            bw.WriteInt16(0);
            bw.WriteBoolean(Unk1a);
            bw.WriteByte(Unk1b);
            bw.WriteUInt32s(EntityGroupIds);
            bw.WriteInt16(Unk3c);
            bw.WriteInt16(Unk3e);
            bw.WriteInt32(Unk40);
            bw.WriteInt32(Variation);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PartGparam
    {
        public int LightId { get; set; }
        public int FogId { get; set; }

        public PartGparam()
        {
            LightId = -1;
            FogId = -1;
        }

        public PartGparam DeepClone() => (PartGparam)MemberwiseClone();

        internal PartGparam(BinaryReaderEx br)
        {
            LightId = br.ReadInt32();
            FogId = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(LightId);
            bw.WriteInt32(FogId);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PartSceneGparam
    {
        public float Unk10 { get; set; }
        public sbyte Unk18 { get; set; }
        public sbyte Unk1d { get; set; }
        public short Unk20 { get; set; }

        public PartSceneGparam()
        {
            Unk10 = -1;
            Unk18 = -1;
            Unk1d = -1;
            Unk20 = -1;
        }

        public PartSceneGparam DeepClone() => (PartSceneGparam)MemberwiseClone();

        internal PartSceneGparam(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            Unk10 = br.ReadSingle();
            br.AssertInt32(0);
            Unk18 = br.ReadSByte();
            br.AssertSByte(-1);
            br.AssertInt16(-1);
            br.AssertSByte(-1);
            Unk1d = br.ReadSByte();
            br.AssertInt16(0);
            Unk20 = br.ReadInt16();
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
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteSingle(Unk10);
            bw.WriteInt32(0);
            bw.WriteSByte(Unk18);
            bw.WriteSByte(-1);
            bw.WriteInt16(-1);
            bw.WriteSByte(-1);
            bw.WriteSByte(Unk1d);
            bw.WriteInt16(0);
            bw.WriteInt16(Unk20);
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
        }
    }

    public class PartGrass
    {
        public int[] GrassTypes { get; private set; }

        public PartGrass()
        {
            GrassTypes = new int[6];
        }

        public PartGrass DeepClone()
        {
            var clone = (PartGrass)MemberwiseClone();
            clone.GrassTypes = (int[])GrassTypes.Clone();
            return clone;
        }

        internal PartGrass(BinaryReaderEx br)
        {
            GrassTypes = br.ReadInt32s(6);
            br.AssertInt32(-1);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32s(GrassTypes);
            bw.WriteInt32(-1);
            bw.WriteInt32(0);
        }
    }

    public class PartStruct88
    {
        public PartStruct88() { }

        public PartStruct88 DeepClone() => (PartStruct88)MemberwiseClone();

        internal PartStruct88(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
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

    public class PartStruct90
    {
        public int Unk00 { get; set; }

        public PartStruct90() { }

        public PartStruct90 DeepClone() => (PartStruct90)MemberwiseClone();

        internal PartStruct90(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PartStruct98
    {
        public int Unk00 { get; set; }
        public int Unk04 { get; set; }
        public int Unk0c { get; set; }
        public int Unk14 { get; set; }

        public PartStruct98()
        {
            Unk00 = -1;
            Unk0c = -1;
            Unk14 = -1;
        }

        public PartStruct98 DeepClone() => (PartStruct98)MemberwiseClone();

        internal PartStruct98(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            Unk04 = br.ReadInt32();
            br.AssertInt32(0);
            Unk0c = br.ReadInt32();
            br.AssertInt32(0);
            Unk14 = br.ReadInt32();
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
            bw.WriteInt32(Unk14);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PartStructa0
    {
        public PartStructa0() { }

        public PartStructa0 DeepClone() => (PartStructa0)MemberwiseClone();

        internal PartStructa0(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
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

    public class PartStructa8
    {
        public short Unk00 { get; set; }
        public short Unk02 { get; set; }
        public short Unk04 { get; set; }

        public PartStructa8()
        {
            Unk00 = -1;
            Unk02 = -1;
            Unk04 = -1;
        }

        public PartStructa8 DeepClone() => (PartStructa8)MemberwiseClone();

        internal PartStructa8(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt16();
            Unk02 = br.ReadInt16();
            Unk04 = br.ReadInt16();
            br.AssertInt16(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt16(Unk00);
            bw.WriteInt16(Unk02);
            bw.WriteInt16(Unk04);
            bw.WriteInt16(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public abstract class PartTypeData
    {
        public virtual PartTypeData DeepClone() => (PartTypeData)MemberwiseClone();
        internal virtual void Deindex(MSB_NR msb) { }
        internal virtual void Reindex(MSB_NR msb) { }
        internal abstract void Write(BinaryWriterEx bw);
    }

    public class PartMapData : PartTypeData
    {
        public PartMapData() { }

        internal PartMapData(BinaryReaderEx br)
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

    public class PartEneData : PartTypeData
    {
        public int NpcThinkParamId { get; set; }
        public int NpcParamId { get; set; }
        public int TalkId { get; set; }
        public bool Unk15 { get; set; }
        public short Unk16 { get; set; }
        public int CharaInitParamId { get; set; }
        private int _parentPartIndex;
        public Part ParentPart { get; set; }
        private short _patrolRouteIndex;
        public Event PatrolRoute { get; set; }
        public short Unk22 { get; set; }
        public int Unk28 { get; set; }
        public int Unk2c { get; set; }
        public int Unk30 { get; set; }
        public int Unk34 { get; set; }
        public int Unk38 { get; set; }
        public sbyte Unk3c { get; set; }
        public int Unk40 { get; set; }
        public int Unk44 { get; set; }
        public int Unk48 { get; set; }
        public int Unk4c { get; set; }
        public PartEneStruct78 Struct78 { get; set; }

        public PartEneData()
        {
            CharaInitParamId = -1;
            Unk22 = -1;
            Unk38 = -1;
            Unk3c = -1;
            Struct78 = new();
        }

        public override PartTypeData DeepClone()
        {
            var clone = (PartEneData)base.DeepClone();
            clone.Struct78 = Struct78.DeepClone();
            return clone;
        }

        internal PartEneData(BinaryReaderEx br)
        {
            long start = br.Position;

            br.AssertInt32(-1);
            br.AssertInt32(-1);
            NpcThinkParamId = br.ReadInt32();
            NpcParamId = br.ReadInt32();
            TalkId = br.ReadInt32();
            br.AssertByte(0);
            Unk15 = br.ReadBoolean();
            Unk16 = br.ReadInt16();
            CharaInitParamId = br.ReadInt32();
            _parentPartIndex = br.ReadInt32();
            _patrolRouteIndex = br.ReadInt16();
            Unk22 = br.ReadInt16();
            br.AssertInt32(-1);
            Unk28 = br.ReadInt32();
            Unk2c = br.ReadInt32();
            Unk30 = br.ReadInt32();
            Unk34 = br.ReadInt32();
            Unk38 = br.ReadInt32();
            Unk3c = br.ReadSByte();
            br.AssertByte(0);
            br.AssertInt16(0);
            Unk40 = br.ReadInt32();
            Unk44 = br.ReadInt32();
            Unk48 = br.ReadInt32();
            Unk4c = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt64(0);
            long offset78 = br.ReadInt64();

            br.Position = start + offset78;
            Struct78 = new(br);
        }

        internal override void Deindex(MSB_NR msb)
        {
            ParentPart = FindEntry(msb.Parts.Entries, _parentPartIndex);

            var patrolRoutes = msb.Events.Entries.Where(e => e.Type == EventType.PatrolRoute).ToArray();
            PatrolRoute = FindEntry(patrolRoutes, _patrolRouteIndex);
        }

        internal override void Reindex(MSB_NR msb)
        {
            _parentPartIndex = FindIndex(msb.Parts.Entries, ParentPart);

            var patrolRoutes = msb.Events.Entries.Where(e => e.Type == EventType.PatrolRoute).ToArray();
            _patrolRouteIndex = (short)FindIndex(patrolRoutes, PatrolRoute);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            long start = bw.Position;

            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(NpcThinkParamId);
            bw.WriteInt32(NpcParamId);
            bw.WriteInt32(TalkId);
            bw.WriteByte(0);
            bw.WriteBoolean(Unk15);
            bw.WriteInt16(Unk16);
            bw.WriteInt32(CharaInitParamId);
            bw.WriteInt32(_parentPartIndex);
            bw.WriteInt16(_patrolRouteIndex);
            bw.WriteInt16(Unk22);
            bw.WriteInt32(-1);
            bw.WriteInt32(Unk28);
            bw.WriteInt32(Unk2c);
            bw.WriteInt32(Unk30);
            bw.WriteInt32(Unk34);
            bw.WriteInt32(Unk38);
            bw.WriteSByte(Unk3c);
            bw.WriteByte(0);
            bw.WriteInt16(0);
            bw.WriteInt32(Unk40);
            bw.WriteInt32(Unk44);
            bw.WriteInt32(Unk48);
            bw.WriteInt32(Unk4c);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt64(0);
            bw.ReserveInt64("PartEneStruct78Offset");

            bw.FillInt64("PartEneStruct78Offset", bw.Position - start);
            Struct78.Write(bw);
        }
    }

    public class PartEneStruct78
    {
        public PartEneStruct78() { }

        public PartEneStruct78 DeepClone() => (PartEneStruct78)MemberwiseClone();

        internal PartEneStruct78(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            br.AssertSingle(1);
            for (int i = 0; i < 5; i++)
            {
                br.AssertInt32(-1);
                br.AssertInt16(-1);
                br.AssertInt16(10);
            }
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
            bw.WriteSingle(1);
            for (int i = 0; i < 5; i++)
            {
                bw.WriteInt32(-1);
                bw.WriteInt16(-1);
                bw.WriteInt16(10);
            }
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PartPlayerData : PartTypeData
    {
        public int Unk00 { get; set; }

        public PartPlayerData() { }

        internal PartPlayerData(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PartHitData : PartTypeData
    {
        public byte Unk00 { get; set; }
        public sbyte Unk02 { get; set; }
        public float Unk04 { get; set; }
        public int Unk18 { get; set; }
        public int Unk1c { get; set; }
        public bool Unk26 { get; set; }
        public bool Unk27 { get; set; }
        public byte Unk34 { get; set; }
        public sbyte Unk35 { get; set; }
        public short Unk3c { get; set; }

        public PartHitData()
        {
            Unk02 = -1;
            Unk18 = -1;
            Unk1c = -1;
            Unk35 = -1;
            Unk3c = -1;
        }

        internal PartHitData(BinaryReaderEx br)
        {
            Unk00 = br.ReadByte();
            br.AssertSByte(-1);
            Unk02 = br.ReadSByte();
            br.AssertByte(0);
            Unk04 = br.ReadSingle();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertSingle(-1);
            Unk18 = br.ReadInt32();
            Unk1c = br.ReadInt32();
            br.AssertInt32(-1);
            br.AssertInt16(-1);
            Unk26 = br.ReadBoolean();
            Unk27 = br.ReadBoolean();
            br.AssertInt32(0);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            Unk34 = br.ReadByte();
            Unk35 = br.ReadSByte();
            br.AssertInt16(0);
            br.AssertInt32(-1);
            Unk3c = br.ReadInt16();
            br.AssertInt16(-1);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt16(0);
            br.AssertInt16(-1);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteByte(Unk00);
            bw.WriteSByte(-1);
            bw.WriteSByte(Unk02);
            bw.WriteByte(0);
            bw.WriteSingle(Unk04);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteSingle(-1);
            bw.WriteInt32(Unk18);
            bw.WriteInt32(Unk1c);
            bw.WriteInt32(-1);
            bw.WriteInt16(-1);
            bw.WriteBoolean(Unk26);
            bw.WriteBoolean(Unk27);
            bw.WriteInt32(0);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteByte(Unk34);
            bw.WriteSByte(Unk35);
            bw.WriteInt16(0);
            bw.WriteInt32(-1);
            bw.WriteInt16(Unk3c);
            bw.WriteInt16(-1);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt16(0);
            bw.WriteInt16(-1);
        }
    }

    public class PartDummyObjData : PartTypeData
    {
        public PartDummyObjData() { }

        internal PartDummyObjData(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(-1);
            br.AssertInt32(0);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(-1);
            bw.WriteInt32(0);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
        }
    }

    public class PartConnectHitData : PartTypeData
    {
        private int _parentHitIndex;
        public Part ParentHit { get; set; }
        public sbyte[] MapId { get; private set; }
        public sbyte Unk0a { get; set; }
        public bool Unk0b { get; set; }

        public PartConnectHitData()
        {
            MapId = new sbyte[4];
            Unk0a = -1;
        }

        public override PartTypeData DeepClone()
        {
            var clone = (PartConnectHitData)base.DeepClone();
            clone.MapId = (sbyte[])MapId.Clone();
            return clone;
        }

        internal PartConnectHitData(BinaryReaderEx br)
        {
            _parentHitIndex = br.ReadInt32();
            MapId = br.ReadSBytes(4);
            br.AssertInt16(0);
            Unk0a = br.ReadSByte();
            Unk0b = br.ReadBoolean();
            br.AssertInt32(0);
        }

        internal override void Deindex(MSB_NR msb)
        {
            var hits = msb.Parts.Entries.Where(p => p.Type == PartType.Hit).ToArray();
            ParentHit = FindEntry(hits, _parentHitIndex);
        }

        internal override void Reindex(MSB_NR msb)
        {
            var hits = msb.Parts.Entries.Where(p => p.Type == PartType.Hit).ToArray();
            _parentHitIndex = FindIndex(hits, ParentHit);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(_parentHitIndex);
            bw.WriteSBytes(MapId);
            bw.WriteInt16(0);
            bw.WriteSByte(Unk0a);
            bw.WriteBoolean(Unk0b);
            bw.WriteInt32(0);
        }
    }

    public class PartGeomData : PartTypeData
    {
        public byte Unk00 { get; set; }
        public byte Unk01 { get; set; }
        public int Unk04 { get; set; }
        public byte Unk10 { get; set; }
        public byte Unk11 { get; set; }
        public sbyte Unk12 { get; set; }
        public int Unk14 { get; set; }
        public short Unk1c { get; set; }
        public int Unk34 { get; set; }
        private int _partIndex38;
        public Part Part38 { get; set; }
        private int _partIndex40;
        public Part Part40 { get; set; }
        private int _partIndex44;
        public Part Part44 { get; set; }
        private int _partIndex48;
        public Part Part48 { get; set; }
        private int _partIndex4c;
        public Part Part4c { get; set; }
        private int _partIndex54;
        public Part Part54 { get; set; }
        public int Unk58 { get; set; }
        public PartGeomStruct68 Struct68 { get; set; }
        public PartGeomStruct70 Struct70 { get; set; }
        public PartGeomStruct78 Struct78 { get; set; }
        public PartGeomStruct80 Struct80 { get; set; }

        public PartGeomData()
        {
            Unk04 = -1;
            Unk12 = -1;
            Unk1c = -1;
            Unk34 = -1;
            Unk58 = -1;
            Struct68 = new();
            Struct70 = new();
            Struct78 = new();
            Struct80 = new();
        }

        public override PartTypeData DeepClone()
        {
            var clone = (PartGeomData)base.DeepClone();
            clone.Struct68 = Struct68.DeepClone();
            clone.Struct70 = Struct70.DeepClone();
            clone.Struct78 = Struct78.DeepClone();
            clone.Struct80 = Struct80.DeepClone();
            return clone;
        }

        internal PartGeomData(BinaryReaderEx br)
        {
            long start = br.Position;

            Unk00 = br.ReadByte();
            Unk01 = br.ReadByte();
            br.AssertInt16(0);
            Unk04 = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            Unk10 = br.ReadByte();
            Unk11 = br.ReadByte();
            Unk12 = br.ReadSByte();
            br.AssertByte(0);
            Unk14 = br.ReadInt32();
            br.AssertInt32(0);
            Unk1c = br.ReadInt16();
            br.AssertInt16(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(-1);
            Unk34 = br.ReadInt32();
            _partIndex38 = br.ReadInt32();
            br.AssertInt32(-1);
            _partIndex40 = br.ReadInt32();
            _partIndex44 = br.ReadInt32();
            _partIndex48 = br.ReadInt32();
            _partIndex4c = br.ReadInt32();
            br.AssertInt32(0);
            _partIndex54 = br.ReadInt32();
            Unk58 = br.ReadInt32();
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            long offset68 = br.ReadInt64();
            long offset70 = br.ReadInt64();
            long offset78 = br.ReadInt64();
            long offset80 = br.ReadInt64();

            br.Position = start + offset68;
            Struct68 = new(br);

            br.Position = start + offset70;
            Struct70 = new(br);

            br.Position = start + offset78;
            Struct78 = new(br);

            br.Position = start + offset80;
            Struct80 = new(br);
        }

        internal override void Deindex(MSB_NR msb)
        {
            Part38 = FindEntry(msb.Parts.Entries, _partIndex38);
            Part40 = FindEntry(msb.Parts.Entries, _partIndex40);
            Part44 = FindEntry(msb.Parts.Entries, _partIndex44);
            Part48 = FindEntry(msb.Parts.Entries, _partIndex48);
            Part4c = FindEntry(msb.Parts.Entries, _partIndex4c);
            Part54 = FindEntry(msb.Parts.Entries, _partIndex54);
        }

        internal override void Reindex(MSB_NR msb)
        {
            _partIndex38 = FindIndex(msb.Parts.Entries, Part38);
            _partIndex40 = FindIndex(msb.Parts.Entries, Part40);
            _partIndex44 = FindIndex(msb.Parts.Entries, Part44);
            _partIndex48 = FindIndex(msb.Parts.Entries, Part48);
            _partIndex4c = FindIndex(msb.Parts.Entries, Part4c);
            _partIndex54 = FindIndex(msb.Parts.Entries, Part54);
        }

        internal override void Write(BinaryWriterEx bw)
        {
            long start = bw.Position;

            bw.WriteByte(Unk00);
            bw.WriteByte(Unk01);
            bw.WriteInt16(0);
            bw.WriteInt32(Unk04);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteByte(Unk10);
            bw.WriteByte(Unk11);
            bw.WriteSByte(Unk12);
            bw.WriteByte(0);
            bw.WriteInt32(Unk14);
            bw.WriteInt32(0);
            bw.WriteInt16(Unk1c);
            bw.WriteInt16(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(-1);
            bw.WriteInt32(Unk34);
            bw.WriteInt32(_partIndex38);
            bw.WriteInt32(-1);
            bw.WriteInt32(_partIndex40);
            bw.WriteInt32(_partIndex44);
            bw.WriteInt32(_partIndex48);
            bw.WriteInt32(_partIndex4c);
            bw.WriteInt32(0);
            bw.WriteInt32(_partIndex54);
            bw.WriteInt32(Unk58);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.ReserveInt64("PartGeomStruct68Offset");
            bw.ReserveInt64("PartGeomStruct70Offset");
            bw.ReserveInt64("PartGeomStruct78Offset");
            bw.ReserveInt64("PartGeomStruct80Offset");

            bw.FillInt64("PartGeomStruct68Offset", bw.Position - start);
            Struct68.Write(bw);

            bw.FillInt64("PartGeomStruct70Offset", bw.Position - start);
            Struct70.Write(bw);

            bw.FillInt64("PartGeomStruct78Offset", bw.Position - start);
            Struct78.Write(bw);

            bw.FillInt64("PartGeomStruct80Offset", bw.Position - start);
            Struct80.Write(bw);
        }
    }

    public class PartGeomStruct68
    {
        public short Unk00 { get; set; }
        public short Unk04 { get; set; }

        public PartGeomStruct68() { }

        public PartGeomStruct68 DeepClone() => (PartGeomStruct68)MemberwiseClone();

        internal PartGeomStruct68(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt16();
            br.AssertInt16(-1);
            Unk04 = br.ReadInt16();
            br.AssertInt16(-1);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(0);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt16(Unk00);
            bw.WriteInt16(-1);
            bw.WriteInt16(Unk04);
            bw.WriteInt16(-1);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(0);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PartGeomStruct70
    {
        public int Unk04 { get; set; }
        public sbyte Unk1c { get; set; }

        public PartGeomStruct70()
        {
            Unk04 = -1;
            Unk1c = -1;
        }

        public PartGeomStruct70 DeepClone() => (PartGeomStruct70)MemberwiseClone();

        internal PartGeomStruct70(BinaryReaderEx br)
        {
            br.AssertInt32(0);
            Unk04 = br.ReadInt32();
            br.AssertInt32(-1);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertSingle(-1);
            br.AssertInt32(0);
            Unk1c = br.ReadSByte();
            br.AssertSByte(-1);
            br.AssertInt16(-1);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(0);
            bw.WriteInt32(Unk04);
            bw.WriteInt32(-1);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteSingle(-1);
            bw.WriteInt32(0);
            bw.WriteSByte(Unk1c);
            bw.WriteSByte(-1);
            bw.WriteInt16(-1);
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

    public class PartGeomStruct78
    {
        public int Unk00 { get; set; }
        public float Unk04 { get; set; }
        public byte Unk0a { get; set; }
        public sbyte Unk0b { get; set; }
        public short Unk0c { get; set; }

        public PartGeomStruct78()
        {
            Unk0b = -1;
            Unk0c = -1;
        }

        public PartGeomStruct78 DeepClone() => (PartGeomStruct78)MemberwiseClone();

        internal PartGeomStruct78(BinaryReaderEx br)
        {
            Unk00 = br.ReadInt32();
            Unk04 = br.ReadSingle();
            br.AssertInt16(-1);
            Unk0a = br.ReadByte();
            Unk0b = br.ReadSByte();
            Unk0c = br.ReadInt16();
            br.AssertInt16(0);
            br.AssertSingle(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertInt32(-1);
            br.AssertSByte(-1);
            br.AssertByte(0);
            br.AssertInt16(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.AssertInt32(0);
        }

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteInt32(Unk00);
            bw.WriteSingle(Unk04);
            bw.WriteInt16(-1);
            bw.WriteByte(Unk0a);
            bw.WriteSByte(Unk0b);
            bw.WriteInt16(Unk0c);
            bw.WriteInt16(0);
            bw.WriteSingle(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteInt32(-1);
            bw.WriteSByte(-1);
            bw.WriteByte(0);
            bw.WriteInt16(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
            bw.WriteInt32(0);
        }
    }

    public class PartGeomStruct80
    {
        public bool Unk00 { get; set; }
        public sbyte Unk02 { get; set; }

        public PartGeomStruct80()
        {
            Unk02 = -1;
        }

        public PartGeomStruct80 DeepClone() => (PartGeomStruct80)MemberwiseClone();

        internal PartGeomStruct80(BinaryReaderEx br)
        {
            Unk00 = br.ReadBoolean();
            br.AssertSByte(-1);
            Unk02 = br.ReadSByte();
            br.AssertByte(0);
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

        internal void Write(BinaryWriterEx bw)
        {
            bw.WriteBoolean(Unk00);
            bw.WriteSByte(-1);
            bw.WriteSByte(Unk02);
            bw.WriteByte(0);
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
        }
    }
}
