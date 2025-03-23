using hoge.Configuration;
using hoge.Models;
using Notion.Client;
using System.Text;

namespace hoge.Services;

public class NotionExporter(
    AppConfiguration config,
    INotionClientWrapper notionClient,
    IMarkdownGenerator markdownGenerator) : INotionExporter
{
    public async Task ExportPagesAsync()
    {
        var pages = await notionClient.GetPagesForPublishingAsync(
            config.NotionDatabaseId,
            config.NotionProperties.RequestPublishingPropertyName);

        var now = DateTime.Now;
        var exportedCount = 0;

        foreach (var page in pages)
        {
            if (!await ExportPageAsync(page, now))
            {
                continue;
            }

            await notionClient.UpdatePagePropertiesAsync(
                page.Id,
                config.NotionProperties.CrawledAtPropertyName,
                config.NotionProperties.RequestPublishingPropertyName,
                now);

            exportedCount++;
        }

        UpdateGitHubEnvironment(exportedCount);
    }

    private async Task<bool> ExportPageAsync(Page page, DateTime now)
    {
        try
        {
            var pageData = await notionClient.ExtractPageDataAsync(page, config.NotionProperties);

            if (!ShouldExportPage(pageData, now))
            {
                return false;
            }

            var outputDirectory = BuildOutputDirectory(pageData);
            Directory.CreateDirectory(outputDirectory);

            var markdown = await markdownGenerator.GenerateMarkdownAsync(
                page,
                pageData,
                outputDirectory);

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
        var template = Scriban.Template.Parse(config.OutputDirectoryPathTemplate);
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
