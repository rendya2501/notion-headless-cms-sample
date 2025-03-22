// Program.cs - エントリポイント
using Notion.Client;
using NotionMarkdownExporter.Configuration;
using NotionMarkdownExporter.Models;
using NotionMarkdownExporter.Services;
using NotionMarkdownExporter.Utils;
using System.Security.Cryptography;
using System.Text;

var config = AppConfiguration.FromCommandLine(args);
var exporter = new NotionExporter(config);
await exporter.ExportPagesAsync();

// Configuration/AppConfiguration.cs - 設定管理
namespace NotionMarkdownExporter.Configuration
{
    public class AppConfiguration
    {
        public string NotionAuthToken { get; private set; }
        public string NotionDatabaseId { get; private set; }
        public string OutputDirectoryPathTemplate { get; private set; }

        public FrontMatterConfig FrontMatter { get; } = new FrontMatterConfig();
        public NotionPropertyConfig NotionProperties { get; } = new NotionPropertyConfig();

        public static AppConfiguration FromCommandLine(string[] args)
        {
            if (args.Length != 3)
            {
                throw new ArgumentException("Required arguments: [NotionAuthToken] [DatabaseId] [OutputPathTemplate]");
            }

            return new AppConfiguration
            {
                NotionAuthToken = args[0],
                NotionDatabaseId = args[1],
                OutputDirectoryPathTemplate = args[2]
            };
        }
    }

    public class FrontMatterConfig
    {
        public string TitleName { get; set; } = "title";
        public string TypeName { get; set; } = "type";
        public string PublishedName { get; set; } = "date";
        public string DescriptionName { get; set; } = "description";
        public string TagsName { get; set; } = "tags";
        public string EyecatchName { get; set; } = "eyecatch";
    }

    public class NotionPropertyConfig
    {
        public string TitlePropertyName { get; set; } = "Title";
        public string TypePropertyName { get; set; } = "Type";
        public string PublishedAtPropertyName { get; set; } = "PublishedAt";
        public string RequestPublishingPropertyName { get; set; } = "RequestPublishing";
        public string CrawledAtPropertyName { get; set; } = "_SystemCrawledAt";
        public string TagsPropertyName { get; set; } = "Tags";
        public string DescriptionPropertyName { get; set; } = "Description";
        public string SlugPropertyName { get; set; } = "Slug";
    }
}

// Services/NotionExporter.cs - メインのエクスポートロジック
namespace NotionMarkdownExporter.Services
{
    public class NotionExporter
    {
        private readonly AppConfiguration _config;
        private readonly NotionClientWrapper _notionClient;
        private readonly MarkdownGenerator _markdownGenerator;

        public NotionExporter(AppConfiguration config)
        {
            _config = config;
            _notionClient = new NotionClientWrapper(config.NotionAuthToken);
            _markdownGenerator = new MarkdownGenerator(config);
        }

        public async Task ExportPagesAsync()
        {
            var pages = await _notionClient.GetPagesForPublishingAsync(
                _config.NotionDatabaseId,
                _config.NotionProperties.RequestPublishingPropertyName);

            var now = DateTime.Now;
            var exportedCount = 0;

            foreach (var page in pages)
            {
                if (await ExportPageAsync(page, now))
                {
                    await _notionClient.UpdatePagePropertiesAsync(
                        page.Id,
                        _config.NotionProperties.CrawledAtPropertyName,
                        _config.NotionProperties.RequestPublishingPropertyName,
                        now);

                    exportedCount++;
                }
            }

            UpdateGitHubEnvironment(exportedCount);
        }

        private async Task<bool> ExportPageAsync(Page page, DateTime now)
        {
            var pageData = await _notionClient.ExtractPageDataAsync(page, _config.NotionProperties);

            if (!ShouldExportPage(pageData, now))
            {
                return false;
            }

            var outputDirectory = BuildOutputDirectory(pageData);
            Directory.CreateDirectory(outputDirectory);

            var markdown = await _markdownGenerator.GenerateMarkdownAsync(
                page,
                pageData,
                outputDirectory);

            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "index.md"),
                markdown,
                new UTF8Encoding(false));

            return true;
        }

        private bool ShouldExportPage(PageData pageData, DateTime now)
        {
            if (!pageData.RequestPublishing)
            {
                Console.WriteLine($"{pageData.PageId}(title = {pageData.Title}): No request publishing.");
                return false;
            }

            if (!pageData.PublishedDateTime.HasValue)
            {
                Console.WriteLine($"{pageData.PageId}(title = {pageData.Title}): Missing publish date.");
                return false;
            }

            if (now < pageData.PublishedDateTime.Value)
            {
                Console.WriteLine($"{pageData.PageId}(title = {pageData.Title}): Publication date not reached.");
                return false;
            }

            return true;
        }

        private string BuildOutputDirectory(PageData pageData)
        {
            var template = Scriban.Template.Parse(_config.OutputDirectoryPathTemplate);
            var slug = string.IsNullOrEmpty(pageData.Slug) ? pageData.Title : pageData.Slug;

            return template.Render(new
            {
                publish = pageData.PublishedDateTime.Value,
                title = pageData.Title,
                slug = slug
            });
        }

        private void UpdateGitHubEnvironment(int exportedCount)
        {
            var githubEnvPath = Environment.GetEnvironmentVariable("GITHUB_ENV");

            if (string.IsNullOrEmpty(githubEnvPath))
            {
                Console.WriteLine("Warning: GITHUB_ENV not set, skipping environment update.");
                return;
            }

            var exportCountLine = $"EXPORTED_COUNT={exportedCount}";
            File.AppendAllText(githubEnvPath, exportCountLine + Environment.NewLine);
            Console.WriteLine(exportCountLine);
        }
    }
}

// Services/NotionClientWrapper.cs - NotionクライアントのWrapper
namespace NotionMarkdownExporter.Services
{
    public class NotionClientWrapper
    {
        private readonly NotionClient _client;

        public NotionClientWrapper(string authToken)
        {
            _client = NotionClientFactory.Create(new ClientOptions
            {
                AuthToken = authToken
            });
        }

        public async Task<List<Page>> GetPagesForPublishingAsync(string databaseId, string requestPublishingPropertyName)
        {
            var allPages = new List<Page>();
            var filter = new CheckboxFilter(requestPublishingPropertyName, true);
            var pagination = await _client.Databases.QueryAsync(databaseId, new DatabasesQueryParameters
            {
                Filter = filter
            });

            do
            {
                allPages.AddRange(pagination.Results);

                if (!pagination.HasMore)
                {
                    break;
                }

                pagination = await _client.Databases.QueryAsync(databaseId, new DatabasesQueryParameters
                {
                    Filter = filter,
                    StartCursor = pagination.NextCursor
                });

            } while (true);

            return allPages;
        }

        public async Task<PageData> ExtractPageDataAsync(Page page, NotionPropertyConfig config)
        {
            var pageData = new PageData { PageId = page.Id };

            foreach (var property in page.Properties)
            {
                if (property.Key == config.PublishedAtPropertyName)
                {
                    if (PropertyParser.TryParseAsDateTime(property.Value, out var publishedAt))
                    {
                        pageData.PublishedDateTime = publishedAt;
                    }
                }
                else if (property.Key == config.CrawledAtPropertyName)
                {
                    if (PropertyParser.TryParseAsDateTime(property.Value, out var crawledAt))
                    {
                        pageData.LastCrawledDateTime = crawledAt;
                    }
                }
                else if (property.Key == config.SlugPropertyName)
                {
                    if (PropertyParser.TryParseAsPlainText(property.Value, out var slug))
                    {
                        pageData.Slug = slug;
                    }
                }
                else if (property.Key == config.TitlePropertyName)
                {
                    if (PropertyParser.TryParseAsPlainText(property.Value, out var title))
                    {
                        pageData.Title = title;
                    }
                }
                else if (property.Key == config.DescriptionPropertyName)
                {
                    if (PropertyParser.TryParseAsPlainText(property.Value, out var description))
                    {
                        pageData.Description = description;
                    }
                }
                else if (property.Key == config.TagsPropertyName)
                {
                    if (PropertyParser.TryParseAsStringList(property.Value, out var tags))
                    {
                        pageData.Tags = tags;
                    }
                }
                else if (property.Key == config.TypePropertyName)
                {
                    if (PropertyParser.TryParseAsPlainText(property.Value, out var type))
                    {
                        pageData.Type = type;
                    }
                }
                else if (property.Key == config.RequestPublishingPropertyName)
                {
                    if (PropertyParser.TryParseAsBoolean(property.Value, out var requestPublishing))
                    {
                        pageData.RequestPublishing = requestPublishing;
                    }
                }
            }

            if (page.Cover is UploadedFile coverImage)
            {
                pageData.CoverImageUrl = coverImage.File.Url;
            }

            return pageData;
        }

        public async Task UpdatePagePropertiesAsync(
            string pageId,
            string crawledAtProperty,
            string requestPublishingProperty,
            DateTime now)
        {
            await _client.Pages.UpdateAsync(pageId, new PagesUpdateParameters
            {
                Properties = new Dictionary<string, PropertyValue>
                {
                    [crawledAtProperty] = new DatePropertyValue
                    {
                        Date = new Date
                        {
                            Start = now
                        }
                    },
                    [requestPublishingProperty] = new CheckboxPropertyValue
                    {
                        Checkbox = false
                    }
                }
            });
        }

        public async Task<List<Block>> GetPageBlocksAsync(string pageId)
        {
            var allBlocks = new List<Block>();
            var pagination = await _client.Blocks.RetrieveChildrenAsync(pageId);

            do
            {
                allBlocks.AddRange(pagination.Results.Cast<Block>());

                if (!pagination.HasMore)
                {
                    break;
                }

                pagination = await _client.Blocks.RetrieveChildrenAsync(pageId, new BlocksRetrieveChildrenParameters
                {
                    StartCursor = pagination.NextCursor
                });

            } while (true);

            return allBlocks;
        }

        public async Task<List<Block>> GetChildBlocksAsync(string blockId)
        {
            var allBlocks = new List<Block>();
            var pagination = await _client.Blocks.RetrieveChildrenAsync(blockId);

            do
            {
                allBlocks.AddRange(pagination.Results.Cast<Block>());

                if (!pagination.HasMore)
                {
                    break;
                }

                pagination = await _client.Blocks.RetrieveChildrenAsync(blockId, new BlocksRetrieveChildrenParameters
                {
                    StartCursor = pagination.NextCursor
                });

            } while (true);

            return allBlocks;
        }
    }
}

// Models/PageData.cs - ページデータモデル
namespace NotionMarkdownExporter.Models
{
    public class PageData
    {
        public string PageId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public DateTime? PublishedDateTime { get; set; }
        public DateTime? LastCrawledDateTime { get; set; }
        public bool RequestPublishing { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
    }
}

// Utils/PropertyParser.cs - NotionプロパティのParser
namespace NotionMarkdownExporter.Utils
{
    public static class PropertyParser
    {
        public static bool TryParseAsDateTime(PropertyValue value, out DateTime dateTime)
        {
            dateTime = default;

            switch (value)
            {
                case DatePropertyValue dateProperty:
                    if (dateProperty.Date?.Start == null)
                    {
                        return false;
                    }
                    dateTime = dateProperty.Date.Start.Value;
                    return true;

                case CreatedTimePropertyValue createdTimeProperty:
                    return DateTime.TryParse(createdTimeProperty.CreatedTime, out dateTime);

                case LastEditedTimePropertyValue lastEditedTimeProperty:
                    return DateTime.TryParse(lastEditedTimeProperty.LastEditedTime, out dateTime);

                default:
                    if (TryParseAsPlainText(value, out var text) &&
                        DateTime.TryParse(text, out dateTime))
                    {
                        return true;
                    }
                    return false;
            }
        }

        public static bool TryParseAsPlainText(PropertyValue value, out string text)
        {
            text = string.Empty;

            switch (value)
            {
                case RichTextPropertyValue richTextProperty:
                    text = string.Join("", richTextProperty.RichText.Select(rt => rt.PlainText));
                    return true;

                case TitlePropertyValue titleProperty:
                    text = string.Join("", titleProperty.Title.Select(t => t.PlainText));
                    return true;

                case SelectPropertyValue selectProperty:
                    text = selectProperty.Select?.Name ?? string.Empty;
                    return true;

                default:
                    return false;
            }
        }

        public static bool TryParseAsStringList(PropertyValue value, out List<string> items)
        {
            items = new List<string>();

            if (value is MultiSelectPropertyValue multiSelectProperty)
            {
                items.AddRange(multiSelectProperty.MultiSelect.Select(s => s.Name));
                return true;
            }

            return false;
        }

        public static bool TryParseAsBoolean(PropertyValue value, out bool result)
        {
            result = false;

            if (value is CheckboxPropertyValue checkboxProperty)
            {
                result = checkboxProperty.Checkbox;
                return true;
            }

            return false;
        }
    }
}

// Services/MarkdownGenerator.cs - Markdownの生成
namespace NotionMarkdownExporter.Services
{
    public class MarkdownGenerator
    {
        private readonly AppConfiguration _config;
        private readonly NotionClientWrapper _notionClient;
        private readonly ImageDownloader _imageDownloader;

        public MarkdownGenerator(AppConfiguration config)
        {
            _config = config;
            _notionClient = new NotionClientWrapper(config.NotionAuthToken);
            _imageDownloader = new ImageDownloader();
        }

        public async Task<string> GenerateMarkdownAsync(Page page, PageData pageData, string outputDirectory)
        {
            var sb = new StringBuilder();

            // FrontMatterの生成
            await AppendFrontMatterAsync(sb, page, pageData, outputDirectory);

            // ページコンテンツの取得と変換
            var blocks = await _notionClient.GetPageBlocksAsync(page.Id);
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
                sb.AppendLine($"{_config.FrontMatter.TypeName}: \"{pageData.Type}\"");
            }

            sb.AppendLine($"{_config.FrontMatter.TitleName}: \"{pageData.Title}\"");

            if (!string.IsNullOrWhiteSpace(pageData.Description))
            {
                sb.AppendLine($"{_config.FrontMatter.DescriptionName}: \"{pageData.Description}\"");
            }

            if (pageData.Tags.Count > 0)
            {
                var formattedTags = pageData.Tags.Select(tag => $"\"{tag}\"");
                sb.AppendLine($"{_config.FrontMatter.TagsName}: [{string.Join(',', formattedTags)}]");
            }

            if (pageData.PublishedDateTime.HasValue)
            {
                sb.AppendLine($"{_config.FrontMatter.PublishedName}: \"{pageData.PublishedDateTime.Value:s}\"");
            }

            if (!string.IsNullOrEmpty(pageData.CoverImageUrl))
            {
                var (fileName, _) = await _imageDownloader.DownloadImageAsync(pageData.CoverImageUrl, outputDirectory);
                sb.AppendLine($"{_config.FrontMatter.EyecatchName}: \"./{fileName}\"");
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
                var childBlocks = await _notionClient.GetChildBlocksAsync(block.Id);
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
                var (fileName, _) = await _imageDownloader.DownloadImageAsync(url, outputDirectory);
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
}

// Utils/ImageDownloader.cs - 画像ダウンロード処理
namespace NotionMarkdownExporter.Utils
{
    public class ImageDownloader
    {
        public async Task<(string FileName, string FilePath)> DownloadImageAsync(string url, string outputDirectory)
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

                return (fileName, filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download image from {url}: {ex.Message}");
                return (string.Empty, string.Empty);
            }
        }
    }
}