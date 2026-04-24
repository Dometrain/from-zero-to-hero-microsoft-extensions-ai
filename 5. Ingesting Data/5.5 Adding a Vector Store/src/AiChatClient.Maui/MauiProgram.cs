using System.ClientModel;
using System.ComponentModel;
using Azure.AI.OpenAI;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.SqliteVec;
using Octokit;
using OllamaSharp;

namespace AiChatClient.Maui;

static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder()
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseMauiCommunityToolkitMarkup()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.ConfigureMauiHandlers(static handlers =>
			{
#if IOS || MACCATALYST
				handlers.AddHandler<CollectionView, CollectionViewNoScrollBarsHandler>();
#endif
			});


#if DEBUG
		builder.Logging.AddDebug();
#endif
		builder.Services.AddSingleton<App>();
		builder.Services.AddSingleton<AppShell>();

		// Add Pages + View Models
		builder.Services.AddTransientWithShellRoute<ChatPage, ChatViewModel>();
		builder.Services.AddTransientWithShellRoute<TrainedFilesPage, TrainedFilesViewModel>();

		// Add Services
		builder.Services.AddSingleton<TrainedFileNameService>();
		builder.Services.AddSingleton<IFilePicker>(static _ => FilePicker.Default);
		builder.Services.AddSingleton<IPreferences>(static _ => Preferences.Default);
		builder.Services.AddSingleton<IDeviceDisplay>(static _ => DeviceDisplay.Current);

		builder.Services.AddSingleton<ChatClientServices>();
		builder.Services.AddSingleton<GitHubServices>();
		builder.Services.AddSingleton<PdfIngestionService>();

		builder.Services.AddSingleton<GitHubClient>(static _ => new GitHubClient(new ProductHeaderValue("AiChatClient")));

		builder.Services.AddChatClient(static _ => CreateOllamaChatClient());
		builder.Services.AddEmbeddingGenerator(static _ => CreateOllamaEmbeddingGenerator());

		return builder.Build();
	}

	static IChatClient CreateOllamaChatClient()
	{
		const string modelId = "qwen3.5";

		var ollamaClient = new OllamaApiClient(GetLocalOllamaEndpoint(), modelId);

		return new ChatClientBuilder(ollamaClient)
			.UseFunctionInvocation()
			.Build();
	}

	static IEmbeddingGenerator<string, Embedding<float>> CreateOllamaEmbeddingGenerator()
	{
		const string modelId = "qwen3-embedding";

		return new OllamaApiClient(GetLocalOllamaEndpoint(), modelId);
	}

	static IServiceCollection AddTransientWithShellRoute<TView, TViewModel>(this IServiceCollection services)
		where TView : NavigableElement, IRoutable
		where TViewModel : class, INotifyPropertyChanged
	{
		return services.AddTransientWithShellRoute<TView, TViewModel>(TView.Route);
	}

	static string GetLocalOllamaEndpoint()
	{
#if ANDROID
		return "http://10.0.2.2:11434";
#else
		return "http://127.0.0.1:11434";
#endif
	}
}