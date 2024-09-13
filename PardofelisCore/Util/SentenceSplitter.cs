using System.Text.RegularExpressions;

namespace PardofelisCore.Util;
public class SentenceSplitter
{
    public static List<string> SentenceSplit(string text, int segmentSize)
    {
        // Split text into paragraphs
        string[] paragraphs = Regex.Split(text, @"\r\n|\n");
        string pattern = @"[!(),—+\-.:;?？。，、；：]+";
        List<string> sentencesList = new List<string>();

        foreach (string paragraph in paragraphs)
        {
            string[] sentences = Regex.Split(paragraph, pattern);
            MatchCollection discardedChars = Regex.Matches(paragraph, pattern);

            int count = 0, p = 0;

            // Iterate over the symbols by which it is split
            for (int i = 0; i < discardedChars.Count; i++)
            {
                count += sentences[i].Length + discardedChars[i].Value.Length;
                if (count >= segmentSize)
                {
                    sentencesList.Add(paragraph.Substring(p, count).Trim());
                    p += count;
                    count = 0;
                }
            }

            // Add the remaining text
            if (paragraph.Length - p > 0)
            {
                if (paragraph.Length - p <= 4 && sentencesList.Count > 0)
                {
                    sentencesList[sentencesList.Count - 1] += paragraph.Substring(p);
                }
                else
                {
                    sentencesList.Add(paragraph.Substring(p));
                }
            }
        }

        // Uncomment the following lines if you want to log the sentences
        // foreach (string sentence in sentencesList)
        // {
        //     Console.WriteLine(sentence);
        // }

        return sentencesList;
    }

    public static void Test(string[] args)
    {
        string text = @"对于一个在北平住惯的人，像我，冬天要是不刮风，便觉得是奇迹；济南的冬天是没有风声的。对于一个刚由伦敦回来的人，像我，冬天要能看得见日光，便觉得是怪事；济南的冬天是响晴的。自然，在热带的地方，日光是永远那么毒，响亮的天气，反有点叫人害怕。可是，在北中国的冬天，而能有温晴的天气，济南真得算个宝地。
            设若单单是有阳光，那也算不了出奇。请闭上眼睛想：一个老城，有山有水，全在天底下晒着阳光，暖和安适地睡着，只等春风来把它们唤醒，这是不是个理想的境界？小山整把济南围了个圈儿，只有北边缺着点口儿。这一圈小山在冬天特别可爱，好像是把济南放在一个小摇篮里，它们安静不动地低声地说：“你们放心吧，这儿准保暖和。”真的，济南的人们在冬天是面上含笑的。他们一看那些小山，心中便觉得有了着落，有了依靠。他们由天上看到山上，便不知不觉地想起：“明天也许就是春天了吧？这样的温暖，今天夜里山草也许就绿起来了吧？”就是这点幻想不能一时实现，他们也并不着急，因为这样慈善的冬天，干啥还希望别的呢！";
        int segmentSize = 50;
        List<string> result = SentenceSplit(text, segmentSize);

        foreach (string sentence in result)
        {
            Console.WriteLine(sentence);
        }
    }
}