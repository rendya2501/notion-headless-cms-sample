using Microsoft.Extensions.DependencyInjection;
using Notion.Client;
using System.Security.Cryptography;
using System.Text;

// DIコンテナの設定
var serviceProvider = new ServiceCollection()
    .AddSingleton<NotionClient>(sp => NotionClientFactory.Create(new ClientOptions { AuthToken = args[0] }))
    .AddSingleton<INotionService, NotionService>()
    .BuildServiceProvider();

// Notionサービスの取得
var notionService = serviceProvider.GetRequiredService<INotionService>();

// 引数からNotionのデータベースIDと出力ディレクトリパスのテンプレートを取得
var notionDatabaseId = args[1];
var outputDirectoryPathTemplate = args[2];

// Notionデータベースのフィルタを設定
var filter = new CheckboxFilter("RequestPublishing", true);
// 更新フラグが立っているページを取得
var pages = await notionService.GetPagesAsync(notionDatabaseId, filter);

var now = DateTime.Now;
// ページのエクスポート数
var exportedCount = 0;

// ページを取得してMarkdown形式でエクスポート
foreach (var page in pages)
{
    // ページをMarkdown形式でエクスポート
    if (!await notionService.ExportPageToMarkdownAsync(page, now))
    {
        // エクスポートに失敗した場合は次のページに進む
        continue;
    }

    // ページのプロパティを更新
    var properties = new Dictionary<string, PropertyValue>
    {
        ["_SystemCrawledAt"] = new DatePropertyValue { Date = new Date { Start = now } },
        ["RequestPublishing"] = new CheckboxPropertyValue { Checkbox = false }
    };
    await notionService.UpdatePagePropertiesAsync(page.Id, properties);

    exportedCount++;
}

// GITHUB_ENV環境変数のパスを取得
var githubEnvPath = Environment.GetEnvironmentVariable("GITHUB_ENV") ?? string.Empty;
if (string.IsNullOrEmpty(githubEnvPath))
{
    Console.WriteLine("Environment.GetEnvironmentVariable(GITHUB_ENV) is null !!");
}

// GITHUB_ENVにエクスポートされたファイルの数を書き込む
var writeLineExportedCount = $"EXPORTED_COUNT={exportedCount}";
using (var writer = new StreamWriter(githubEnvPath, true))
{
    writer.WriteLine(writeLineExportedCount);
}
Console.WriteLine(writeLineExportedCount);