namespace AiChatClient.Maui;

public class PdfChunkRecord
{
	public string Text { get; set; } = string.Empty;
	public string SourceFile { get; set; } = string.Empty;
	public ReadOnlyMemory<float> Vector { get; set; }
}