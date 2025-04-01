using hoge.Models;

namespace hoge.Services;

/// <summary>
/// マークダウンを生成するクラスです。
/// </summary>
/// <param name="frontmatterGenerator"></param>
/// <param name="contentGenerator"></param>
public class MarkdownGenerator(IFrontmatterGenerator frontmatterGenerator, IContentGenerator contentGenerator) : IMarkdownGenerator
{
    /// <summary>
    /// マークダウンを生成します。
    /// </summary>
    /// <param name="pageProperty"></param>
    /// <param name="outputDirectory"></param>
    /// <returns></returns>
    public async Task<string> GenerateMarkdownAsync(PageProperty pageProperty, string outputDirectory)
    {
        var frontmatter = frontmatterGenerator.GenerateFrontmatter(pageProperty);
        var content = await contentGenerator.GenerateContentAsync(pageProperty.PageId, outputDirectory);

        return $"{frontmatter}{content}";
    }
}
