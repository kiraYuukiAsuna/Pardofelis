using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PardofelisCore.Config;

public enum Role
{
    System,
    Assistant,
    User,
    Tool
}

public class Message
{
    public Role Role;
    public String Content;

    public Message(Role role, String content)
    {
        Role = role;
        Content = content;
    }
}

public class ChatContent
{
    public string CharacterName;
    public string YourName;
    public List<Message> Messages;

    public ChatContent()
    {
        CharacterName = "未知";
        YourName = "未知";
        Messages = new List<Message>();
    }
}
