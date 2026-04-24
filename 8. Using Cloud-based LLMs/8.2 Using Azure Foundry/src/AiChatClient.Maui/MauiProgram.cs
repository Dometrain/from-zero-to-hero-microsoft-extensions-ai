using System.ClientModel;
using System.ComponentModel;
using System.Runtime.Versioning;
using AiChatClient.Maui.Services;
using Azure.AI.OpenAI;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.Maui.Essentials.AI;
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
		builder.Services.AddSingleton<ImageGenerationServices>();

		builder.Services.AddSingleton<GitHubClient>(static _ => new GitHubClient(new ProductHeaderValue("AiChatClient")));

		builder.Services.AddChatClient(static _ =>
		{
			if ((OperatingSystem.IsIOSVersionAtLeast(26) || OperatingSystem.IsMacCatalystVersionAtLeast(26))
				&& DeviceInfo.Current.DeviceType == DeviceType.Physical)
			{
				return CreateAppleIntelligenceChatClient();
			}

			return CreateOllamaChatClient();
		});
		builder.Services.AddEmbeddingGenerator(static _ =>
		{
			if ((OperatingSystem.IsIOSVersionAtLeast(13) || OperatingSystem.IsMacCatalystVersionAtLeast(13, 1))
				&& DeviceInfo.Current.DeviceType == DeviceType.Physical)
			{
				return CreateAppleEmbeddingGenerator();
			}

			return CreateOllamaEmbeddingGenerator();
		});
		// builder.Services.AddImageGenerator(static _ => CreateAzureOpenAiImageGenerator());
		builder.Services.AddKeyedSingleton("PdfVectorStore", static (_, _) => CreateVectorStoreCollection());

		return builder.Build();
	}

	[SupportedOSPlatform("iOS26.0")]
	[SupportedOSPlatform("macos26.0")]
	[SupportedOSPlatform("MacCatalyst26.0")]
	static IChatClient CreateAppleIntelligenceChatClient()
	{
#if IOS || MACCATALYST
		return new AppleIntelligenceChatClient();
#else
		throw new NotSupportedException("Apple Intelligence is only available on iOS + macOS");
#endif
	}

	[SupportedOSPlatform("iOS13.0")]
	[SupportedOSPlatform("macos10.15")]
	[SupportedOSPlatform("MacCatalyst13.1")]
	static IEmbeddingGenerator<string, Embedding<float>> CreateAppleEmbeddingGenerator()
	{
#if IOS || MACCATALYST
		return new NLEmbeddingGenerator();
#else
		throw new NotSupportedException("Apple Intelligence is only available on iOS + macOS");
#endif
	}

	static IChatClient CreateOllamaChatClient()
	{
		const string modelId = "qwen3.5";

		var ollamaClient = new OllamaApiClient(GetLocalOllamaEndpoint(), modelId);

		return new ChatClientBuilder(ollamaClient)
			.UseFunctionInvocation()
			.Build();
	}

	static IImageGenerator CreateAzureOpenAiImageGenerator()
	{
		const string imageModel = "gpt-image-1.5";
		var apiCredentials = new ApiKeyCredential("");

		return new AzureOpenAIClient(new Uri(""), apiCredentials)
			.GetImageClient(imageModel)
			.AsIImageGenerator();
	}

	static IEmbeddingGenerator<string, Embedding<float>> CreateOllamaEmbeddingGenerator()
	{
		const string modelId = "qwen3-embedding";

		return new OllamaApiClient(GetLocalOllamaEndpoint(), modelId);
	}

	static VectorStoreCollection<string, PdfChunkRecord> CreateVectorStoreCollection()
	{
		const string collectionName = "pdf-chunks";

#if ANDROID || IOS || MACCATALYST
		var vectorStore = new InMemoryVectorStore();
#else
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "vectorstore.db");
		var vectorStore = new SqliteVectorStore($"Data Source={dbPath}");
#endif

		return vectorStore.GetCollection<string, PdfChunkRecord>(collectionName);
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