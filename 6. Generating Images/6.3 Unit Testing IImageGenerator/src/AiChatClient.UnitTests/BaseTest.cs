using Microsoft.Extensions.AI;
using Octokit;
using OllamaSharp;

namespace AiChatClient.UnitTests;

public abstract class BaseTest
{
	readonly Lazy<IChatClient> _chatClientHolder = new(CreateOllamaChatClient);
	readonly Lazy<GitHubClient> _gitHubClientHolder = new(new GitHubClient(new ProductHeaderValue("AiChatClient")));
	readonly Lazy<IEmbeddingGenerator<string, Embedding<float>>> _embeddingGeneratorHolder = new(CreateOllamaEmbeddingGenerator);

	protected IChatClient ChatClient => _chatClientHolder.Value;
	protected GitHubClient GitHubClient => _gitHubClientHolder.Value;
	protected IEmbeddingGenerator<string, Embedding<float>> EmbeddingGenerator => _embeddingGeneratorHolder.Value;

	[OneTimeTearDown]
	protected virtual void TearDown()
	{
		if (_chatClientHolder.IsValueCreated)
			ChatClient.Dispose();

		if (_embeddingGeneratorHolder.IsValueCreated)
			EmbeddingGenerator.Dispose();
	}

	static IChatClient CreateOllamaChatClient()
	{
		const string modelId = "qwen3.5";

		var ollamaClient = new OllamaApiClient("http://127.0.0.1:11434", modelId);

		return new ChatClientBuilder(ollamaClient)
			.UseFunctionInvocation()
			.Build();
	}

	static IEmbeddingGenerator<string, Embedding<float>> CreateOllamaEmbeddingGenerator()
	{
		const string modelId = "qwen3-embedding";

		return new OllamaApiClient("http://127.0.0.1:11434", modelId);
	}
}