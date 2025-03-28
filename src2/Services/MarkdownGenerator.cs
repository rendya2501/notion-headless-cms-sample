using hoge.Models;

namespace hoge.Services;

/// <summary>
/// マークダウンを生成するクラスです。
/// </summary>
/// <param name="config"></param>
/// <param name="notionClient"></param>
public class MarkdownGenerator(IHeaderGenerator headerGenerator, IContentGenerator contentGenerator) : IMarkdownGenerator
{
    /// <summary>
    /// マークダウンを生成します。
    /// </summary>
    /// <param name="pageData"></param>
    /// <param name="outputDirectory"></param>
    /// <returns></returns>
    public async Task<string> GenerateMarkdownAsync(PageData pageData, string outputDirectory)
    {
        var header = await headerGenerator.GenerateHeaderAsync(pageData, outputDirectory);
        var content = await contentGenerator.GenerateContentAsync(pageData.PageId, outputDirectory);

        return $"{header}{content}";
    }
}
