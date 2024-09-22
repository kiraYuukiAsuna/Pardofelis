using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Text;
using Serilog;

namespace PardofelisCore.Util;

#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0001

public class Rag
{
    private static readonly SemaphoreSlim RagSemaphore = new SemaphoreSlim(1, 1);

    public static async Task InsertTextChunkAsync(ISemanticTextMemory textMemory, string collection, string text,
        string additionalMetaData)
    {
        if (string.IsNullOrEmpty(collection) || string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Collection or text cannot be null or empty.");
        }

        var size = text.Length;
        var lines = TextChunker.SplitPlainTextLines(text, 32);

        foreach (var line in lines)
        {
            await textMemory.SaveInformationAsync(collection, line, Guid.NewGuid().ToString(),
                additionalMetadata: additionalMetaData,
                cancellationToken: default);
        }
    }

    public static async Task<List<KeyValuePair<string, string>>> VectorSearch(ISemanticTextMemory textMemory,
        string collection, string text)
    {
        await RagSemaphore.WaitAsync();
        try
        {
            var lines = TextChunker.SplitPlainTextLines(text, 32);
            List<KeyValuePair<string, string>> results = new();

            foreach (var line in lines)
            {
                var memoryResult = textMemory.SearchAsync(collection, line, 16, 0.6);
                await foreach (var item in memoryResult)
                {
                    Log.Information(
                        $"Text: {item.Metadata.Text}, AdditionalMetadata: {item.Metadata.AdditionalMetadata}");
                    results.Add(new KeyValuePair<string, string>(item.Metadata.Text, item.Metadata.AdditionalMetadata));
                }
            }

            return results;
        }
        finally
        {
            RagSemaphore.Release();
        }
    }
}