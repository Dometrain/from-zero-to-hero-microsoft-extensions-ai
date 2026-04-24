using Microsoft.Extensions.AI;

namespace AiChatClient.Maui;

public class ChatClientServices(IChatClient client)
{
	readonly IChatClient _client = client;
	readonly List<ChatMessage> _conversationHistory = [];

	public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(ChatMessage message, ChatOptions? options, CancellationToken token)
	{
		_conversationHistory.Add(message);
		return _client.GetStreamingResponseAsync(_conversationHistory, options, token);
	}

	public void AddAssistantResponse(string text)
	{
		_conversationHistory.Add(new ChatMessage(ChatRole.Assistant, text));
	}

	public void ClearConversationHistory()
	{
		_conversationHistory.Clear();
	}
}