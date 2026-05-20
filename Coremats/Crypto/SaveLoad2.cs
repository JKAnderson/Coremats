using System.Security.Cryptography;
using static Coremats.Crypto.Util;

namespace Coremats.Crypto;

public static class SaveLoad2
{
    private const int IV_SIZE = 16;

    // I could store these in backing fields to avoid parsing them every time,
    // but I value the aesthetics more than the hopefully trivial cost
    public static byte[] ArmoredCore6Key
        => ParseHexString("B1 56 87 9F 13 48 97 98 70 05 C4 87 00 AE F8 79");
    public static byte[] DarkSouls2Key
        => ParseHexString("B7 FD 46 3E 4A 9C 11 02 DF 17 39 E5 F3 B2 A5 0F");
    public static byte[] DarkSouls2ScholarKey
        => ParseHexString("59 9F 9B 69 96 40 A5 52 36 EE 2D 70 83 5E C7 44");
    public static byte[] DarkSouls3Key
        => ParseHexString("FD 46 4D 69 5E 69 A3 9A 10 E3 19 A7 AC E8 B7 FA");
    public static byte[] DarkSoulsRemasteredKey
        => ParseHexString("01 23 45 67 89 AB CD EF FE DC BA 98 76 54 32 10");
    public static byte[] NightreignKey
        => ParseHexString("18 F6 32 66 05 BD 17 8A 55 24 52 3A C0 A0 C6 09");

    private static Aes CreateAes(byte[] key)
    {
        var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        // FromSoft *almost* uses PKCS7 padding, but they neglect to include a padding block
        // when the plaintext is a multiple of the block size, so it cannot be reliably stripped.
        // In practice this only actually happens in DS2 so I don't know if the other games
        // technically have the same bug, but I don't think it's worth adding a switch to the API.
        // SL2 files always include an internal data length field, so consumers can
        // strip and regenerate authentically incorrect padding as they wish.
        aes.Padding = PaddingMode.None;
        aes.Key = key;
        return aes;
    }

    public static byte[] DecryptFile(ReadOnlySpan<byte> encrypted, byte[] key) => DecryptFile(encrypted, key, out _);

    public static byte[] DecryptFile(ReadOnlySpan<byte> encrypted, byte[] key, out ReadOnlySpan<byte> iv)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(encrypted.Length, IV_SIZE);

        using var aes = CreateAes(key);
        iv = encrypted[..IV_SIZE];
        var ciphertext = encrypted[IV_SIZE..];
        return aes.DecryptCbc(ciphertext, iv, aes.Padding);
    }

    public static byte[] EncryptFile(ReadOnlySpan<byte> unencrypted, byte[] key)
    {
        // Oh no I'm creating an extra instance just to have it generate an IV for me
        using var aes = CreateAes(key);
        return EncryptFile(unencrypted, key, aes.IV);
    }

    public static byte[] EncryptFile(ReadOnlySpan<byte> unencrypted, byte[] key, ReadOnlySpan<byte> iv)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(iv.Length, IV_SIZE);

        using var aes = CreateAes(key);
        int cipherLength = aes.GetCiphertextLengthCbc(unencrypted.Length, aes.Padding);
        var encrypted = new byte[IV_SIZE + cipherLength];
        iv.CopyTo(encrypted);
        aes.EncryptCbc(unencrypted, iv, encrypted.AsSpan(IV_SIZE), aes.Padding);
        return encrypted;
    }
}
