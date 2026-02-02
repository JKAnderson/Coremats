namespace Coremats;

public partial class MSB_NR
{
    public class LayerParam : Param<Model>
    {
        public override string Name => "LAYER_PARAM_ST";

        public LayerParam() : base() { }

        internal LayerParam(BexReader br, bool lastParam) : base(br, lastParam, (br, version) => new(br, version)) { }
    }

    public class Layer : Entry
    {
        internal Layer(BexReader br, int version)
        {
            throw new NotImplementedException();
        }

        internal override void Write(BexWriter bw, int version)
        {
            throw new NotImplementedException();
        }
    }
}
