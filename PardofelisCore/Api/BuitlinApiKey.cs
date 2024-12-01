namespace PardofelisCore.Api;

public class BuitlinApiKeyInfo
{
    public string Url = "";
    public string ModelName = "";
    public string ApiKey = "";
    
}

public static class BuitlinApiKeyConfig
{
    public static List<BuitlinApiKeyInfo> BuitlinApiKeyInfos = new();
}
