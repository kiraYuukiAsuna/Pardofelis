namespace PardofelisCore.Api;

public class BuitlinApiKeyInfo
{
    public string Url = "";
    public string ModelName = "";
    public string ApiKey = "";
    
}

public static class BuitlinApiKeyConfig
{
    public static List<BuitlinApiKeyInfo> BuitlinApiKeyInfos = new()
    {
        new BuitlinApiKeyInfo()
        {
            Url = "https://chatapi.nloli.xyz/v1",
            ModelName = "gpt-4o-mini",
            ApiKey = "sk-O8uZWKkEzVHa2jIG54F8269a27354c668f09A546444c0bCc"
        },
        new BuitlinApiKeyInfo()
        {
            Url = "https://hkc3s.shamiko.uk/v1",
            ModelName = "gpt-4o-mini",
            ApiKey = "sk-O8uZWKkEzVHa2jIG54F8269a27354c668f09A546444c0bCc"
        }
    };
}
