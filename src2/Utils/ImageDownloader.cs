using System.Security.Cryptography;
using System.Text;

namespace hoge.Utils;

public class ImageDownloader
{
    public static async Task<string> DownloadImageAsync(string url, string outputDirectory)
    {
        var uri = new Uri(url);
        var fileNameBytes = Encoding.UTF8.GetBytes(uri.LocalPath);
        var fileName = $"{Convert.ToHexString(MD5.HashData(fileNameBytes))}{Path.GetExtension(uri.LocalPath)}";
        var filePath = Path.Combine(outputDirectory, fileName);

        using var client = new HttpClient();

        try
        {
            var response = await client.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            await using var fileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            await response.Content.CopyToAsync(fileStream);

            return fileName;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download image from {url}: {ex.Message}");
            return string.Empty;
        }
    }

    //public static async Task<byte[]> DownloadImageAsync(string url)
    //{
    //    using var client = new HttpClient();
    //    try
    //    {
    //        var response = await client.GetAsync(url);
    //        response.EnsureSuccessStatusCode();
    //        return await response.Content.ReadAsByteArrayAsync();
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Failed to download image from {url}: {ex.Message}");
    //        return Array.Empty<byte>();
    //    }
    //}

    //public static async Task<string> SaveImageAsync(byte[] imageData, string url, string outputDirectory)
    //{
    //    var uri = new Uri(url);
    //    var fileNameBytes = Encoding.UTF8.GetBytes(uri.LocalPath);
    //    var fileName = $"{Convert.ToHexString(MD5.HashData(fileNameBytes))}{Path.GetExtension(uri.LocalPath)}";
    //    var filePath = Path.Combine(outputDirectory, fileName);

    //    try
    //    {
    //        await using var fileStream = new FileStream(
    //            filePath,
    //            FileMode.Create,
    //            FileAccess.Write,
    //            FileShare.None);

    //        await fileStream.WriteAsync(imageData);

    //        return fileName;
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Failed to save image to {filePath}: {ex.Message}");
    //        return string.Empty;
    //    }
    //}
}
