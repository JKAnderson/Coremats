namespace Coremats.Crypto;

internal static class Util
{
    public static byte[] ParseHexString(string hex) => Convert.FromHexString(hex.Replace(" ", ""));
}
