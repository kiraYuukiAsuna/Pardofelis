using System;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Threading.Tasks;

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
            // 目前，我们只会在控制台打印出成功消息
            Console.WriteLine("电子邮件已发送！");
            return "发送失败，网络错误，无法连接到服务器";
        }
    }
}
