using Microsoft.SemanticKernel;
using System.ComponentModel;
using MimeKit;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace EmailPlugin;

[JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
public partial class Config : ObservableObject
{
    public static string CurrentPluginWorkingDirectory = System.IO.Directory.GetCurrentDirectory();

    [ObservableProperty] [JsonProperty("SmtpServer")]
    public string _smtpServer = "smtp.qq.com";

    [ObservableProperty] [JsonProperty("SmtpPort")]
    public int _smtpPort = 465;

    [ObservableProperty] [JsonProperty("SmtpUser")]
    public string _smtpUser = "";

    [ObservableProperty] [JsonProperty("SmtpPass")]
    public string _smtpPass = "";

    public float _a;
    public bool _b;
    public List<KeyValuePair<string, string>> _c;

    public void Init()
    {
        var logFileFolder = Path.Join(CurrentPluginWorkingDirectory, "Log");
        if (!Directory.Exists(logFileFolder))
        {
            Log.Information("Log file folder not found. Creating a new one.");
            Directory.CreateDirectory(logFileFolder);
        }

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Path.Join(logFileFolder, "PluginLog.txt"), rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public static Config ReadConfig()
    {
        var configFilePath = Path.Join(CurrentPluginWorkingDirectory, "Config.json");
        if (!File.Exists(configFilePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            Log.Information("Config file not found. Creating a new one.");
            var newConfig = new Config();
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            var json = JsonConvert.SerializeObject(newConfig, settings);
            File.WriteAllText(configFilePath, json);
            return newConfig;
        }

        var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFilePath));
        Log.Information("Read config info: {@ConfigManager}", config);

        return config != null ? config : new Config();
    }

    public static void WriteConfig(Config config)
    {
        var configFilePath = Path.Join(CurrentPluginWorkingDirectory, "Config.json");
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        var json = JsonConvert.SerializeObject(config, settings);
        File.WriteAllText(configFilePath, json);
        Log.Information("Write config info to file: {@ConfigManager}", config);
    }
}

public class EmailPlugin
{
    public Config PluginConfig = new Config();

    public EmailPlugin()
    {
        PluginConfig.Init();
        PluginConfig = Config.ReadConfig();
    }

    [KernelFunction]
    [Description("向收件人发送电子邮件。")]
    public async Task<string> SendEmailAsync(
        Kernel kernel,
        [Description("以分号分隔的收件人电子邮件列表")] string recipientEmails,
        string subject,
        string body
    )
    {
        Console.WriteLine($"向 {recipientEmails} 发送电子邮件：");
        Console.WriteLine($"主题：{subject}");
        Console.WriteLine($"正文：{body}");
        // 添加使用收件人电子邮件、主题和正文发送电子邮件的逻辑

        string result = "";
        var emails = recipientEmails.Split(',');
        foreach (var email in emails)
        {
            var emailSender = new EmailSender(PluginConfig.SmtpServer, PluginConfig.SmtpPort, PluginConfig.SmtpUser,
                PluginConfig.SmtpPass);
            result = await emailSender.SendEmailAsync(email, subject, body);
            Console.WriteLine("电子邮件已发送！");
        }

        return "发送电子邮件成功，发送给了 " + recipientEmails + "。" + result;
    }
}

class EmailSender
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _smtpUser;
    private readonly string _smtpPass;

    public EmailSender(string smtpServer, int smtpPort, string smtpUser, string smtpPass)
    {
        _smtpServer = smtpServer;
        _smtpPort = smtpPort;
        _smtpUser = smtpUser;
        _smtpPass = smtpPass;
    }

    public async Task<string> SendEmailAsync(string recipientEmail, string subject, string body)
    {
        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress("", _smtpUser));
        emailMessage.To.Add(new MailboxAddress("", recipientEmail));
        emailMessage.Subject = subject;
        emailMessage.Body = new TextPart("html") { Text = body };

        string result = "";

        using (var client = new MailKit.Net.Smtp.SmtpClient())
        {
            await client.ConnectAsync(_smtpServer, _smtpPort, true);
            await client.AuthenticateAsync(_smtpUser, _smtpPass);
            result = await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }

        return result;
    }
}
