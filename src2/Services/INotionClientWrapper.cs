using hoge.Models;
using Notion.Client;

namespace hoge.Services;

public interface INotionClientWrapper
{
    Task<List<Page>> GetPagesForPublishingAsync(string databaseId);
    /// <summary>
    /// ページのプロパティをコピーします。
    /// </summary>
    /// <param name="page"></param>
    /// <returns></returns>
    PageData CopyPageProperties(Page page);
    Task UpdatePagePropertiesAsync(string pageId, DateTime now);
    Task<List<Block>> GetBlocksAsync(string blockId);
    Task<List<NotionBlock>> GetPageFullContent(string blockId);
}
