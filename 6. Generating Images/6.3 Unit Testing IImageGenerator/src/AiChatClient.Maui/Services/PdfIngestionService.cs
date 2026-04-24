using System.Numerics.Tensors;
using AiChatClient.Maui;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using UglyToad.PdfPig;

namespace AiChatClient;

public class PdfIngestionService(
	IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
	[FromKeyedServices("PdfVectorStore")] VectorStoreCollection<string, PdfChunkRecord> vectorCollection)
{
	const int _chunkSize = 1000;
	const int _chunkOverlap = 200;

	readonly VectorStoreCollection<string, PdfChunkRecord> _vectorCollection = vectorCollection;
	readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator = embeddingGenerator;

	public async Task IngestPdfAsync(Stream pdfStream, string fileName, CancellationToken token = default)
	{
		await _vectorCollection.EnsureCollectionExistsAsync(token);

		var text = ExtractTextFromPdf(pdfStream);

		List<PdfChunkRecord> records = [];
		foreach (var chunk in ChunkText(text))
		{
			var embedding = await _embeddingGenerator.GenerateAsync(chunk, cancellationToken: token);

			var pdfChunkRecord = new PdfChunkRecord
			{
				SourceFile = fileName,
				Text = chunk,
				Vector = embedding.Vector
			};
			records.Add(pdfChunkRecord);
		}

		await _vectorCollection.UpsertAsync(records, token);
	}

	public async Task<string?> SearchAsync(string query, CancellationToken token = default)
	{
		var doesCollectionExist = await _vectorCollection.CollectionExistsAsync(token);

		if (!doesCollectionExist)
			return null;

		var queryEmbedding = await _embeddingGenerator.GenerateAsync(query, cancellationToken: token);

		var matchingResults = new List<PdfChunkRecord>();

		await foreach (var result in _vectorCollection.SearchAsync(queryEmbedding.Vector, 10, cancellationToken: token))
		{
			if (result.Score < 0.5f)
				matchingResults.Add(result.Record);
		}

		if (matchingResults.Count is 0)
			return null;

		return string.Join("\n\n", matchingResults.Select(static x => x.Text));
	}

	static string ExtractTextFromPdf(Stream pdfStream)
	{
		using var memoryStream = new MemoryStream();
		pdfStream.CopyTo(memoryStream);
		memoryStream.Position = 0;

		using var document = PdfDocument.Open(memoryStream);
		return string.Join("\n", document.GetPages().Select(p => p.Text));
	}

	static List<string> ChunkText(string text)
	{
		var chunks = new List<string>();

		if (string.IsNullOrWhiteSpace(text))
			return chunks;

		var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		for (var i = 0; i < words.Length; i += _chunkSize - _chunkOverlap)
		{
			var chunk = string.Join(' ', words.Skip(i).Take(_chunkSize));

			if (!string.IsNullOrWhiteSpace(chunk))
				chunks.Add(chunk);

			if (i + _chunkSize >= words.Length)
				break;
		}

		return chunks;
	}
}