using hoge.Models;
using hoge.Utils;
using Notion.Client;

namespace hoge.Services;

public class NotionClientWrapper(INotionClient client) : INotionClientWrapper
{
    /// <summary>
    /// Notion のプロパティ名を定義します。
    /// </summary>
    private class NotionPropertyConstants
    {
        public const string TitlePropertyName = "Title";
        public const string TypePropertyName = "Type";
        public const string PublishedAtPropertyName = "PublishedAt";
        public const string RequestPublishingPropertyName = "RequestPublishing";
        public const string CrawledAtPropertyName = "_SystemCrawledAt";
        public const string TagsPropertyName = "Tags";
        public const string DescriptionPropertyName = "Description";
        public const string SlugPropertyName = "Slug";
    }


    public async Task<List<Page>> GetPagesForPublishingAsync(string databaseId)
    {
        var allPages = new List<Page>();
        var filter = new CheckboxFilter(NotionPropertyConstants.RequestPublishingPropertyName, true);
        string? nextCursor = null;

        do
        {
            var pagination = await client.Databases.QueryAsync(databaseId, new DatabasesQueryParameters
            {
                Filter = filter,
                StartCursor = nextCursor
            });

            allPages.AddRange(pagination.Results);
            nextCursor = pagination.HasMore ? pagination.NextCursor : null;
        } while (nextCursor != null);


        return allPages;
    }

    public PageData CopyPageProperties(Page page)
    {
        var pageData = new PageData { PageId = page.Id };

        foreach (var property in page.Properties)
        {
            if (property.Key == NotionPropertyConstants.PublishedAtPropertyName)
            {
                if (PropertyParser.TryParseAsDateTime(property.Value, out var publishedAt))
                {
                    pageData.PublishedDateTime = publishedAt;
                }
            }
            else if (property.Key == NotionPropertyConstants.CrawledAtPropertyName)
            {
                if (PropertyParser.TryParseAsDateTime(property.Value, out var crawledAt))
                {
                    pageData.LastCrawledDateTime = crawledAt;
                }
            }
            else if (property.Key == NotionPropertyConstants.SlugPropertyName)
            {
                if (PropertyParser.TryParseAsPlainText(property.Value, out var slug))
                {
                    pageData.Slug = slug;
                }
            }
            else if (property.Key == NotionPropertyConstants.TitlePropertyName)
            {
                if (PropertyParser.TryParseAsPlainText(property.Value, out var title))
                {
                    pageData.Title = title;
                }
            }
            else if (property.Key == NotionPropertyConstants.DescriptionPropertyName)
            {
                if (PropertyParser.TryParseAsPlainText(property.Value, out var description))
                {
                    pageData.Description = description;
                }
            }
            else if (property.Key == NotionPropertyConstants.TagsPropertyName)
            {
                if (PropertyParser.TryParseAsStringList(property.Value, out var tags))
                {
                    pageData.Tags = tags;
                }
            }
            else if (property.Key == NotionPropertyConstants.TypePropertyName)
            {
                if (PropertyParser.TryParseAsPlainText(property.Value, out var type))
                {
                    pageData.Type = type;
                }
            }
            else if (property.Key == NotionPropertyConstants.RequestPublishingPropertyName)
            {
                if (PropertyParser.TryParseAsBoolean(property.Value, out var requestPublishing))
                {
                    pageData.RequestPublishing = requestPublishing;
                }
            }
        }

        return pageData;
    }

    public async Task UpdatePagePropertiesAsync(string pageId, DateTime now)
    {
        await client.Pages.UpdateAsync(pageId, new PagesUpdateParameters
        {
            Properties = new Dictionary<string, PropertyValue>
            {
                [NotionPropertyConstants.CrawledAtPropertyName] = new DatePropertyValue
                {
                    Date = new Date
                    {
                        Start = now
                    }
                },
                [NotionPropertyConstants.RequestPublishingPropertyName] = new CheckboxPropertyValue
                {
                    Checkbox = false
                },
                ["セレクト"] = new SelectPropertyValue
                {
                    Select = new SelectOption
                    {
                        Name = "公開済み"
                    }
                }
            }
        });
    }

    public async Task<List<NotionBlock>> GetPageFullContent(string blockId)
    {
        List<NotionBlock> results = [];
        string? nextCursor = null;

        // ページの親要素を取得
        do
        {
            var pagination = await client.Blocks.RetrieveChildrenAsync(
                blockId,
                new BlocksRetrieveChildrenParameters
                {
                    StartCursor = nextCursor
                }
            );

            results.AddRange(pagination.Results.Cast<Block>().Select(NotionBlock.FromBlock));
            nextCursor = pagination.HasMore ? pagination.NextCursor : null;
        } while (nextCursor != null);

        // ページの子要素を取得
        var tasks = results
            .Where(block => block.HasChildren)
            .Select(async block => block.Children = await GetPageFullContent(block.Id))
            .ToList();

        await Task.WhenAll(tasks);

        return results;
    }

    public async Task<List<Block>> GetBlocksAsync(string blockId)
    {
        var allBlocks = new List<Block>();
        string? nextCursor = null;

        do
        {
            var pagination = await client.Blocks.RetrieveChildrenAsync(blockId, new BlocksRetrieveChildrenParameters
            {
                StartCursor = nextCursor
            });

            allBlocks.AddRange(pagination.Results.Cast<Block>());
            nextCursor = pagination.HasMore ? pagination.NextCursor : null;

        } while (nextCursor != null);

        return allBlocks;
    }
}
