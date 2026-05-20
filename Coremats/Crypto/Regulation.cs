using System.Security.Cryptography;
using System.Text;
using static Coremats.Crypto.Util;

namespace Coremats.Crypto;

public static class Regulation
{
    private const int IV_SIZE = 16;

    public static byte[] ArmoredCore6Key
        => ParseHexString("10 CE ED 47 7B 7C D9 D7 E6 93 8E 11 47 13 E7 87 D5 39 13 B1 0D 31 8E C1 35 E4 BE 50 50 4E 0E 10");
    public static byte[] DarkSouls2Key
        => ParseHexString("40 17 81 30 DF 0A 94 54 33 09 E1 71 EC BF 25 4C");
    public static byte[] DarkSouls3Key
        => Encoding.ASCII.GetBytes("ds3#jn/8_7(rsY9pg55GFN7VFL#+3n/)");
    public static byte[] EldenRingKey
        => ParseHexString("99 BF FC 36 6A 6B C8 C6 F5 82 7D 09 36 02 D6 76 C4 28 92 A0 1C 20 7F B0 24 D3 AF 4E 49 3F EF 99");
    public static byte[] NightreignKey
        => ParseHexString("9A 8E E9 0C 4C 01 A4 31 68 A1 7D 9D 75 E4 A7 D0 21 07 EB CF 43 D5 AC B0 55 4F 94 16 01 B5 79 18");

    private static Aes CreateAesCbc(byte[] key)
    {
        var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.Zeros;
        aes.Key = key;
        return aes;
    }

    public static byte[] DecryptRegulationCbc(ReadOnlySpan<byte> encrypted, byte[] key) => DecryptRegulationCbc(encrypted, key, out _);

    public static byte[] DecryptRegulationCbc(ReadOnlySpan<byte> encrypted, byte[] key, out ReadOnlySpan<byte> iv)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(encrypted.Length, IV_SIZE);

        using var aes = CreateAesCbc(key);
        iv = encrypted[..IV_SIZE];
        var ciphertext = encrypted[IV_SIZE..];
        return aes.DecryptCbc(ciphertext, iv, aes.Padding);
    }

    public static byte[] EncryptRegulationCbc(ReadOnlySpan<byte> unencrypted, byte[] key)
    {
        // Arbitrary IVs are supported, but in practice FromSoft always uses 0 so that's what I'll default to
        Span<byte> iv = stackalloc byte[IV_SIZE];
        iv.Clear();
        return EncryptRegulationCbc(unencrypted, key, iv);
    }

    public static byte[] EncryptRegulationCbc(ReadOnlySpan<byte> unencrypted, byte[] key, ReadOnlySpan<byte> iv)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(iv.Length, IV_SIZE);

        using var aes = CreateAesCbc(key);
        int cipherLength = aes.GetCiphertextLengthCbc(unencrypted.Length, aes.Padding);
        var encrypted = new byte[IV_SIZE + cipherLength];
        iv.CopyTo(encrypted);
        aes.EncryptCbc(unencrypted, iv, encrypted.AsSpan(IV_SIZE), aes.Padding);
        return encrypted;
    }
}
