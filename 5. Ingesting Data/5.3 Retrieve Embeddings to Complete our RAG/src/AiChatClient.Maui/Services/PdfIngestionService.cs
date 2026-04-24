using System.Numerics.Tensors;
using AiChatClient.Maui;
using Microsoft.Extensions.AI;
using UglyToad.PdfPig;

namespace AiChatClient;

public class PdfIngestionService(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
{
	const int _chunkSize = 1000;
	const int _chunkOverlap = 200;

	static readonly List<PdfChunkRecord> _pdfChunkRecords = [];
	readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator = embeddingGenerator;

	public async Task IngestPdfAsync(Stream pdfStream, string fileName, CancellationToken token = default)
	{
		var text = ExtractTextFromPdf(pdfStream);

		foreach (var chunk in ChunkText(text))
		{
			var embedding = await _embeddingGenerator.GenerateAsync(chunk, cancellationToken: token);

			var pdfChunkRecord = new PdfChunkRecord
			{
				SourceFile = fileName,
				Text = chunk,
				Vector = embedding.Vector
			};

			_pdfChunkRecords.Add(pdfChunkRecord);
		}
	}

	public async Task<string?> SearchAsync(string query, CancellationToken token = default)
	{
		throw new NotImplementedException();
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