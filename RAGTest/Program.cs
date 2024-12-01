using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Sqlite;
using Microsoft.SemanticKernel.Memory;
using PardofelisCore;
using PardofelisCore.Config;
using PardofelisCore.Util;
using System.Reflection.Metadata;
using System;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.SemanticKernel.Text;


#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0001


AppDataDirectoryChecker.InitPardofelisAppSettings();
AppDataDirectoryChecker.SetCurrentPardofelisAppDataPrefixPath("D:\\Dev");
if (Directory.Exists(AppDataDirectoryChecker.GetCurrentPardofelisAppDataPrefixPath().Message))
{
    Console.WriteLine($"PardofelisAppDataPrefixPath [{AppDataDirectoryChecker.GetCurrentPardofelisAppDataPrefixPath().Message}] exists.");
}
else
{
    Console.WriteLine($"PardofelisAppDataPrefixPath [{AppDataDirectoryChecker.GetCurrentPardofelisAppDataPrefixPath().Message}] not exists.");
}
var res = AppDataDirectoryChecker.CheckAppDataDirectoryAndCreateNoExist();
if (res.Status == false)
{
    Console.WriteLine(res.Message);
    Console.WriteLine($"Failed to find correct PardofelisAppData path. CurrentPath: [{AppDataDirectoryChecker.GetCurrentPardofelisAppDataPrefixPath().Message}]. Please set it to the correct path!");
}

// 启动向量数据库下载
var memoryBuilder = new MemoryBuilder();
memoryBuilder.WithOpenAITextEmbeddingGeneration("zpoint", "api key", "", new HttpClient()
{
    BaseAddress = new Uri("http://127.0.0.1:14251/v1")
});

IMemoryStore memoryStore = await SqliteMemoryStore.ConnectAsync(Path.Join("E:/", "MemStore.db"));
memoryBuilder.WithMemoryStore(memoryStore);
var TextMemory = memoryBuilder.Build();

//var text = File.ReadAllText(@"E:\1_进化的四十六亿重奏 · 全文.txt");

//var collection = "小说";

//var additionalMetadata = "";

//if (string.IsNullOrEmpty(collection) || string.IsNullOrEmpty(text))
//{
//    throw new ArgumentException("Collection or text cannot be null or empty.");
//}
//var a = text.Length;
//var lines = TextChunker.SplitPlainTextLines(text, 32);
//var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 128);

//int curIdx = 0;
//foreach (var para in paragraphs)
//{
//    Console.WriteLine((float)curIdx/paragraphs.Count);
//    await TextMemory.SaveInformationAsync(collection, para, Guid.NewGuid().ToString(), additionalMetadata: additionalMetadata,
//                cancellationToken: default);
//    curIdx++;
//}

//var result = await Rag.VectorSearch(TextMemory, "小说", "螺旋生物");

//foreach(var item in result)
//{
//    Console.WriteLine(item.Key);
//}
