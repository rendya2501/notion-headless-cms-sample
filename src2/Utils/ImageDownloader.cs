using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace hoge.Utils;

/// <summary>
/// 画像ダウンローダー
/// </summary>
public class ImageDownloader
{
    /// <summary>
    /// 画像をダウンロードする
    /// </summary>
    /// <param name="url">画像のURL</param>
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

    /// <summary>
    /// マークダウンの画像をダウンロードする
    /// </summary>
    /// <param name="markdown">マークダウン</param>
    /// <param name="outputDirectory">出力ディレクトリ</param>
    /// <returns>マークダウン</returns>
    public static async Task<string> ProcessMarkdownImagesAsync(string markdown, string outputDirectory)
    {
        var imagePattern = new Regex(@"!\[([^\]]*)\]\(([^)]+)\)");
        var processedMarkdown = await imagePattern.ReplaceAsync(markdown, async match =>
        {
            var altText = match.Groups[1].Value;
            var imageUrl = match.Groups[2].Value;
            
            if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
            {
                return match.Value; // 相対パスの場合はそのまま返す
            }

            var downloadedFileName = await DownloadImageAsync(imageUrl, outputDirectory);
            return string.IsNullOrEmpty(downloadedFileName)
                ? match.Value // ダウンロード失敗時は元のURLを使用
                : $"![{altText}]({downloadedFileName})";
        });

        return processedMarkdown;
    }
}

/// <summary>
/// Regexの拡張メソッド
/// </summary>
public static class RegexExtensions
{
    /// <summary>
    /// マークダウンの画像をダウンロードする
    /// </summary>
    /// <param name="regex">正規表現</param>
    /// <param name="input">入力</param>
    /// <param name="replacementFn">置換関数</param>
    /// <returns>マークダウン</returns>
    public static async Task<string> ReplaceAsync(this Regex regex, string input, Func<Match, Task<string>> replacementFn)
    {
        var matches = regex.Matches(input);
        var replacements = new Dictionary<Match, string>();

        foreach (Match match in matches)
        {
            replacements[match] = await replacementFn(match);
        }

        var result = new StringBuilder(input);
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            var match = matches[i];
            result.Remove(match.Index, match.Length);
            result.Insert(match.Index, replacements[match]);
        }

        return result.ToString();
    }
}
