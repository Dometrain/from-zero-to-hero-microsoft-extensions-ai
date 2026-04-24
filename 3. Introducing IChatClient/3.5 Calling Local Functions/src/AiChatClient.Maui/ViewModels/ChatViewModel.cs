using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;

namespace AiChatClient.Maui;

public partial class ChatViewModel(ChatClientServices chatClientServices) : BaseViewModel
{
	readonly ChatClientServices _chatClientServices = chatClientServices;

	public string ImageGenerationModeImageButtonSource => IsImageGenerationMode
		? "palette_filled.png"
		: "palette_outline.png";

	public ObservableCollection<ChatModel> ConversationHistory { get; } = [];

	[ObservableProperty]
	public partial string InputText { get; set; } = string.Empty;

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(SubmitInputTextCommand))]
	public partial bool CanSubmitInputTextExecute { get; private set; } = true;

	[ObservableProperty, NotifyPropertyChangedFor(nameof(ImageGenerationModeImageButtonSource))]
	public partial bool IsImageGenerationMode { get; set; } = false;

	public async Task ClearConversationHistory(CancellationToken token)
	{
		CanSubmitInputTextExecute = false;

		try
		{
			ConversationHistory.Clear();
			_chatClientServices.ClearConversationHistory();
		}
		finally
		{
			CanSubmitInputTextExecute = true;
		}
	}

	[RelayCommand(CanExecute = nameof(CanSubmitInputTextExecute))]
	void ToggleImageGenerationModeButton()
	{
		IsImageGenerationMode = !IsImageGenerationMode;
	}

	[RelayCommand(IncludeCancelCommand = true, AllowConcurrentExecutions = false, CanExecute = nameof(CanSubmitInputTextExecute))]
	async Task SubmitInputText(CancellationToken token)
	{
		var inputText = InputText;
		var isImageGenerationMode = IsImageGenerationMode;

		InputText = string.Empty;

		CanSubmitInputTextExecute = false;

		ConversationHistory.Add(new ChatModel(inputText, ChatRole.User));

		var assistantChatModel = new ChatModel(string.Empty, ChatRole.Assistant);

		ConversationHistory.Add(assistantChatModel);

		try
		{
			if (isImageGenerationMode)
			{
				throw new NotImplementedException();
			}
			else
			{
				await foreach (var response in _chatClientServices.GetStreamingResponseAsync
								   (new ChatMessage(ChatRole.User, inputText), null, token))
				{
					assistantChatModel.Text += response.Text;
				}

				_chatClientServices.AddAssistantResponse(assistantChatModel.Text);
			}
		}
		catch (Exception e)
		{
			Trace.WriteLine(e);
		}
		finally
		{
			CanSubmitInputTextExecute = true;
		}
	}
}