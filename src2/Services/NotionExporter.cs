using hoge.Configuration;
using hoge.Models;
using Notion.Client;
using System.Text;

namespace hoge.Services;

/// <summary>
/// Notionのページをエクスポートするサービス
/// </summary>
/// <param name="config"></param>
/// <param name="notionClient"></param>
/// <param name="markdownGenerator"></param>
public class NotionExporter(
    AppConfiguration config,
    INotionClientWrapper notionClient,
    IMarkdownGenerator markdownGenerator) : INotionExporter
{
    public async Task ExportPagesAsync()
    {
        // リクエストされた公開ページ一覧を取得
        var pages = await notionClient.GetPagesForPublishingAsync(config.NotionDatabaseId);

        //  エクスポート日時
        var now = DateTime.Now;
        // エクスポート成功数
        var exportedCount = 0;

        // ページごとにエクスポート
        foreach (var page in pages)
        {
            // ページのエクスポートに失敗した場合は次のページに進む
            if (!await ExportPageAsync(page, now))
            {
                continue;
            }

            // ページのプロパティを更新
            await notionClient.UpdatePagePropertiesAsync(page.Id, now);

            //  エクスポート成功数をカウント
            exportedCount++;
        }

        // GitHub Actions の環境変数を更新
        UpdateGitHubEnvironment(exportedCount);
    }

    /// <summary>
    /// ページをエクスポートします。
    /// </summary>
    /// <param name="page"></param>
    /// <param name="now"></param>
    /// <returns></returns>
    private async Task<bool> ExportPageAsync(Page page, DateTime now)
    {
        try
        {
            // ページのプロパティを取得
            var pageData = notionClient.CopyPageProperties(page);

            // ページのエクスポートが不要な場合はスキップ
            if (!ShouldExportPage(pageData, now))
            {
                return false;
            }

            // 出力ディレクトリを構築
            var outputDirectory = BuildOutputDirectory(pageData);
            // 出力ディレクトリが存在しない場合は作成
            Directory.CreateDirectory(outputDirectory);

            // ページの Markdown を生成
            var markdown = await markdownGenerator.GenerateMarkdownAsync(pageData);

            // Markdown を出力
            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "index.md"),
                markdown,
                new UTF8Encoding(false));

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting page {page.Id}: {ex.Message}");
            // 詳細なログ記録や監視システムへの通知をここに追加
            return false;
        }
    }

    /// <summary>
    /// ページをエクスポートするかどうかを判定します。
    /// </summary>
    /// <param name="pageProperty"></param>
    /// <param name="now"></param>
    /// <returns></returns>
    private static bool ShouldExportPage(PageProperty pageProperty, DateTime now)
    {
        // リクエスト公開が無効な場合はスキップ
        if (!pageProperty.RequestPublishing)
        {
            Console.WriteLine($"{pageProperty.PageId}(title = {pageProperty.Title}): No request publishing.");
            return false;
        }

        // 公開日時が未設定の場合はスキップ
        if (!pageProperty.PublishedDateTime.HasValue)
        {
            Console.WriteLine($"{pageProperty.PageId}(title = {pageProperty.Title}): Missing publish date.");
            return false;
        }

        // 公開日時が未来の場合はスキップ
        if (now < pageProperty.PublishedDateTime.Value)
        {
            Console.WriteLine($"{pageProperty.PageId}(title = {pageProperty.Title}): Publication date not reached.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 出力ディレクトリを構築します。
    /// </summary>
    /// <param name="pageProperty"></param>
    /// <returns></returns>
    private string BuildOutputDirectory(PageProperty pageProperty)
    {
        // 出力ディレクトリパスのテンプレートをパース
        var template = Scriban.Template.Parse(config.OutputDirectoryPathTemplate);
        // スラッグが設定されている場合はそれを使用、未設定の場合はタイトルを使用
        var slug = !string.IsNullOrEmpty(pageProperty.Slug) ? pageProperty.Slug : pageProperty.Title;

        // 出力ディレクトリパスをレンダリング
        return template.Render(new
        {
            publish = pageProperty.PublishedDateTime.Value,
            title = pageProperty.Title,
            slug = slug
        });
    }

    /// <summary>
    /// GitHub Actions の環境変数を更新します。
    /// </summary>
    /// <param name="exportedCount"></param>
    private static void UpdateGitHubEnvironment(int exportedCount)
    {
        // GitHub Actions の環境変数ファイルパスを取得
        var githubEnvPath = Environment.GetEnvironmentVariable("GITHUB_ENV");

        // 環境変数が設定されていない場合は警告を出力
        if (string.IsNullOrEmpty(githubEnvPath))
        {
            Console.WriteLine("Warning: GITHUB_ENV not set, skipping environment update.");
            return;
        }

        // エクスポート成功数を環境変数に追記
        var exportCountLine = $"EXPORTED_COUNT={exportedCount}";
        File.AppendAllText(githubEnvPath, exportCountLine + Environment.NewLine);
        Console.WriteLine(exportCountLine);
    }
}
