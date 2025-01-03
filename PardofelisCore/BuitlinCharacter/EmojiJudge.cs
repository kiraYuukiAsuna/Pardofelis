﻿using PardofelisCore.Config;

namespace PardofelisCore.BuitlinCharacter;

public class EmojiJudge
{
    private List<ChatMessage> _messages = new List<ChatMessage>()
    {
        new ChatMessage(
            Role.System,
            "请你判断对话的情感基调，然后判断是否需要为B调用一张表情包，现在可以使用的表情包为：[wink],[不知所措],[点赞],[愤怒],[惊讶],[开心],[哭哭],[礼貌],[冒泡],[你好],[生气],[帅气],[晚安],[凶],[耶],[疑惑]\n表情一次只能使用一个，如果判断可以使用某个表情包，请直接发送：[名称]\n如果判断不需要表情包，请直接发送：[None]\n例如以下对话：\nA:最近看了场电影\nB:去看电影了？什么电影啊？好看么？\n你应该发送：[疑惑]"
        ),
        new ChatMessage(
            Role.User,
            "请你判断对话的情感基调，然后判断是否需要配一张表情包。这里是你需要判断的对话：\nA:你好\nB:你好呀~ 最近有什么新鲜事可以和我分享一下吗？"
        ),
        new ChatMessage(
            Role.Assistant,
            "[你好]"
        ),
        new ChatMessage(
            Role.User,
            "请你判断对话的情感基调，然后判断是否需要配一张表情包。这里是你需要判断的对话：\nA:能做我女朋友么！\nB:啊？这也太突然了吧？"
        ),
        new ChatMessage(
            Role.Assistant,
            "[不知所措]"
        ),
        new ChatMessage(
            Role.User,
            "请你判断对话的情感基调，然后判断是否需要配一张表情包。这里是你需要判断的对话：\nA:今天真是糟糕的一天！什么事情都不顺心！\nB:别担心，明天会更好！"
        ),
        new ChatMessage(
            Role.Assistant,
            "[wink]"
        ),
        new ChatMessage(
            Role.User,
            "请你判断对话的情感基调，然后判断是否需要配一张表情包。这里是你需要判断的对话：\nA:我今天考了98分！\nB:哇，好厉害啊！"
        ),
        new ChatMessage(
            Role.Assistant,
            "[点赞]"
        ),
        new ChatMessage(
            Role.User,
            "请你判断对话的情感基调，然后判断是否需要配一张表情包。这里是你需要判断的对话：\nA:你为什么不理我？\nB:我手机没电了，没看到信息。"
        ),
        new ChatMessage(
            Role.Assistant,
            "[哭哭]"
        ),
        new ChatMessage(
            Role.User,
            "请你判断对话的情感基调，然后判断是否需要配一张表情包。这里是你需要判断的对话：\nA:你今天怎么穿成这样？太丑了！\nB:我就是喜欢这样穿！"
        ),
        new ChatMessage(
            Role.Assistant,
            "[生气]"
        ),
        new ChatMessage(
            Role.User,
            "请你判断对话的情感基调，然后判断是否需要配一张表情包。这里是你需要判断的对话：\nA:我今天中彩票了！\nB:恭喜恭喜！"
        ),
        new ChatMessage(
            Role.Assistant,
            "[开心]"
        ),
        new ChatMessage(
            Role.User,
            "请你判断对话的情感基调，然后判断是否需要配一张表情包。这里是你需要判断的对话：\nA:你今天晚上有空吗？\nB:没有，我今天晚上要加班。"
        ),
        new ChatMessage(
            Role.Assistant,
            "[None]"
        ),
        new ChatMessage(
            Role.User,
            "请你判断对话的情感基调，然后判断是否需要配一张表情包。这里是你需要判断的对话：\nA:你看起来……胖了好多，哈哈~\nB:你是不是找死？"
        ),
        new ChatMessage(
            Role.Assistant,
            "[愤怒]"
        ),       
        new ChatMessage(
            Role.User,
            "请你判断对话的情感基调，然后判断是否需要配一张表情包。这里是你需要判断的对话：\nA:你好啊\nB:你好。"
        ),
        new ChatMessage(
            Role.Assistant,
            "[你好]"
        ),        
        new ChatMessage(
        Role.User,
        "请你判断对话的情感基调，然后判断是否需要配一张表情包。这里是你需要判断的对话：\nA:你能比个耶么？\nB:不能，为什么要比耶？"
        ),
        new ChatMessage(
            Role.Assistant,
            "[None]"
        )
    };


}