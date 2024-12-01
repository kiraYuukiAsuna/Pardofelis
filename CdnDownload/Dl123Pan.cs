using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace CdnDownload;

public static class Dl123Pan
{
    public static string GetDirectLinkByFilePathAndName(string filePath, string fileName)
    {
        // 通过文件路径和文件名获取直链，手动拼接
        var url = "https://vip.123pan.cn" + "/" + "1838918272" + filePath + "/" + fileName;
        return url;
    }

    public static string SignUrl(string originUrl, string privateKey, ulong uid, TimeSpan validDuration)
    {
        long ts = DateTimeOffset.UtcNow.Add(validDuration).ToUnixTimeSeconds(); // 有效时间戳
        int rInt = new Random().Next(); // 随机正整数

        Uri objURL = new Uri(originUrl);
        string path = objURL.GetComponents(UriComponents.Path, UriFormat.Unescaped);
        string toBeHashed = $"/{path}-{ts}-{rInt}-{uid}-{privateKey}";

        using (MD5 md5 = MD5.Create())
        {
            byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(toBeHashed));
            string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            string authKey = $"{ts}-{rInt}-{uid}-{hash}";

            var query = HttpUtility.ParseQueryString(objURL.Query);
            query["auth_key"] = authKey;

            UriBuilder uriBuilder = new UriBuilder(objURL)
            {
                Query = query.ToString()
            };

            return uriBuilder.ToString();
        }
    }

    public static string SignUrlDefault(string originUrl)
    {
        string privateKey = "BVxB5aBpPa5ISeXf"; // 鉴权密钥，即用户在123云盘直链管理中设置的鉴权密钥
        ulong uid = 1838918272; // 账号id，即用户在123云盘个人中心页面所看到的账号id
        TimeSpan validDuration = TimeSpan.FromDays(1); // 链接签名有效期

        string newUrl = SignUrl(originUrl, privateKey, uid, validDuration);

        return newUrl;
    }

    public static void DownloadFileAsync(string url, string destinationPath)
    {
        using (WebClient client = new WebClient())
        {
            client.DownloadFile(new Uri(url), destinationPath);
        }
    }

    public static void DownloadByPathAndName(string filePath, string fileName, string downloadPath,
        string downloadFileName)
    {
        var directlink = GetDirectLinkByFilePathAndName(filePath, fileName);

        if (!Directory.Exists(downloadPath))
        {
            Directory.CreateDirectory(downloadPath);
        }

        var downloadFilePath = Path.Join(downloadPath, downloadFileName);
        var signedUrl = SignUrlDefault(directlink);
        DownloadFileAsync(signedUrl, downloadFilePath);
    }
}