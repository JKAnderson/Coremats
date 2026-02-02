namespace Coremats;

public partial class MSB_NR
{
    public enum PointFormType : uint
    {
        Point = 0,
        Circle = 1,
        Sphere = 2,
        Cylinder = 3,
        Square = 4,
        Box = 5,
        Composite = 6,
    }

    public abstract class PointForm
    {
        internal static PointForm Read(BexReader br, PointFormType type)
        {
            return type switch
            {
                PointFormType.Circle => new Circle(br),
                PointFormType.Sphere => new Sphere(br),
                PointFormType.Cylinder => new Cylinder(br),
                PointFormType.Square => new Square(br),
                PointFormType.Box => new Box(br),
                PointFormType.Composite => new Composite(br),
                _ => throw new NotImplementedException($"Unexpected point form type: {type}"),
            };
        }

        internal virtual void Deindex(MSB_NR msb) { }
        internal virtual void Reindex(MSB_NR msb) { }
        internal abstract void Write(BexWriter bw);

        public class Circle : PointForm
        {
            public float Radius { get; set; }

            public Circle() { }

            internal Circle(BexReader br)
            {
                Radius = br.ReadSingle();
            }

            internal override void Write(BexWriter bw)
            {
                bw.WriteSingle(Radius);
            }
        }

        public class Sphere : PointForm
        {
            public float Radius { get; set; }

            public Sphere() { }

            internal Sphere(BexReader br)
            {
                Radius = br.ReadSingle();
            }

            internal override void Write(BexWriter bw)
            {
                bw.WriteSingle(Radius);
            }
        }

        public class Cylinder : PointForm
        {
            public float Radius { get; set; }
            public float Height { get; set; }

            public Cylinder() { }

            internal Cylinder(BexReader br)
            {
                Radius = br.ReadSingle();
                Height = br.ReadSingle();
            }

            internal override void Write(BexWriter bw)
            {
                bw.WriteSingle(Radius);
                bw.WriteSingle(Height);
            }
        }

        public class Square : PointForm
        {
            public float Width { get; set; }
            public float Depth { get; set; }

            public Square() { }

            internal Square(BexReader br)
            {
                Width = br.ReadSingle();
                Depth = br.ReadSingle();
            }

            internal override void Write(BexWriter bw)
            {
                bw.WriteSingle(Width);
                bw.WriteSingle(Depth);
            }
        }

        public class Box : PointForm
        {
            public float Width { get; set; }
            public float Depth { get; set; }
            public float Height { get; set; }

            public Box() { }

            internal Box(BexReader br)
            {
                Width = br.ReadSingle();
                Depth = br.ReadSingle();
                Height = br.ReadSingle();
            }

            internal override void Write(BexWriter bw)
            {
                bw.WriteSingle(Width);
                bw.WriteSingle(Depth);
                bw.WriteSingle(Height);
            }
        }

        public class Composite : PointForm
        {
            public CompositeItem[] Points { get; private set; }

            public Composite()
            {
                Points = new CompositeItem[8];
                for (int i = 0; i < 8; i++)
                    Points[i] = new CompositeItem();
            }

            internal Composite(BexReader br)
            {
                Points = new CompositeItem[8];
                for (int i = 0; i < 8; i++)
                    Points[i] = new CompositeItem(br);
            }

            internal override void Deindex(MSB_NR msb)
            {
                foreach (var point in Points)
                    point.Deindex(msb);
            }

            internal override void Reindex(MSB_NR msb)
            {
                foreach (var point in Points)
                    point.Reindex(msb);
            }

            internal override void Write(BexWriter bw)
            {
                foreach (CompositeItem item in Points)
                    item.Write(bw);
            }
        }

        public class CompositeItem
        {
            private int _pointIndex;
            public Point Point { get; set; }
            public int Unk04 { get; set; }

            public CompositeItem() { }

            internal CompositeItem(BexReader br)
            {
                _pointIndex = br.ReadInt32();
                Unk04 = br.ReadInt32();
            }

            internal void Deindex(MSB_NR msb)
            {
                Point = FindEntry(msb.Points.Entries, _pointIndex);
            }

            internal void Reindex(MSB_NR msb)
            {
                _pointIndex = FindIndex(msb.Points.Entries, Point);
            }

            internal void Write(BexWriter bw)
            {
                bw.WriteInt32(_pointIndex);
                bw.WriteInt32(Unk04);
            }

            public override string ToString()
            {
                return $"[{_pointIndex}] ({Unk04})";
            }
        }
    }
}
