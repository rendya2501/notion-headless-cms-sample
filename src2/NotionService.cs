using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Notion.Client;
using System.Security.Cryptography;
using System.Text;

namespace src2;

public class NotionService : INotionService
{
    private readonly NotionClient _client;

    public NotionService(NotionClient client)
    {
        _client = client;
    }

    public async Task<IEnumerable<Page>> GetPagesAsync(string databaseId, CheckboxFilter filter)
    {
        var pagination = await _client.Databases.QueryAsync(databaseId, new DatabasesQueryParameters { Filter = filter });
        var pages = new List<Page>();

        do
        {
            pages.AddRange(pagination.Results);
            if (!pagination.HasMore) break;
            pagination = await _client.Databases.QueryAsync(databaseId, new DatabasesQueryParameters { Filter = filter, StartCursor = pagination.NextCursor });
        } while (true);

        return pages;
    }

    /// <summary>
    /// 指定されたNotionページをMarkdown形式でエクスポートします。
    /// </summary>
    /// <param name="page">エクスポートするNotionページ</param>
    /// <param name="now">現在の日時</param>
    /// <param name="forceExport">強制的にエクスポートするかどうか</param>
    /// <returns>エクスポートが成功したかどうかを示すタスク</returns>
    public async Task<bool> ExportPageToMarkdownAsync(Page page, DateTime now, bool forceExport = false)
    {
        bool requestPublishing = false;
        string title = string.Empty;
        string type = string.Empty;
        string slug = page.Id;
        string description = string.Empty;
        List<string>? tags = null;
        DateTime? publishedDateTime = null;
        DateTime? lastSystemCrawledDateTime = null;

        // frontmatterを構築
        foreach (var property in page.Properties)
        {
            if (property.Key == notionPublishedAtPropertyName)
            {
                if (TryParsePropertyValueAsDateTime(property.Value, out var parsedPublishedAt))
                {
                    publishedDateTime = parsedPublishedAt;
                }
            }
            else if (property.Key == notionCrawledAtPropertyName)
            {
                if (TryParsePropertyValueAsDateTime(property.Value, out var parsedCrawledAt))
                {
                    lastSystemCrawledDateTime = parsedCrawledAt;
                }
            }
            else if (property.Key == notionSlugPropertyName)
            {
                if (TryParsePropertyValueAsPlainText(property.Value, out var parsedSlug))
                {
                    slug = parsedSlug;
                }
            }
            else if (property.Key == notionTitlePropertyName)
            {
                if (TryParsePropertyValueAsPlainText(property.Value, out var parsedTitle))
                {
                    title = parsedTitle;
                }
            }
            else if (property.Key == notionDescriptionPropertyName)
            {
                if (TryParsePropertyValueAsPlainText(property.Value, out var parsedDescription))
                {
                    description = parsedDescription;
                }
            }
            else if (property.Key == notionTagsPropertyName)
            {
                if (TryParsePropertyValueAsStringSet(property.Value, out var parsedTags))
                {
                    tags = parsedTags.Select(tag => $"\"{tag}\"").ToList();
                }
            }
            else if (property.Key == notionTypePropertyName)
            {
                if (TryParsePropertyValueAsPlainText(property.Value, out var parsedType))
                {
                    type = parsedType;
                }
            }
            else if (property.Key == notionRequestPublisingPropertyName)
            {
                if (TryParsePropertyValueAsBoolean(property.Value, out var parsedBoolean))
                {
                    requestPublishing = parsedBoolean;
                }
            }
        }

        if (!requestPublishing)
        {
            Console.WriteLine($"{page.Id}(title = {title}): No request publishing.");
            return false;
        }

        if (!publishedDateTime.HasValue)
        {
            Console.WriteLine($"{page.Id}(title = {title}): Skip updating becase this page don't have publish ate.");
            return false;
        }

        if (!forceExport)
        {
            if (now < publishedDateTime.Value)
            {
                Console.WriteLine($"{page.Id}(title = {title}): Skip updating because the publication date have not been reached");
                return false;
            }
        }

        slug = string.IsNullOrEmpty(slug) ? title : slug;
        var outputDirectory = BuildOutputDirectory(publishedDateTime.Value, title, slug);
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("---");

        if (!string.IsNullOrWhiteSpace(type))
        {
            stringBuilder.AppendLine($"{frontMatterTypeName}: \"{type}\"");
        }

        stringBuilder.AppendLine($"{frontMatterTitleName}: \"{title}\"");

        if (!string.IsNullOrWhiteSpace(description))
        {
            stringBuilder.AppendLine($"{frontMatterDescriptionName}: \"{description}\"");
        }
        if (tags is not null)
        {
            stringBuilder.AppendLine($"{frontMatterTagsName}: [{string.Join(',', tags)}]");
        }
        stringBuilder.AppendLine($"{frontMatterPublishedName}: \"{publishedDateTime.Value.ToString("s")}\"");

        if (page.Cover is not null && page.Cover is UploadedFile uploadedFile)
        {
            var (fileName, _) = await DownloadImage(uploadedFile.File.Url, outputDirectory);
            stringBuilder.AppendLine($"{frontMatterEyecatch}: \"./{fileName}\"");
        }

        stringBuilder.AppendLine("");
        stringBuilder.AppendLine("---");
        stringBuilder.AppendLine("");


        // ページの内容を追加
        var pagination = await CreateNotionClient().Blocks.RetrieveChildrenAsync(page.Id);
        do
        {
            foreach (Block block in pagination.Results.Cast<Block>())
            {
                await AppendBlockLineAsync(block, string.Empty, outputDirectory, stringBuilder);
            }

            if (!pagination.HasMore)
            {
                break;
            }

            pagination = await CreateNotionClient().Blocks.RetrieveChildrenAsync(page.Id, new BlocksRetrieveChildrenParameters
            {
                StartCursor = pagination.NextCursor,
            });
        } while (true);

        using var fileStream = File.OpenWrite($"{outputDirectory}/index.md");
        using var streamWriter = new StreamWriter(fileStream, new UTF8Encoding(false));
        await streamWriter.WriteAsync(stringBuilder.ToString());

        return true;
    }


    public async Task UpdatePagePropertiesAsync(string pageId, Dictionary<string, PropertyValue> properties)
    {
        await _client.Pages.UpdateAsync(pageId, new PagesUpdateParameters { Properties = properties });
    }
}
