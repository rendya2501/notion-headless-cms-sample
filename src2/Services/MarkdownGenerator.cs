using hoge.Configuration;
using hoge.Models;
using hoge.Utils;
using Notion.Client;
using System.Text;

namespace hoge.Services;

public class MarkdownGenerator(
    AppConfiguration config,
    INotionClientWrapper notionClient) : IMarkdownGenerator
{
    public async Task<string> GenerateMarkdownAsync(Page page, PageData pageData, string outputDirectory)
    {
        var sb = new StringBuilder();

        // FrontMatterの生成
        await AppendFrontMatterAsync(sb, page, pageData, outputDirectory);

        // ページコンテンツの取得と変換
        var blocks = await notionClient.GetPageBlocksAsync(page.Id);
        await AppendBlocksAsync(sb, blocks, string.Empty, outputDirectory);

        return sb.ToString();
    }

    private async Task AppendFrontMatterAsync(
        StringBuilder sb,
        Page page,
        PageData pageData,
        string outputDirectory)
    {
        sb.AppendLine("---");

        if (!string.IsNullOrWhiteSpace(pageData.Type))
        {
            sb.AppendLine($"{config.FrontMatter.TypeName}: \"{pageData.Type}\"");
        }

        sb.AppendLine($"{config.FrontMatter.TitleName}: \"{pageData.Title}\"");

        if (!string.IsNullOrWhiteSpace(pageData.Description))
        {
            sb.AppendLine($"{config.FrontMatter.DescriptionName}: \"{pageData.Description}\"");
        }

        if (pageData.Tags.Count > 0)
        {
            var formattedTags = pageData.Tags.Select(tag => $"\"{tag}\"");
            sb.AppendLine($"{config.FrontMatter.TagsName}: [{string.Join(',', formattedTags)}]");
        }

        if (pageData.PublishedDateTime.HasValue)
        {
            sb.AppendLine($"{config.FrontMatter.PublishedName}: \"{pageData.PublishedDateTime.Value:s}\"");
        }

        if (!string.IsNullOrEmpty(pageData.CoverImageUrl))
        {
            var (fileName, _) = await ImageDownloader.DownloadImageAsync(pageData.CoverImageUrl, outputDirectory);
            sb.AppendLine($"{config.FrontMatter.EyecatchName}: \"./{fileName}\"");
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }

    private async Task AppendBlocksAsync(
        StringBuilder sb,
        List<Block> blocks,
        string indent,
        string outputDirectory)
    {
        foreach (var block in blocks)
        {
            await AppendBlockAsync(sb, block, indent, outputDirectory);
        }
    }

    private async Task AppendBlockAsync(
        StringBuilder sb,
        Block block,
        string indent,
        string outputDirectory)
    {
        switch (block)
        {
            case ParagraphBlock paragraphBlock:
                AppendParagraph(sb, paragraphBlock, indent);
                break;

            case HeadingOneBlock h1:
                AppendHeading(sb, h1.Heading_1.RichText, indent, "# ");
                break;

            case HeadingTwoBlock h2:
                AppendHeading(sb, h2.Heading_2.RichText, indent, "## ");
                break;

            case HeadingThreeBlock h3:
                AppendHeading(sb, h3.Heading_3.RichText, indent, "### ");
                break;

            case ImageBlock imageBlock:
                await AppendImage(sb, imageBlock, indent, outputDirectory);
                break;

            case CodeBlock codeBlock:
                AppendCode(sb, codeBlock, indent);
                break;

            case BulletedListItemBlock bulletListItem:
                AppendBulletListItem(sb, bulletListItem, indent);
                break;

            case NumberedListItemBlock numberedListItem:
                AppendNumberedListItem(sb, numberedListItem, indent);
                break;

            case BookmarkBlock bookmarkBlock:
                AppendBookmark(sb, bookmarkBlock, indent);
                break;

            case DividerBlock _:
                sb.AppendLine($"{indent}---");
                break;

            default:
                // 未対応のブロックタイプ
                break;
        }

        sb.AppendLine();

        // 子ブロックの処理
        if (block.HasChildren)
        {
            var childBlocks = await notionClient.GetChildBlocksAsync(block.Id);
            await AppendBlocksAsync(sb, childBlocks, $"{indent}    ", outputDirectory);
        }
    }

    private void AppendParagraph(StringBuilder sb, ParagraphBlock paragraphBlock, string indent)
    {
        sb.Append(indent);

        foreach (var richText in paragraphBlock.Paragraph.RichText)
        {
            AppendRichText(sb, richText);
        }
    }

    private void AppendHeading(StringBuilder sb, IEnumerable<RichTextBase> richTexts, string indent, string headingPrefix)
    {
        sb.Append($"{indent}{headingPrefix}");

        foreach (var richText in richTexts)
        {
            AppendRichText(sb, richText);
        }
    }

    private async Task AppendImage(StringBuilder sb, ImageBlock imageBlock, string indent, string outputDirectory)
    {
        var url = string.Empty;

        switch (imageBlock.Image)
        {
            case ExternalFile externalFile:
                url = externalFile.External.Url;
                break;

            case UploadedFile uploadedFile:
                url = uploadedFile.File.Url;
                break;
        }

        if (!string.IsNullOrEmpty(url))
        {
            var (fileName, _) = await ImageDownloader.DownloadImageAsync(url, outputDirectory);
            sb.Append($"{indent}![](./{fileName})");
        }
    }

    private void AppendCode(StringBuilder sb, CodeBlock codeBlock, string indent)
    {
        var language = MapCodeLanguage(codeBlock.Code.Language);
        sb.AppendLine($"{indent}```{language}");

        foreach (var richText in codeBlock.Code.RichText)
        {
            sb.Append(indent);
            sb.Append(richText.PlainText.Replace("\t", "    "));
            sb.AppendLine();
        }

        sb.AppendLine($"{indent}```");
    }

    private string MapCodeLanguage(string notionLanguage)
    {
        return notionLanguage switch
        {
            "c#" => "csharp",
            _ => notionLanguage
        };
    }

    private void AppendBulletListItem(StringBuilder sb, BulletedListItemBlock bulletListItem, string indent)
    {
        sb.Append($"{indent}* ");

        foreach (var richText in bulletListItem.BulletedListItem.RichText)
        {
            AppendRichText(sb, richText);
        }
    }

    private void AppendNumberedListItem(StringBuilder sb, NumberedListItemBlock numberedListItem, string indent)
    {
        sb.Append($"{indent}1. ");

        foreach (var richText in numberedListItem.NumberedListItem.RichText)
        {
            AppendRichText(sb, richText);
        }
    }

    private void AppendBookmark(StringBuilder sb, BookmarkBlock bookmarkBlock, string indent)
    {
        var caption = bookmarkBlock.Bookmark.Caption.FirstOrDefault()?.PlainText;
        var url = bookmarkBlock.Bookmark.Url;

        sb.Append(indent);

        if (!string.IsNullOrEmpty(caption))
        {
            sb.Append($"[{caption}]({url})");
        }
        else
        {
            sb.Append($"<{url}>");
        }
    }

    private void AppendRichText(StringBuilder sb, RichTextBase richText)
    {
        var text = richText.PlainText;

        if (!string.IsNullOrEmpty(richText.Href))
        {
            text = $"[{text}]({richText.Href})";
        }

        if (richText.Annotations.IsCode)
        {
            text = $"`{text}`";
        }

        if (richText.Annotations.IsItalic && richText.Annotations.IsBold)
        {
            text = $"***{text}***";
        }
        else if (richText.Annotations.IsBold)
        {
            text = $"**{text}**";
        }
        else if (richText.Annotations.IsItalic)
        {
            text = $"*{text}*";
        }

        if (richText.Annotations.IsStrikeThrough)
        {
            text = $"~{text}~";
        }

        sb.Append(text);
    }
}
