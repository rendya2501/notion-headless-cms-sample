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
        await AppendFrontMatterAsync(sb, pageData, outputDirectory);

        // ページコンテンツの取得と変換
        var blocks = await notionClient.GetBlocksAsync(page.Id);
        var listCount = new Stack<int>();
        listCount.Push(0);
        await AppendBlocksAsync(sb, blocks, string.Empty, outputDirectory, listCount);

        return sb.ToString();
    }


    private async Task AppendFrontMatterAsync(
        StringBuilder sb,
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
            var fileName = await ImageDownloader.DownloadImageAsync(pageData.CoverImageUrl, outputDirectory);
            sb.AppendLine($"{config.FrontMatter.EyecatchName}: \"./{fileName}\"");
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }

    private async Task AppendBlocksAsync(
        StringBuilder sb,
        List<Block> blocks,
        string indent,
        string outputDirectory,
        Stack<int> listCounter)
    {
        Block? previousBlock = null;

        foreach (var block in blocks)
        {
            // 直前のブロックがリストで、今回がリストじゃない場合はリセット
            if (previousBlock is NumberedListItemBlock && block is not NumberedListItemBlock)
            {
                listCounter.Clear();
                listCounter.Push(0);
            }

            await AppendBlockAsync(sb, block, indent, outputDirectory, listCounter);

            previousBlock = block;
        }
    }

    private async Task AppendBlockAsync(
        StringBuilder sb,
        Block block,
        string indent,
        string outputDirectory,
        Stack<int> listCounters)
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
                AppendNumberedListItem(sb, numberedListItem, indent, listCounters);
                break;

            case BookmarkBlock bookmarkBlock:
                AppendBookmark(sb, bookmarkBlock, indent);
                break;

            case QuoteBlock quoteBlock:
                AppendQuote(sb, quoteBlock, indent);
                break;

            case CalloutBlock calloutBlock:
                AppendCallout(sb, calloutBlock, indent);
                break;

            case ToggleBlock toggleBlock:
                AppendToggle(sb, toggleBlock, indent);
                break;

            case DividerBlock _:
                sb.AppendLine($"{indent}---");
                break;

            case TableBlock _:
                // テーブルは子ブロックとして処理
                break;

            case TableRowBlock tableRow:
                AppendTableRow(sb, tableRow, indent);
                break;

            default:
                // 未対応のブロックタイプの場合はプレーンテキストを試みる
                AppendUnknownBlock(sb, block, indent);
                break;
        }

        // 子ブロックの処理
        if (block.HasChildren)
        {
            listCounters.Push(0);

            string childIndent = block switch
            {
                QuoteBlock or CalloutBlock => $"{indent}> ",
                BulletedListItemBlock or NumberedListItemBlock => $"{indent}    ",
                _ => indent
            };

            var childBlocks = await notionClient.GetBlocksAsync(block.Id);
            await AppendBlocksAsync(sb, childBlocks, childIndent, outputDirectory, listCounters);

            // 階層のカウンターを削除
            listCounters.Pop();
        }
    }

    private void AppendParagraph(StringBuilder sb, ParagraphBlock paragraphBlock, string indent)
    {
        var richTexts = paragraphBlock.Paragraph.RichText;
        if (!richTexts.Any())
        {
            sb.AppendLine(indent);
            return;
        }

        foreach (var richText in richTexts)
        {
            var lines = richText.PlainText.Split([Environment.NewLine, "\n"], StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    sb.AppendLine(indent);
                    continue;
                }
                sb.Append(indent);
                var tempRichText = new RichTextBase
                {
                    PlainText = line,
                    Annotations = richText.Annotations,
                    Href = richText.Href
                };
                AppendRichText(sb, tempRichText);
                sb.Append("  ");
                sb.AppendLine();
            }
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
            var fileName = await ImageDownloader.DownloadImageAsync(url, outputDirectory);
            sb.AppendLine($"{indent}![](./{fileName})");
        }
    }

    private void AppendCode(StringBuilder sb, CodeBlock codeBlock, string indent)
    {
        var language = MapCodeLanguage(codeBlock.Code.Language);
        sb.AppendLine($"{indent}``` {language}");

        foreach (var richText in codeBlock.Code.RichText)
        {
            var lines = richText.PlainText.Split([Environment.NewLine, "\n"], StringSplitOptions.None);
            foreach (var line in lines)
            {
                sb.Append(indent);
                var tempRichText = new RichTextBase
                {
                    PlainText = line.Replace("\t", "    "),
                    Annotations = richText.Annotations,
                    Href = richText.Href
                };
                AppendRichText(sb, tempRichText);
                sb.AppendLine();
            }
        }

        sb.AppendLine($"{indent}```");

        static string MapCodeLanguage(string notionLanguage)
        {
            return notionLanguage switch
            {
                "c#" => "csharp",
                _ => notionLanguage
            };
        }
    }


    private void AppendBulletListItem(StringBuilder sb, BulletedListItemBlock bulletListItem, string indent)
    {
        sb.Append($"{indent}- ");

        foreach (var richText in bulletListItem.BulletedListItem.RichText)
        {
            var lines = richText.PlainText
                .Split([Environment.NewLine, "\n"], StringSplitOptions.None)
                .Select((text, index) => new { text, index });
            foreach (var line in lines)
            {
                var tempRichText = new RichTextBase
                {
                    PlainText = line.text,
                    Annotations = richText.Annotations,
                    Href = richText.Href
                };
                AppendRichText(sb, tempRichText);
                if (!string.IsNullOrWhiteSpace(tempRichText.PlainText)) sb.Append("  ");
                // 最後の要素以外に対して改行を追加
                if (line.index < lines.Count() - 1)
                {
                    sb.AppendLine();
                    sb.Append($"{indent}  ");
                }
            }
        }

        sb.AppendLine();
    }

    private void AppendNumberedListItem(StringBuilder sb, NumberedListItemBlock numberedListItem, string indent, Stack<int> listCounters)
    {
        listCounters.Push(listCounters.Pop() + 1);
        int currentNumber = listCounters.Peek();

        sb.Append($"{indent}{currentNumber}. ");

        foreach (var richText in numberedListItem.NumberedListItem.RichText)
        {
            var lines = richText.PlainText
                .Split([Environment.NewLine, "\n"], StringSplitOptions.None)
                .Select((text, index) => new { text, index });
            foreach (var line in lines)
            {
                var tempRichText = new RichTextBase
                {
                    PlainText = line.text,
                    Annotations = richText.Annotations,
                    Href = richText.Href
                };
                AppendRichText(sb, tempRichText);
                if (!string.IsNullOrWhiteSpace(tempRichText.PlainText)) sb.Append("  ");
                // 最後の要素以外に対して改行を追加
                if (line.index < lines.Count() - 1)
                {
                    sb.AppendLine();
                    sb.Append($"{indent}   ");
                }
            }
        }

        sb.AppendLine();
    }

    private void AppendBookmark(StringBuilder sb, BookmarkBlock bookmarkBlock, string indent)
    {
        var caption = bookmarkBlock.Bookmark.Caption.FirstOrDefault()?.PlainText;
        var url = bookmarkBlock.Bookmark.Url;

        sb.Append(indent);

        if (!string.IsNullOrWhiteSpace(caption))
        {
            sb.Append($"[{caption}]({url})");
            return;
        }

        sb.Append($"<{url}>");
    }

    private void AppendQuote(StringBuilder sb, QuoteBlock quoteBlock, string indent)
    {
        foreach (var richText in quoteBlock.Quote.RichText)
        {
            var lines = richText.PlainText.Split([Environment.NewLine, "\n"], StringSplitOptions.None);
            foreach (var line in lines)
            {
                sb.Append($"{indent}> ");
                var tempRichText = new RichTextBase
                {
                    PlainText = line,
                    Annotations = richText.Annotations,
                    Href = richText.Href
                };
                AppendRichText(sb, tempRichText);
                if (!string.IsNullOrWhiteSpace(tempRichText.PlainText)) sb.Append("  ");
                sb.AppendLine();
            }
        }
    }

    private void AppendCallout(StringBuilder sb, CalloutBlock calloutBlock, string indent)
    {
        foreach (var richText in calloutBlock.Callout.RichText)
        {
            var lines = richText.PlainText.Split([Environment.NewLine, "\n"], StringSplitOptions.None);
            foreach (var line in lines)
            {
                sb.Append($"{indent}> ");
                var tempRichText = new RichTextBase
                {
                    PlainText = line,
                    Annotations = richText.Annotations,
                    Href = richText.Href
                };
                AppendRichText(sb, tempRichText);
                if (!string.IsNullOrWhiteSpace(tempRichText.PlainText)) sb.Append("  ");
                sb.AppendLine();
            }
        }
    }

    private void AppendToggle(StringBuilder sb, ToggleBlock toggleBlock, string indent)
    {
        // トグルをマークダウンの詳細表示として変換（<details>）
        sb.AppendLine($"{indent}<details>");
        sb.Append($"{indent}<summary>");

        foreach (var richText in toggleBlock.Toggle.RichText)
        {
            AppendRichText(sb, richText);
        }

        sb.AppendLine("</summary>");
        // 子要素はAppendBlockAsyncで処理される
        sb.AppendLine($"{indent}</details>");
    }

    private void AppendTableRow(StringBuilder sb, TableRowBlock tableRow, string indent)
    {
        sb.Append(indent);
        sb.Append("| ");

        foreach (var cell in tableRow.TableRow.Cells)
        {
            foreach (var richText in cell)
            {
                AppendRichText(sb, richText);
            }
            sb.Append(" | ");
        }

        sb.AppendLine();
    }

    private void AppendUnknownBlock(StringBuilder sb, Block block, string indent)
    {
        sb.Append(indent);
        sb.Append("<!-- Unsupported block type: ");
        sb.Append(block.Type);
        sb.Append(" -->");
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
