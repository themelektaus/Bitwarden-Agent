using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BitwardenAgent;

public record EncryptedData(byte[] Key, byte[] IV, byte[] Data)
{
    public static EncryptedData Encrypt(byte[] decryptedData)
    {
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();
        using var outputStream = new MemoryStream();
        using var encryptor = aes.CreateEncryptor();
        using var encryptionStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
        encryptionStream.Write(decryptedData);
        return new(aes.Key, aes.IV, outputStream.ToArray());
    }

    public string GetPassword()
    {
        var password = new byte[Key.Length + IV.Length];
        Array.Copy(Key, password, Key.Length);
        Array.Copy(IV, 0, password, Key.Length, IV.Length);
        return Convert.ToBase64String(password);
    }

    public byte[] Decrypt()
    {
        Aes encryptor = Aes.Create();
        encryptor.Key = Key;
        encryptor.IV = IV;
        using var outputStream = new MemoryStream();
        using var decryptor = encryptor.CreateDecryptor();
        using var decryptionStream = new CryptoStream(outputStream, decryptor, CryptoStreamMode.Write);
        decryptionStream.Write(Data, 0, Data.Length);
        return outputStream.ToArray();
    }

    public static (string password, byte[] data) Encrypt(string decryptedData)
    {
        var encryptedData = Encrypt(Encoding.UTF8.GetBytes(decryptedData));
        return (encryptedData.GetPassword(), encryptedData.Data);
    }

    public static string Decrypt(string password, byte[] data)
    {
        var pw = Convert.FromBase64String(password);
        var key = new byte[16];
        var iv = new byte[8];
        Array.Copy(pw, 0, key, 0, 16);
        Array.Copy(pw, 16, iv, 0, 8);
        var encryptedData = new EncryptedData(key, iv, data);
        var decryptedData = encryptedData.Decrypt();
        return Encoding.UTF8.GetString(decryptedData); 
    }
}
