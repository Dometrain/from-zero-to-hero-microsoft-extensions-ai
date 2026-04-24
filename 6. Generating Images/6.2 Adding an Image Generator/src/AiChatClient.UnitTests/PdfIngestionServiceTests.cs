using AiChatClient.Maui;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace AiChatClient.UnitTests;

public class PdfIngestionServiceTests : BaseTest
{
	[Test]
	public async Task SearchAsync_ReturnsNull_WhenNoScoresAreWithinThreshold()
	{
		// Arrange
		const string pdfText = "The quick brown fox jumps over the lazy dog";

		var vectorCollection = CreateVectorStoreCollection();
		var pdfIngestionService = new PdfIngestionService(EmbeddingGenerator, vectorCollection);

		var pdfStream = CreatePdfStream(pdfText);
		await pdfIngestionService.IngestPdfAsync(pdfStream, "foxes.pdf");

		// Act
		var result = await pdfIngestionService.SearchAsync("IBM Computers");

		// Assert
		Assert.That(result, Is.Null);
	}

	[Test]
	public async Task SearchAsync_ReturnsExpectedText_WhenScoresAreWithinThreshold()
	{
		// Arrange
		const string pdfText = "The quick brown fox jumps over the lazy dog";

		var vectorCollection = CreateVectorStoreCollection();
		var pdfIngestionService = new PdfIngestionService(EmbeddingGenerator, vectorCollection);

		var pdfStream = CreatePdfStream(pdfText);
		await pdfIngestionService.IngestPdfAsync(pdfStream, "foxes.pdf");

		// Act
		var result = await pdfIngestionService.SearchAsync(pdfText);

		// Assert
		Assert.That(result, Is.Not.Null);
		Assert.That(result, Does.Contain(pdfText));
	}

	static VectorStoreCollection<string, PdfChunkRecord> CreateVectorStoreCollection()
	{
		var vectorStore = new InMemoryVectorStore();
		return vectorStore.GetCollection<string, PdfChunkRecord>("test-pdf-chunks");
	}

	static Stream CreatePdfStream(string text)
	{
		var builder = new PdfDocumentBuilder();
		var page = builder.AddPage(PageSize.A4);
		var font = builder.AddStandard14Font(Standard14Font.Helvetica);

		page.AddText(text, 12, new PdfPoint(25, 700), font);

		var bytes = builder.Build();
		return new MemoryStream(bytes);
	}
}