using AiChatClient.Maui;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Octokit;
using OllamaSharp;

namespace AiChatClient.UnitTests;

public class ChatClientServicesTests : BaseTest
{
	[Test]
	public async Task MaintainsConversationHistory_EquivalenceEvaluator()
	{
		// Arrange
		var service = new ChatClientServices(ChatClient);
		var equivalenceEvaluator = new EquivalenceEvaluator();
		var equivalenceContext = new EquivalenceEvaluatorContext("The user's favorite color is blue");

		const string firstPrompt = "My favorite color is blue. Remember this.";
		const string secondPrompt = "What is my favorite color?";
		string firstAssistantResponse = "";
		string secondAssistantResponse = "";

		// Act
		await foreach (var response in service.GetStreamingResponseAsync(
						   new ChatMessage(ChatRole.User, firstPrompt), null, CancellationToken.None))
		{
			firstAssistantResponse += response.Text;
		}
		service.AddAssistantResponse(firstAssistantResponse);

		await foreach (var response in service.GetStreamingResponseAsync(
						   new ChatMessage(ChatRole.User, secondPrompt), null, CancellationToken.None))
		{
			secondAssistantResponse += response.Text;
		}
		service.AddAssistantResponse(secondAssistantResponse);

		var responseForEvaluator = new ChatResponse(new ChatMessage(ChatRole.Assistant, secondAssistantResponse));
		var chatMessageForEvaluator = new ChatMessage(ChatRole.User, secondPrompt);

		var equivalenceResult = await equivalenceEvaluator.EvaluateAsync(
			[chatMessageForEvaluator],
			responseForEvaluator,
			new ChatConfiguration(ChatClient),
			[equivalenceContext]);

		var equivalenceResultMetric = equivalenceResult.Get<NumericMetric>(EquivalenceEvaluator.EquivalenceMetricName);

		// Assert
		Assert.That(equivalenceResultMetric.Value, Is.GreaterThanOrEqualTo(4));
	}
}