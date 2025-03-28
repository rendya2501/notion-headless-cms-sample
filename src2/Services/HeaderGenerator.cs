using hoge.Constants;
using hoge.Models;
using hoge.Utils;
using System.Text;

namespace hoge.Services;

public class HeaderGenerator : IHeaderGenerator
{
    /// <summary>
    /// フロントマターを追加します。
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="pageData"></param>
    /// <param name="outputDirectory"></param>
    /// <returns></returns>
    public async Task<StringBuilder> GenerateHeaderAsync(PageData pageData, string outputDirectory)
    {
        var sb = new StringBuilder();

        sb.AppendLine("---");

        if (!string.IsNullOrWhiteSpace(pageData.Type))
        {
            sb.AppendLine($"{FrontMatterConstants.TypeName}: \"{pageData.Type}\"");
        }

        sb.AppendLine($"{FrontMatterConstants.TitleName}: \"{pageData.Title}\"");

        if (!string.IsNullOrWhiteSpace(pageData.Description))
        {
            sb.AppendLine($"{FrontMatterConstants.DescriptionName}: \"{pageData.Description}\"");
        }

        if (pageData.Tags.Count > 0)
        {
            var formattedTags = pageData.Tags.Select(tag => $"\"{tag}\"");
            sb.AppendLine($"{FrontMatterConstants.TagsName}: [{string.Join(',', formattedTags)}]");
        }

        if (pageData.PublishedDateTime.HasValue)
        {
            sb.AppendLine($"{FrontMatterConstants.PublishedName}: \"{pageData.PublishedDateTime.Value:s}\"");
        }

        if (!string.IsNullOrEmpty(pageData.CoverImageUrl))
        {
            var fileName = await ImageDownloader.DownloadImageAsync(pageData.CoverImageUrl, outputDirectory);
            sb.AppendLine($"{FrontMatterConstants.EyecatchName}: \"./{fileName}\"");
        }

        sb.AppendLine("---");
        sb.AppendLine();

        return sb;
    }

}
