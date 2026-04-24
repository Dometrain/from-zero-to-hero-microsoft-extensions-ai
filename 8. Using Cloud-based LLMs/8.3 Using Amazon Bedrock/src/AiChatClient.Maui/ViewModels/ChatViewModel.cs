using System.Collections.ObjectModel;
using System.Diagnostics;
using AiChatClient.Maui.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;

namespace AiChatClient.Maui;

public partial class ChatViewModel(
	ImageGenerationServices imageGenerationServices,
	PdfIngestionService pdfIngestionService,
	ChatClientServices chatClientServices,
	GitHubServices gitHubServices) : BaseViewModel
{
	readonly ImageGenerationServices _imageGenerationServices = imageGenerationServices;
	readonly PdfIngestionService _pdfIngestionService = pdfIngestionService;
	readonly ChatClientServices _chatClientServices = chatClientServices;
	readonly GitHubServices _gitHubServices = gitHubServices;

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
				_chatClientServices.AddUserInput(inputText);

				var image = await _imageGenerationServices.GenerateImageAsync(inputText, token);
				if (image is not null)
				{
					assistantChatModel.ImageData = image;
					assistantChatModel.Text = "Here's the generated image:";
				}
				else
				{
					assistantChatModel.Text = "I was unable to generate an image for you";
				}
			}
			else
			{
				var options = new ChatOptions
				{
					Tools =
					[
						AIFunctionFactory.Create(_gitHubServices.GetBrandonMinnicksUserName),
						AIFunctionFactory.Create(_gitHubServices.GetRepositoryCount),
						AIFunctionFactory.Create(_gitHubServices.GetUserBio),
						AIFunctionFactory.Create(() => "TheCodeTraveler/AsyncAwaitBestPractices",
							name: "GetBrandonMinnicksMostPopularLibrary",
							description: "The library AsyncAwaitBestPractices has millions of downloads on NuGet")
					]
				};

				var pdfContext = await _pdfIngestionService.SearchAsync(inputText, token);

				var prompt = pdfContext is null
					? inputText
					: $"""
					   Use the following context from the ingested documents to answer the question.
					   If the context does not contain the answer, say so.

					   Context: {pdfContext}

					   Question: {inputText}
					   """;

				await foreach (var response in _chatClientServices.GetStreamingResponseAsync
								   (new ChatMessage(ChatRole.User, prompt), options, token))
				{
					assistantChatModel.Text += response.Text;
				}
			}

			_chatClientServices.AddAssistantResponse(assistantChatModel.Text);
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