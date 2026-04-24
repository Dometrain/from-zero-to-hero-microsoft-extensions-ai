using Microsoft.Extensions.AI;
using Size = System.Drawing.Size;

namespace AiChatClient.Maui.Services;

public class ImageGenerationServices(IImageGenerator imageGenerator)
{
	readonly IImageGenerator _imageGenerator = imageGenerator;

	public async Task<byte[]?> GenerateImageAsync(string prompt, CancellationToken token)
	{
		var options = new ImageGenerationOptions
		{
			MediaType = "image/png",
			ImageSize = new Size(1024, 1024),
			Count = 1,
			StreamingCount = 1
		};

		var response = await _imageGenerator.GenerateImagesAsync(prompt, options, token);

		var firstImage = response.Contents.OfType<DataContent>().FirstOrDefault();

		return firstImage?.Data.ToArray();
	}
}