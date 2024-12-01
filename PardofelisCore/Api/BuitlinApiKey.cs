using System.Security.Cryptography;
using System.Text;
using CdnDownload;
using Newtonsoft.Json;
using PardofelisCore.Config;

namespace PardofelisCore.Api;

public static class EncryptionHelper
{
    public static byte[] GenerateRandomKey(int size)
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            var key = new byte[size];
            rng.GetBytes(key);
            return key;
        }
    }
    
    public static byte[] GenerateRandomIV(int size)
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            var iv = new byte[size];
            rng.GetBytes(iv);
            return iv;
        }
    }
}

public class BuitlinApiKeyInfo
{
    public string Url = "";
    public string ModelName = "";
    public string ApiKey = "";
    public int Priority = 0;
    public bool Enabled = false;
}

public static class BuitlinApiKeyConfig
{
    private static string LocalBuitlinApiKeyPath =
        Path.Join(CommonConfig.PardofelisAppDataPath, "Download", "Resources", "BuitlinApiKey.json");

    public static void FetchApiKeyInfo()
    {
        try
        {
            var downloadPath = Path.Join(CommonConfig.PardofelisAppDataPath, "Download", "Resources");
            Dl123Pan.DownloadByPathAndName("/directlink/Resources", "BuitlinApiKey.json",
                downloadPath, "BuitlinApiKey.json");

            var json = File.ReadAllText(LocalBuitlinApiKeyPath);
            var encryptedJson = EncryptStringToBytes_Aes(json, Key, IV);
            File.WriteAllBytes(LocalBuitlinApiKeyPath, encryptedJson);

            var decryptedJson = DecryptStringFromBytes_Aes(encryptedJson, Key, IV);
            BuitlinApiKeyInfos = JsonConvert.DeserializeObject<List<BuitlinApiKeyInfo>>(decryptedJson);

            // Sort BuitlinApiKeyInfos by Priority in ascending order
            BuitlinApiKeyInfos.Sort((x, y) => x.Priority.CompareTo(y.Priority));

            Serilog.Log.Information("FetchApiKeyInfo Success");
        }
        catch (Exception e)
        {
            Serilog.Log.Error(e, "FetchApiKeyInfo Error");
            throw;
        }
    }

    public static List<BuitlinApiKeyInfo> BuitlinApiKeyInfos = new();

    private static readonly byte[] Key = EncryptionHelper.GenerateRandomKey(32); // 32 bytes for AES-256
    private static readonly byte[] IV = EncryptionHelper.GenerateRandomIV(16); // 16 bytes for AES
    
    private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
    {
        if (plainText == null || plainText.Length <= 0)
            throw new ArgumentNullException(nameof(plainText));
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException(nameof(Key));
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException(nameof(IV));
        byte[] encrypted;

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }

                encrypted = msEncrypt.ToArray();
            }
        }

        return encrypted;
    }

    private static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
    {
        if (cipherText == null || cipherText.Length <= 0)
            throw new ArgumentNullException(nameof(cipherText));
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException(nameof(Key));
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException(nameof(IV));
        string plaintext;

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(cipherText))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                plaintext = srDecrypt.ReadToEnd();
            }
        }

        return plaintext;
    }
}