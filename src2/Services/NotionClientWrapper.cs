using hoge.Configuration;
using hoge.Models;
using hoge.Utils;
using Notion.Client;

namespace hoge.Services;

public class NotionClientWrapper(INotionClient client) : INotionClientWrapper
{
    public async Task<List<Page>> GetPagesForPublishingAsync(string databaseId, string requestPublishingPropertyName)
    {
        var allPages = new List<Page>();
        var filter = new CheckboxFilter(requestPublishingPropertyName, true);
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
        await client.Pages.UpdateAsync(pageId, new PagesUpdateParameters
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

    //public async Task<List<Block>> GetBlocksAsync(string blockId)
    //{
    //    return await RetrieveAllItemsAsync(
    //        async (parameters) => await client.Blocks.RetrieveChildrenAsync(blockId, parameters),
    //        new BlocksRetrieveChildrenParameters()
    //    );
    //}

    //private async Task<List<Block>> RetrieveAllItemsAsync(
    //    Func<BlocksRetrieveChildrenParameters, Task<PaginatedList<IBlock>>> retrievalFunc,
    //    BlocksRetrieveChildrenParameters initialParameters)
    //{
    //    var allItems = new List<Block>();
    //    initialParameters.StartCursor = null;

    //    do
    //    {
    //        var pagination = await retrievalFunc(initialParameters);

    //        allItems.AddRange(pagination.Results.Cast<Block>());
    //        initialParameters.StartCursor = pagination.HasMore ? pagination.NextCursor : null;

    //    } while (initialParameters.StartCursor != null);

    //    return allItems;
    //}
}
