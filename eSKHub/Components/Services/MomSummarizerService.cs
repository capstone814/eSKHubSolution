using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Components.Forms;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using System.Text.Json;
using System.Text;
using System.Net.Http.Json;

namespace eSKHub.Services
{
    public interface IMomSummarizerService
    {
        Task<string> SummarizeMomAsync(IBrowserFile file);
    }

    public class MomSummarizerService : IMomSummarizerService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public MomSummarizerService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GeminiSettings:ApiKey"] ?? string.Empty;
        }

        public async Task<string> SummarizeMomAsync(IBrowserFile file)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new Exception("Gemini API key is not configured.");
            }

            string extractedText = await ExtractTextFromFileAsync(file);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                throw new Exception("Could not extract any text from the provided document.");
            }

            return await CallGeminiAsync(extractedText);
        }

        private async Task<string> ExtractTextFromFileAsync(IBrowserFile file)
        {
            var extension = Path.GetExtension(file.Name).ToLowerInvariant();

            // Limit to 10MB to avoid memory issues with huge files
            using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            if (extension == ".txt")
            {
                using var reader = new StreamReader(memoryStream, Encoding.UTF8);
                return await reader.ReadToEndAsync();
            }
            else if (extension == ".pdf")
            {
                var textBuilder = new StringBuilder();
                using (var pdfDocument = PdfDocument.Open(memoryStream.ToArray()))
                {
                    foreach (var page in pdfDocument.GetPages())
                    {
                        textBuilder.AppendLine(ContentOrderTextExtractor.GetText(page));
                    }
                }
                return textBuilder.ToString();
            }
            else if (extension == ".docx")
            {
                using (var wordDoc = WordprocessingDocument.Open(memoryStream, false))
                {
                    var body = wordDoc.MainDocumentPart?.Document?.Body;
                    return body?.InnerText ?? string.Empty;
                }
            }

            throw new NotSupportedException($"File format '{extension}' is not supported.");
        }

        private async Task<string> CallGeminiAsync(string documentText)
        {
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            var prompt = $@"
You are a highly efficient AI administrative assistant. Your task is to summarize the following Minutes of Meeting (MoM) document. 
Please provide a well-structured summary formatted in Markdown. 
Make sure to extract and highlight:
1. **Meeting Details**: Date, Time, Participants (if available).
2. **Key Discussions**: The main points that were talked about.
3. **Decisions Made**: Explicit conclusions or approvals.
4. **Action Items**: Next steps, tasks assigned, and deadlines.

Here is the document text:
------------------------------------------
{documentText}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to communicate with AI API: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseJson);

            try
            {
                var summaryText = document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return summaryText ?? "Error generating summary.";
            }
            catch (Exception)
            {
                throw new Exception("Unexpected response format from the AI Service.");
            }
        }
    }
}
