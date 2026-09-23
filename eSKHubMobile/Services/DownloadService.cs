using Microsoft.Maui.Storage;

namespace eSKHubMobile.Services
{
    public interface IDownloadService
    {
        Task DownloadFileAsync(string fileName, string base64Data);
    }

    public class DownloadService : IDownloadService
    {
        public async Task DownloadFileAsync(string fileName, string base64Data)
        {
            if (string.IsNullOrEmpty(base64Data)) return;

            try
            {
                string extension = ".bin";
                // Detect mime type and extension from data URI
                if (base64Data.StartsWith("data:"))
                {
                    int commaIndex = base64Data.IndexOf(',');
                    if (commaIndex > 0)
                    {
                        string header = base64Data.Substring(0, commaIndex);
                        if (header.Contains("pdf")) extension = ".pdf";
                        else if (header.Contains("png")) extension = ".png";
                        else if (header.Contains("jpg") || header.Contains("jpeg")) extension = ".jpg";
                        else if (header.Contains("docx")) extension = ".docx";
                        else if (header.Contains("xlsx")) extension = ".xlsx";

                        base64Data = base64Data.Substring(commaIndex + 1);
                    }
                }

                if (!fileName.Contains("."))
                {
                    fileName += extension;
                }

                byte[] fileBytes = Convert.FromBase64String(base64Data);

                // Save to local temporary file
                string tempFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);
                await File.WriteAllBytesAsync(tempFilePath, fileBytes);

                // Use MAUI Share API to let user save or open the file
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Download File",
                    File = new ShareFile(tempFilePath)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download error: {ex.Message}");
            }
        }
    }
}
