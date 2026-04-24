using AiChatClient.Maui.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;

namespace AiChatClient.UnitTests;

public class ImageGenerationServiceTests : BaseTest
{
	[Test]
	public async Task GenerateImageAsync_ReturnsNonNullByteArray()
	{
		// Arrange
		var service = new ImageGenerationServices(ImageGenerator);

		// Act 
		var result = await service.GenerateImageAsync("A simple red circle on a white background", CancellationToken.None);

		// Assert
		Assert.That(result, Is.Not.Null);
		Assert.That(result, Is.Not.Empty);
	}

	[Test]
	public async Task GenerateImageAsync_EquivalenceEvaluator()
	{
		// Arrange
		const string prompt = "A simple red circle on a white background";
		var service = new ImageGenerationServices(ImageGenerator);

		var equivalenceEvaluator = new EquivalenceEvaluator();
		var equivalenceContext = new EquivalenceEvaluatorContext("The image contains a red circle on a white background");

		// Act
		var imageBytes = await service.GenerateImageAsync(prompt, CancellationToken.None);
		Assert.That(imageBytes, Is.Not.Null);

		var messages = new List<ChatMessage>
		{
			new ChatMessage(ChatRole.User,
			[
				new DataContent(imageBytes, "image/png"),
				new TextContent("Describe what this image contains")
			])
		};

		var chatResponse = await ChatClient.GetResponseAsync(messages);

		var equivalenceResult = await equivalenceEvaluator.EvaluateAsync(
			messages,
			chatResponse,
			new ChatConfiguration(ChatClient),
			[equivalenceContext]);

		var equivalenceResultMetric = equivalenceResult.Get<NumericMetric>(EquivalenceEvaluator.EquivalenceMetricName);

		// Assert
		Assert.That(equivalenceResultMetric.Value, Is.GreaterThanOrEqualTo(4));
	}
}