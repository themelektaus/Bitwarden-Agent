using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BitwardenAgent;

public record EncryptedData(byte[] Key, byte[] IV, byte[] Data)
{
    static readonly byte[] PROTECTION_ENTROPY
        = "619687c0d5704358b1ce4c04ea952c68"u8.ToArray();

    public static EncryptedData From(byte[] password, byte[] data)
    {
        var keyLength = password.Length / 3 * 2;
        var key = password.Take(keyLength).ToArray();
        var iv = password.Skip(keyLength).ToArray();
        return new(key, iv, data);
    }

    public static EncryptedData Encrypt(string data)
    {
        return Encrypt(Encoding.UTF8.GetBytes(data));
    }

    public static EncryptedData Encrypt(byte[] data)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateKey();
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var stream = new MemoryStream();

        using (
            var cryptoStream = new CryptoStream(
                stream,
                encryptor,
                CryptoStreamMode.Write
            )
        )
        {
            cryptoStream.Write(data, 0, data.Length);
        }

        return new(aes.Key, aes.IV, stream.ToArray());
    }

    public byte[] GetPassword()
    {
        return Enumerable.Concat(Key, IV).ToArray();
    }

    public string Decrypt()
    {
        return Encoding.UTF8.GetString(DecryptInternal());
    }

    byte[] DecryptInternal()
    {
        byte[] result;

        using (var aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = Key;
            aes.IV = IV;

            using var decryptor = aes.CreateDecryptor();
            using var sourceStream = new MemoryStream(Data);
            using var destinationStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(
                sourceStream,
                decryptor,
                CryptoStreamMode.Read
            );

            cryptoStream.CopyTo(destinationStream);

            result = destinationStream.ToArray();
        }

        return result;
    }

    public static void Protect(ref byte[] value)
        => value = ProtectedData.Protect(
            value,
            PROTECTION_ENTROPY,
            DataProtectionScope.CurrentUser
        );

    public static void Unprotect(ref byte[] value)
        => value = ProtectedData.Unprotect(
            value,
            PROTECTION_ENTROPY,
            DataProtectionScope.CurrentUser
        );
}
