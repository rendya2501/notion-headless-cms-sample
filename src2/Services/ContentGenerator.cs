using hoge.Configuration;
using hoge.Models;
using hoge.Utils;
using Notion.Client;
using System.Text;

namespace hoge.Services;

public class ContentGenerator(AppConfiguration config, INotionClientWrapper notionClient) : IContentGenerator
{
    private readonly StringBuilder sb = new();

    private readonly Dictionary<Type, Func<Context, string>> transformers = new()
    {
        { typeof(BookmarkBlock), Transformer.CreateMarkdownBookmarkTransformer() },
        { typeof(BreadcrumbBlock), Transformer.CreateMarkdownBreadcrumbTransformer() },
        { typeof(CalloutBlock), Transformer.CreateMarkdownCalloutTransformer() },
        { typeof(CodeBlock), Transformer.CreateMarkdownCodeTransformer() },
        { typeof(ColumnListBlock), Transformer.CreateMarkdownColumnListTransformer() },
        { typeof(DividerBlock), Transformer.CreateMarkdownDividerTransformer() },
        { typeof(EquationBlock), Transformer.CreateMarkdownEquationTransformer() },
        { typeof(HeadingOneBlock), Transformer.CreateMarkdownHeadingTransformer() },
        { typeof(HeadingTwoBlock), Transformer.CreateMarkdownHeadingTransformer() },
        { typeof(HeadingThreeBlock), Transformer.CreateMarkdownHeadingTransformer() },
        { typeof(LinkPreviewBlock), Transformer.CreateMarkdownLinkPreviewTransformer() },
        { typeof(BulletedListItemBlock), Transformer.CreateMarkdownBulletedListItemTransformer() },
        { typeof(NumberedListItemBlock), Transformer.CreateMarkdownNumberedListItemTransformer() },
        { typeof(ToDoBlock), Transformer.CreateMarkdownTodoListItemTransformer() },
        { typeof(ParagraphBlock), Transformer.CreateMarkdownParagraphTransformer() },
        { typeof(QuoteBlock), Transformer.CreateMarkdownQuoteTransformer() },
        { typeof(SyncedBlockBlock), Transformer.CreateMarkdownSyncedBlockTransformer() },
        { typeof(TableOfContentsBlock), Transformer.CreateMarkdownTableOfContentsTransformer() },
        { typeof(TableBlock), Transformer.CreateMarkdownTableTransformer() },
        { typeof(ToggleBlock), Transformer.CreateMarkdownToggleTransformer() },
        { typeof(ImageBlock), Transformer.CreateMarkdownImageTransformer() },
        { typeof(VideoBlock), Transformer.CreateMarkdownVideoTransformer() },
        { typeof(EmbedBlock), Transformer.CreateMarkdownEmbedTransformer() }
    };

    //private readonly INotionClientWrapper notionClient;
    //private readonly AppConfiguration config;

    //public ContentGenerator(
    //    AppConfiguration config,
    //    INotionClientWrapper notionClient)
    //{
    //    this.config = config;
    //    this.notionClient = notionClient;
    //    //transformers = new Dictionary<Type, Func<Context, string>>
    //    //{
    //    //    { typeof(BookmarkBlock), Transformer.CreateMarkdownBookmarkTransformer() },
    //    //    { typeof(BreadcrumbBlock), Transformer.CreateMarkdownBreadcrumbTransformer() },
    //    //    { typeof(CalloutBlock), Transformer.CreateMarkdownCalloutTransformer() },
    //    //    { typeof(CodeBlock), Transformer.CreateMarkdownCodeTransformer() },
    //    //    { typeof(ColumnListBlock), Transformer.CreateMarkdownColumnListTransformer() },
    //    //    { typeof(DividerBlock), Transformer.CreateMarkdownDividerTransformer() },
    //    //    { typeof(EquationBlock), Transformer.CreateMarkdownEquationTransformer() },
    //    //    { typeof(HeadingOneBlock), Transformer.CreateMarkdownHeadingTransformer() },
    //    //    { typeof(HeadingTwoBlock), Transformer.CreateMarkdownHeadingTransformer() },
    //    //    { typeof(HeadingThreeBlock), Transformer.CreateMarkdownHeadingTransformer() },
    //    //    { typeof(LinkPreviewBlock), Transformer.CreateMarkdownLinkPreviewTransformer() },
    //    //    { typeof(BulletedListItemBlock), Transformer.CreateMarkdownBulletedListItemTransformer() },
    //    //    { typeof(NumberedListItemBlock), Transformer.CreateMarkdownNumberedListItemTransformer() },
    //    //    { typeof(ToDoBlock), Transformer.CreateMarkdownTodoListItemTransformer() },
    //    //    { typeof(ParagraphBlock), Transformer.CreateMarkdownParagraphTransformer() },
    //    //    { typeof(QuoteBlock), Transformer.CreateMarkdownQuoteTransformer() },
    //    //    { typeof(SyncedBlockBlock), Transformer.CreateMarkdownSyncedBlockTransformer() },
    //    //    { typeof(TableOfContentsBlock), Transformer.CreateMarkdownTableOfContentsTransformer() },
    //    //    { typeof(TableBlock), Transformer.CreateMarkdownTableTransformer() },
    //    //    { typeof(ToggleBlock), Transformer.CreateMarkdownToggleTransformer() },
    //    //    { typeof(ImageBlock), Transformer.CreateMarkdownImageTransformer() },
    //    //    { typeof(VideoBlock), Transformer.CreateMarkdownVideoTransformer() },
    //    //    { typeof(EmbedBlock), Transformer.CreateMarkdownEmbedTransformer() }
    //    //};
    //}

    public async Task<StringBuilder> GenerateContentAsync(string pageId, string outputDirectory)
    {
        // ページのブロックを取得
        var blocks = await notionClient.GetPageFullContent(pageId);
        // await AppendBlocksAsync2(blocks, string.Empty);

        var result = Execute(blocks);

        return new StringBuilder(result);
    }

    private string Execute(List<NotionBlock> blocks)
    {
        var context = new Context
        {
            Execute = Execute,
            Blocks = blocks,
            CurrentBlock = blocks.FirstOrDefault(),
            CurrentBlockIndex = 0
        };

        var transformedBlocks = new List<string>();

        foreach (var (block, index) in blocks.Select((block, index) => (block, index)))
        {
            context.CurrentBlock = block;
            context.CurrentBlockIndex = index;

            var originalBlock = block.GetOriginalBlock<Block>();
            if (transformers.TryGetValue(originalBlock.GetType(), out var transformer))
            {
                var transformed = transformer(context);
                transformedBlocks.Add(transformed);
            }
        }

        //for (int i = 0; i < blocks.Count; i++)
        //{
        //    var block = blocks[i];
        //    context.CurrentBlock = block;
        //    context.CurrentBlockIndex = i;

        //    var originalBlock = block.GetOriginalBlock<Block>();
        //    if (transformers.TryGetValue(originalBlock.GetType(), out var transformer))
        //    {
        //        var transformed = transformer(context);
        //        transformedBlocks.Add(transformed);
        //    }
        //}

        return string.Join("\n", transformedBlocks.Where(b => !string.IsNullOrEmpty(b)));
    }

    private static PageData CopyPageProperties(Page page)
    {
        var pageData = new PageData
        {
            Id = page.Id,
            CreatedTime = page.CreatedTime,
            LastEditedTime = page.LastEditedTime,
            Archived = page.Archived,
            Icon = page.Icon switch
            {
                EmojiObject emoji => emoji.Emoji,
                FileObject file => file.File.Url,
                _ => string.Empty
            },
            Cover = page.Cover switch
            {
                ExternalFileObject external => external.External.Url,
                FileObject file => file.File.Url,
                _ => string.Empty
            }
        };

        foreach (var property in page.Properties)
        {
            pageData.Properties[property.Key] = property.Value switch
            {
                TitlePropertyValue title => title.Title.FirstOrDefault()?.PlainText ?? string.Empty,
                RichTextPropertyValue richText => richText.RichText.FirstOrDefault()?.PlainText ?? string.Empty,
                NumberPropertyValue number => number.Number?.ToString() ?? string.Empty,
                SelectPropertyValue select => select.Select?.Name ?? string.Empty,
                MultiSelectPropertyValue multiSelect => string.Join(",", multiSelect.MultiSelect.Select(x => x.Name)),
                DatePropertyValue date => date.Date?.Start.ToString() ?? string.Empty,
                PeoplePropertyValue people => string.Join(",", people.People.Select(x => x.Name)),
                FilesPropertyValue files => string.Join(",", files.Files.Select(x => x.Name)),
                CheckboxPropertyValue checkbox => checkbox.Checkbox.ToString(),
                EmailPropertyValue email => email.Email ?? string.Empty,
                PhoneNumberPropertyValue phone => phone.PhoneNumber ?? string.Empty,
                UrlPropertyValue url => url.Url ?? string.Empty,
                _ => string.Empty
            };
        }

        return pageData;
    }

    ///// <summary>
    ///// ブロックを追加します。
    ///// </summary>
    ///// <param name="sb"></param>
    ///// <param name="blocks"></param>
    ///// <param name="indent"></param>
    ///// <param name="outputDirectory"></param>
    ///// <param name="listCounter"></param>
    ///// <returns></returns>
    //private async Task AppendBlocksAsync2(List<NotionBlock> blocks, string indent)
    //{
    //    foreach (var (block, currentIindex) in blocks.Select((block, index) => (block, index)))
    //    {
    //        Block originalBlock = block.GetOriginalBlock<Block>();
    //        switch (originalBlock)
    //        {
    //            case ParagraphBlock paragraphBlock:
    //                AppendParagraph(paragraphBlock, indent);
    //                break;

    //            case HeadingOneBlock h1:
    //                AppendHeading(h1.Heading_1.RichText, indent, "# ");
    //                break;

    //            case HeadingTwoBlock h2:
    //                AppendHeading(h2.Heading_2.RichText, indent, "## ");
    //                break;

    //            case HeadingThreeBlock h3:
    //                AppendHeading(h3.Heading_3.RichText, indent, "### ");
    //                break;

    //            case ImageBlock imageBlock:
    //                await AppendImageAsync(imageBlock, indent, config.OutputDirectory);
    //                break;

    //            case CodeBlock codeBlock:
    //                AppendCode(codeBlock, indent);
    //                break;

    //            case BulletedListItemBlock bulletListItem:
    //                AppendBulletListItem(bulletListItem, indent);
    //                break;

    //            case NumberedListItemBlock numberedListItem:
    //                var beforeBlocks = blocks.GetRange(0, currentIindex);
    //                var listCount = 1;
    //                for (var index = beforeBlocks.Count - 1; index >= 0; index--)
    //                {
    //                    var previousBlock = beforeBlocks[index].GetOriginalBlock<Block>();
    //                    if (previousBlock is not NumberedListItemBlock)
    //                    {
    //                        break;
    //                    }
    //                    listCount++;
    //                }

    //                sb.Append($"{indent}{listCount}. ");

    //                foreach (var richText in numberedListItem.NumberedListItem.RichText)
    //                {
    //                    var lines = richText.PlainText
    //                        .Split([Environment.NewLine, "\n"], StringSplitOptions.None)
    //                        .Select((text, index) => new { text, index });
    //                    foreach (var line in lines)
    //                    {
    //                        var tempRichText = new RichTextBase
    //                        {
    //                            PlainText = line.text,
    //                            Annotations = richText.Annotations,
    //                            Href = richText.Href
    //                        };
    //                        AppendRichText(tempRichText);
    //                        if (!string.IsNullOrWhiteSpace(tempRichText.PlainText)) sb.Append("  ");
    //                        if (line.index < lines.Count() - 1)
    //                        {
    //                            sb.AppendLine();
    //                            sb.Append($"{indent}   ");
    //                        }
    //                    }
    //                }

    //                sb.AppendLine();
    //                break;

    //            case BookmarkBlock bookmarkBlock:
    //                AppendBookmark(bookmarkBlock, indent);
    //                break;

    //            case QuoteBlock quoteBlock:
    //                AppendQuote(quoteBlock, indent);
    //                break;

    //            case CalloutBlock calloutBlock:
    //                AppendCallout(calloutBlock, indent);
    //                break;

    //            case ToggleBlock toggleBlock:
    //                AppendToggle(toggleBlock, indent);
    //                break;

    //            case DividerBlock _:
    //                sb.AppendLine($"{indent}---");
    //                break;

    //            case TableBlock _:
    //                // テーブルは子ブロックとして処理
    //                break;

    //            case TableRowBlock tableRow:
    //                AppendTableRow(tableRow, indent);
    //                break;

    //            default:
    //                // 未対応のブロックタイプの場合はプレーンテキストを試みる
    //                AppendUnknownBlock(originalBlock, indent);
    //                break;
    //        }

    //        // 子ブロックの処理
    //        if (block.HasChildren)
    //        {
    //            string childIndent = originalBlock switch
    //            {
    //                QuoteBlock or CalloutBlock => $"{indent}> ",
    //                BulletedListItemBlock or NumberedListItemBlock => $"{indent}    ",
    //                _ => indent
    //            };

    //            await AppendBlocksAsync2(block.Children, childIndent);
    //        }
    //    }
    //}

    ///// <summary>
    ///// ブロックを追加します。
    ///// </summary>
    ///// <param name="sb"></param>
    ///// <param name="blocks"></param>
    ///// <param name="indent"></param>
    ///// <param name="outputDirectory"></param>
    ///// <param name="listCounter"></param>
    ///// <returns></returns>
    //private async Task AppendBlocksAsync(List<Block> blocks, string indent, string outputDirectory, Stack<int> listCounter)
    //{
    //    Block? previousBlock = null;

    //    foreach (var block in blocks)
    //    {
    //        var notionBlock = NotionBlock.FromBlock(block);
    //        // 直前のブロックがリストで、今回がリストじゃない場合はリセット
    //        if (previousBlock is NumberedListItemBlock && block is not NumberedListItemBlock)
    //        {
    //            listCounter.Clear();
    //            listCounter.Push(0);
    //        }

    //        await AppendBlockAsync(notionBlock, indent, outputDirectory, listCounter);

    //        previousBlock = block;
    //    }
    //}

    ///// <summary>
    ///// ブロックを追加します。
    ///// </summary>
    ///// <param name="sb"></param>
    ///// <param name="block"></param>
    ///// <param name="indent"></param>
    ///// <param name="outputDirectory"></param>
    ///// <param name="listCounters"></param>
    ///// <returns></returns>
    //private async Task AppendBlockAsync(NotionBlock block, string indent, string outputDirectory, Stack<int> listCounters)
    //{
    //    var originalBlock = block.GetOriginalBlock<Block>();
    //    switch (originalBlock)
    //    {
    //        case ParagraphBlock paragraphBlock:
    //            AppendParagraph(paragraphBlock, indent);
    //            break;

    //        case HeadingOneBlock h1:
    //            AppendHeading(h1.Heading_1.RichText, indent, "# ");
    //            break;

    //        case HeadingTwoBlock h2:
    //            AppendHeading(h2.Heading_2.RichText, indent, "## ");
    //            break;

    //        case HeadingThreeBlock h3:
    //            AppendHeading(h3.Heading_3.RichText, indent, "### ");
    //            break;

    //        case ImageBlock imageBlock:
    //            await AppendImage(imageBlock, indent, outputDirectory);
    //            break;

    //        case CodeBlock codeBlock:
    //            AppendCode(codeBlock, indent);
    //            break;

    //        case BulletedListItemBlock bulletListItem:
    //            AppendBulletListItem(bulletListItem, indent);
    //            break;

    //        case NumberedListItemBlock numberedListItem:
    //            AppendNumberedListItem(numberedListItem, indent, listCounters);
    //            break;

    //        case BookmarkBlock bookmarkBlock:
    //            AppendBookmark(bookmarkBlock, indent);
    //            break;

    //        case QuoteBlock quoteBlock:
    //            AppendQuote(quoteBlock, indent);
    //            break;

    //        case CalloutBlock calloutBlock:
    //            AppendCallout(calloutBlock, indent);
    //            break;

    //        case ToggleBlock toggleBlock:
    //            AppendToggle(toggleBlock, indent);
    //            break;

    //        case DividerBlock _:
    //            sb.AppendLine($"{indent}---");
    //            break;

    //        case TableBlock _:
    //            // テーブルは子ブロックとして処理
    //            break;

    //        case TableRowBlock tableRow:
    //            AppendTableRow(tableRow, indent);
    //            break;

    //        default:
    //            // 未対応のブロックタイプの場合はプレーンテキストを試みる
    //            AppendUnknownBlock(originalBlock, indent);
    //            break;
    //    }

    //    // 子ブロックの処理
    //    if (block.HasChildren)
    //    {
    //        listCounters.Push(0);

    //        string childIndent = originalBlock switch
    //        {
    //            QuoteBlock or CalloutBlock => $"{indent}> ",
    //            BulletedListItemBlock or NumberedListItemBlock => $"{indent}    ",
    //            _ => indent
    //        };

    //        var childBlocks = await notionClient.GetBlocksAsync(block.Id);
    //        await AppendBlocksAsync(childBlocks, childIndent, outputDirectory, listCounters);

    //        // 階層のカウンターを削除
    //        listCounters.Pop();
    //    }
    //}

    ///// <summary>
    ///// 段落を追加します。
    ///// </summary>
    ///// <param name="paragraphBlock"></param>
    ///// <param name="indent"></param>
    //private void AppendParagraph(ParagraphBlock paragraphBlock, string indent)
    //{
    //    // リッチテキストがない場合は空行を追加
    //    var richTexts = paragraphBlock.Paragraph.RichText;
    //    if (!richTexts.Any())
    //    {
    //        sb.AppendLine(indent);
    //        return;
    //    }

    //    foreach (var richText in richTexts)
    //    {
    //        var lines = richText.PlainText.Split([Environment.NewLine, "\n"], StringSplitOptions.None);
    //        foreach (var line in lines)
    //        {
    //            sb.Append(indent);
    //            // リッチテキストの改行はマークダウンの改行に変換
    //            if (string.IsNullOrWhiteSpace(line))
    //            {
    //                continue;
    //            }
    //            var tempRichText = new RichTextBase
    //            {
    //                PlainText = line,
    //                Annotations = richText.Annotations,
    //                Href = richText.Href
    //            };
    //            AppendRichText(tempRichText);
    //            // 文字列の末尾にマークダウンの改行であるスペース2つを追記
    //            sb.Append("  ");
    //            sb.AppendLine();
    //        }
    //    }
    //}

    //private void AppendHeading(IEnumerable<RichTextBase> richTexts, string indent, string headingPrefix)
    //{
    //    sb.Append($"{indent}{headingPrefix}");

    //    foreach (var richText in richTexts)
    //    {
    //        AppendRichText(richText);
    //    }
    //}

    //private async Task AppendImage(ImageBlock imageBlock, string indent, string outputDirectory)
    //{
    //    var url = imageBlock.Image switch
    //    {
    //        ExternalFile externalFile => externalFile.External.Url,
    //        UploadedFile uploadedFile => uploadedFile.File.Url,
    //        _ => string.Empty
    //    };

    //    if (!string.IsNullOrEmpty(url))
    //    {
    //        var fileName = await ImageDownloader.DownloadImageAsync(url, outputDirectory);
    //        sb.AppendLine($"{indent}![](./{fileName})");
    //    }
    //}

    //private void AppendCode(CodeBlock codeBlock, string indent)
    //{
    //    var language = MapCodeLanguage(codeBlock.Code.Language);
    //    sb.AppendLine($"{indent}``` {language}");

    //    foreach (var richText in codeBlock.Code.RichText)
    //    {
    //        var lines = richText.PlainText.Split([Environment.NewLine, "\n"], StringSplitOptions.None);
    //        foreach (var line in lines)
    //        {
    //            sb.Append(indent);
    //            var tempRichText = new RichTextBase
    //            {
    //                PlainText = line.Replace("\t", "    "),
    //                Annotations = richText.Annotations,
    //                Href = richText.Href
    //            };
    //            AppendRichText(tempRichText);
    //            sb.AppendLine();
    //        }
    //    }

    //    sb.AppendLine($"{indent}```");

    //    static string MapCodeLanguage(string notionLanguage)
    //    {
    //        return notionLanguage switch
    //        {
    //            "c#" => "csharp",
    //            _ => notionLanguage
    //        };
    //    }
    //}

    //private void AppendBulletListItem(BulletedListItemBlock bulletListItem, string indent)
    //{
    //    sb.Append($"{indent}- ");

    //    foreach (var richText in bulletListItem.BulletedListItem.RichText)
    //    {
    //        var lines = richText.PlainText
    //            .Split([Environment.NewLine, "\n"], StringSplitOptions.None)
    //            .Select((text, index) => new { text, index });
    //        foreach (var line in lines)
    //        {
    //            var tempRichText = new RichTextBase
    //            {
    //                PlainText = line.text,
    //                Annotations = richText.Annotations,
    //                Href = richText.Href
    //            };
    //            AppendRichText(tempRichText);
    //            if (!string.IsNullOrWhiteSpace(tempRichText.PlainText)) sb.Append("  ");
    //            // 最後の要素以外に対して改行を追加
    //            if (line.index < lines.Count() - 1)
    //            {
    //                sb.AppendLine();
    //                sb.Append($"{indent}  ");
    //            }
    //        }
    //    }

    //    sb.AppendLine();
    //}

    //private void AppendNumberedListItem(NumberedListItemBlock numberedListItem, string indent, Stack<int> listCounters)
    //{
    //    listCounters.Push(listCounters.Pop() + 1);
    //    int currentNumber = listCounters.Peek();

    //    sb.Append($"{indent}{currentNumber}. ");

    //    foreach (var richText in numberedListItem.NumberedListItem.RichText)
    //    {
    //        var lines = richText.PlainText
    //            .Split([Environment.NewLine, "\n"], StringSplitOptions.None)
    //            .Select((text, index) => new { text, index });
    //        foreach (var line in lines)
    //        {
    //            var tempRichText = new RichTextBase
    //            {
    //                PlainText = line.text,
    //                Annotations = richText.Annotations,
    //                Href = richText.Href
    //            };
    //            AppendRichText(tempRichText);
    //            if (!string.IsNullOrWhiteSpace(tempRichText.PlainText)) sb.Append("  ");
    //            // 最後の要素以外に対して改行を追加
    //            if (line.index < lines.Count() - 1)
    //            {
    //                sb.AppendLine();
    //                sb.Append($"{indent}   ");
    //            }
    //        }
    //    }

    //    sb.AppendLine();
    //}

    //private void AppendBookmark(BookmarkBlock bookmarkBlock, string indent)
    //{
    //    var caption = bookmarkBlock.Bookmark.Caption.FirstOrDefault()?.PlainText;
    //    var url = bookmarkBlock.Bookmark.Url;

    //    sb.Append(indent);

    //    if (!string.IsNullOrWhiteSpace(caption))
    //    {
    //        sb.Append($"[{caption}]({url})");
    //        return;
    //    }

    //    sb.Append($"<{url}>");
    //}

    //private void AppendQuote(QuoteBlock quoteBlock, string indent)
    //{
    //    foreach (var richText in quoteBlock.Quote.RichText)
    //    {
    //        var lines = richText.PlainText.Split([Environment.NewLine, "\n"], StringSplitOptions.None);
    //        foreach (var line in lines)
    //        {
    //            sb.Append($"{indent}> ");
    //            var tempRichText = new RichTextBase
    //            {
    //                PlainText = line,
    //                Annotations = richText.Annotations,
    //                Href = richText.Href
    //            };
    //            AppendRichText(tempRichText);
    //            if (!string.IsNullOrWhiteSpace(tempRichText.PlainText)) sb.Append("  ");
    //            sb.AppendLine();
    //        }
    //    }
    //}

    //private void AppendCallout(CalloutBlock calloutBlock, string indent)
    //{
    //    foreach (var richText in calloutBlock.Callout.RichText)
    //    {
    //        var lines = richText.PlainText.Split([Environment.NewLine, "\n"], StringSplitOptions.None);
    //        foreach (var line in lines)
    //        {
    //            sb.Append($"{indent}> ");
    //            var tempRichText = new RichTextBase
    //            {
    //                PlainText = line,
    //                Annotations = richText.Annotations,
    //                Href = richText.Href
    //            };
    //            AppendRichText(tempRichText);
    //            if (!string.IsNullOrWhiteSpace(tempRichText.PlainText)) sb.Append("  ");
    //            sb.AppendLine();
    //        }
    //    }
    //}

    //private void AppendToggle(ToggleBlock toggleBlock, string indent)
    //{
    //    // トグルをマークダウンの詳細表示として変換（<details>）
    //    sb.AppendLine($"{indent}<details>");
    //    sb.Append($"{indent}<summary>");

    //    foreach (var richText in toggleBlock.Toggle.RichText)
    //    {
    //        AppendRichText(richText);
    //    }

    //    sb.AppendLine("</summary>");
    //    // 子要素はAppendBlockAsyncで処理される
    //    sb.AppendLine($"{indent}</details>");
    //}

    //private void AppendTableRow(TableRowBlock tableRow, string indent)
    //{
    //    sb.Append(indent);
    //    sb.Append("| ");

    //    foreach (var cell in tableRow.TableRow.Cells)
    //    {
    //        foreach (var richText in cell)
    //        {
    //            AppendRichText(richText);
    //        }
    //        sb.Append(" | ");
    //    }

    //    sb.AppendLine();
    //}

    //private void AppendUnknownBlock(Block block, string indent)
    //{
    //    sb.Append(indent);
    //    sb.Append("<!-- Unsupported block type: ");
    //    sb.Append(block.Type);
    //    sb.Append(" -->");
    //}

    //private void AppendRichText(RichTextBase richText)
    //{
    //    var text = richText.PlainText;

    //    if (!string.IsNullOrEmpty(richText.Href))
    //    {
    //        text = $"[{text}]({richText.Href})";
    //    }

    //    if (richText.Annotations.IsCode)
    //    {
    //        text = $"`{text}`";
    //    }

    //    if (richText.Annotations.IsItalic && richText.Annotations.IsBold)
    //    {
    //        text = $"***{text}***";
    //    }
    //    else if (richText.Annotations.IsBold)
    //    {
    //        text = $"**{text}**";
    //    }
    //    else if (richText.Annotations.IsItalic)
    //    {
    //        text = $"*{text}*";
    //    }

    //    if (richText.Annotations.IsStrikeThrough)
    //    {
    //        text = $"~{text}~";
    //    }

    //    sb.Append(text);
    //}

    //private async Task AppendImageAsync(ImageBlock imageBlock, string indent, string outputDirectory)
    //{
    //    var url = string.Empty;
    //    switch (imageBlock.Image)
    //    {
    //        case ExternalFile externalFile:
    //            url = externalFile.External.Url;
    //            break;
    //        case UploadedFile uploadedFile:
    //            url = uploadedFile.File.Url;
    //            break;
    //    }

    //    if (!string.IsNullOrEmpty(url))
    //    {
    //        var fileName = await ImageDownloader.DownloadImageAsync(url, outputDirectory);
    //        if (!string.IsNullOrEmpty(fileName))
    //        {
    //            sb.AppendLine($"{indent}![](./{fileName})");
    //        }
    //    }
    //}
}
