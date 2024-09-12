using Microsoft.SemanticKernel;
using System.ComponentModel;
using MimeKit;

namespace FunctionCall.Agent
{
    public class EmailPlugin
    {
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
                var emailSender = new EmailSender("smtp.qq.com", 465, "1175445708@qq.com", "esietlytzouojegf");
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
}