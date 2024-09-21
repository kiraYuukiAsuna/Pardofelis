using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Text;


namespace PardofelisCore.Util;

#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0001

public class Rag
{
    private static readonly SemaphoreSlim RagSemaphore = new SemaphoreSlim(1, 1);

    public static async Task InsertTextChunkAsync(ISemanticTextMemory textMemory, string collection, string text, string additionalMetaData)
    {
        if (string.IsNullOrEmpty(collection) || string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Collection or text cannot be null or empty.");
        }
        var a = text.Length;
        var lines = TextChunker.SplitPlainTextLines(text, 32);
        var paragraphs = TextChunker.SplitPlainTextParagraphs(lines, 128);

        foreach (var para in paragraphs)
        {
            await textMemory.SaveInformationAsync(collection, para, Guid.NewGuid().ToString(), additionalMetadata: additionalMetaData,
                cancellationToken: default);
        }
    }

    public static async Task<List<KeyValuePair<string, string>>> VectorSearch(ISemanticTextMemory textMemory, string collection, string text)
    {
        await RagSemaphore.WaitAsync();
        try
        {
            var memoryResult = textMemory.SearchAsync(collection, text, 16, 0.7);
            List<KeyValuePair<string, string>> results = new();
            await foreach (var item in memoryResult)
            {
                Console.WriteLine($"Text: {item.Metadata.Text}, AdditionalMetadata: {item.Metadata.AdditionalMetadata}");
                results.Add(new KeyValuePair<string, string>(item.Metadata.Text, item.Metadata.AdditionalMetadata));
            }
            return results;
        }
        finally
        {
            RagSemaphore.Release();
        }
    }
}