namespace Coremats;

public partial class MSB_NR
{
    public class LayerParam : Param<Model>
    {
        public override string Name => "LAYER_PARAM_ST";

        public LayerParam() : base() { }

        internal LayerParam(BinaryReaderEx br, bool lastParam) : base(br, lastParam, br => new(br)) { }
    }

    public class Layer : Entry
    {
        internal Layer(BinaryReaderEx br)
        {
            throw new NotImplementedException();
        }

        internal override void Write(BinaryWriterEx bw)
        {
            throw new NotImplementedException();
        }
    }
}
