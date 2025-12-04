using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;

namespace Coremats.Crypto;

public static class Binder5
{
    public static byte[] DecryptBhd(byte[] input, string pem)
    {
        ICipherParameters parameters = GetCipherParameters(pem);
        return DecryptRsa(input, parameters);
    }

    private static AsymmetricKeyParameter GetCipherParameters(string pem)
    {
        using var sr = new StringReader(pem);
        using var pr = new PemReader(sr);
        return (AsymmetricKeyParameter)pr.ReadObject();
    }

    private static byte[] DecryptRsa(byte[] input, ICipherParameters parameters)
    {
        var engine = new RsaEngine();
        engine.Init(false, parameters);
        int inputBlockSize = engine.GetInputBlockSize();
        int outputBlockSize = engine.GetOutputBlockSize();
        if (input.Length % inputBlockSize != 0)
            throw new ArgumentException($"Input buffer must be a multiple of block size {inputBlockSize}");

        int blocks = input.Length / inputBlockSize;
        byte[] output = new byte[outputBlockSize * blocks];
        Parallel.For(0, blocks, i =>
        {
            // Some versions of DS2 emit the final block as all 0 if the input was all 0
            // This must have been a bug in their serializer because it's obviously not correct RSA,
            // older versions of BouncyCastle were just silently skipping it and returning nothing,
            // which happened to work out anyways
            if (input.AsSpan(i * inputBlockSize, inputBlockSize).ContainsAnyExcept((byte)0))
            {
                byte[] outputBlock = engine.ProcessBlock(input, i * inputBlockSize, inputBlockSize);
                int padding = outputBlockSize - outputBlock.Length;
                Buffer.BlockCopy(outputBlock, 0, output, i * outputBlockSize + padding, outputBlock.Length);
            }
        });
        return output;
    }
}
