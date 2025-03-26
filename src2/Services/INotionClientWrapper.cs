using hoge.Configuration;
using hoge.Models;
using Notion.Client;

namespace hoge.Services;

public interface INotionClientWrapper
{
    Task<List<Page>> GetPagesForPublishingAsync(string databaseId, string requestPublishingPropertyName);
    Task<PageData> ExtractPageDataAsync(Page page, NotionPropertyConfig config);
    Task UpdatePagePropertiesAsync(string pageId, string crawledAtProperty, string requestPublishingProperty, DateTime now);
    Task<List<Block>> GetBlocksAsync(string blockId);
}
