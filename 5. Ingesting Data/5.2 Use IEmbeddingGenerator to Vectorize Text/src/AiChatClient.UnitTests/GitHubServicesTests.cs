using AiChatClient.Maui;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.NLP;

namespace AiChatClient.UnitTests;

public class GitHubServicesTests : BaseTest
{
	[Test]
	public async Task EnsureGitHubBioMatchesExtpectedResults()
	{
		// Arrange
		var chatClientService = new ChatClientServices(ChatClient);
		var githubService = new GitHubServices(GitHubClient);
		var f1Evaluator = new F1Evaluator();
		var f1Context = new F1EvaluatorContext(".NET MAUI app developer. C# Consultant and Educator. Formerly @microsoft @xamarinhq @aws. Creator + Lead Developer of the .NET MAUI @CommunityToolkit");

		var options = new ChatOptions
		{
			Tools =
			[
				AIFunctionFactory.Create(githubService.GetBrandonMinnicksUserName),
				AIFunctionFactory.Create(githubService.GetUserBio),
			]
		};

		var requestMessage = new ChatMessage(ChatRole.User, "What is Brandon Minnick's GitHub User Bio?");

		// Act
		string asisstantResponse = "";
		await foreach (var response in chatClientService.GetStreamingResponseAsync(requestMessage, options, CancellationToken.None))
		{
			asisstantResponse += response.Text;
		}

		var assistantResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, asisstantResponse));
		var f1Metric = await f1Evaluator.EvaluateAsync(
			[requestMessage],
			assistantResponse,
			new ChatConfiguration(ChatClient),
			[f1Context]);

		var f1ResultMetric = f1Metric.Get<NumericMetric>(F1Evaluator.F1MetricName);

		// Assert
		Assert.That(f1ResultMetric.Value, Is.GreaterThanOrEqualTo(0.85));
	}
}