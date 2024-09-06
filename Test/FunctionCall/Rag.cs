using System.Numerics;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Text;

#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0001

namespace FunctionCall;

public class Rag
{
    public static async Task InsertTextChunkAsync(ISemanticTextMemory textMemory, string collection, string text)
    {
        if (string.IsNullOrEmpty(collection) || string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Collection or text cannot be null or empty.");
        }

        var lines = TextChunker.SplitPlainTextLines(text, 128);
        var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 1024);

        foreach (var para in paragraphs)
        {
            await textMemory.SaveInformationAsync(collection, id: Guid.NewGuid().ToString(), text: para,
                cancellationToken: default);
        }
    }

    public static async Task<List<string>> VectorSearch(ISemanticTextMemory textMemory, string collection, string text)
    {
        var memoryResult = textMemory.SearchAsync(collection, text, 3, default);
        List<string> results = new List<string>();
        await foreach (var item in memoryResult)
        {
            results.Add(item.Metadata.Text);
        }

        return results;
    }
}