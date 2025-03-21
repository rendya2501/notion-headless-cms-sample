using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Notion.Client;
using System.Security.Cryptography;
using System.Text;

namespace src2;

public interface INotionService
{
    Task<IEnumerable<Page>> GetPagesAsync(string databaseId, CheckboxFilter filter);
    Task<bool> ExportPageToMarkdownAsync(Page page, DateTime now, bool forceExport = false);
    Task UpdatePagePropertiesAsync(string pageId, Dictionary<string, PropertyValue> properties);
}
