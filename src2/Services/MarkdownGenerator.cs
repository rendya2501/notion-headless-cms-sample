using hoge.Models;

namespace hoge.Services;

/// <summary>
/// マークダウンを生成するクラスです。  
/// </summary>
/// <param name="frontmatterGenerator"></param>
/// <param name="contentGenerator"></param>
public class MarkdownGenerator(
    INotionClientWrapper notionClient,
    IFrontmatterGenerator frontmatterGenerator,
    IContentGenerator contentGenerator) : IMarkdownGenerator
{
    /// <summary>
    /// マークダウンを生成します。
    /// </summary>
    /// <param name="pageProperty"></param>
    /// <returns></returns>
    public async Task<string> GenerateMarkdownAsync(PageProperty pageProperty)
    {
        // ページの全内容を取得(非同期で実行)
        var pageFullContent = notionClient.GetPageFullContent(pageProperty.PageId);

        // フロントマターを作成
        var frontmatter = frontmatterGenerator.GenerateFrontmatter(pageProperty);

        // ページの全内容をマークダウンに変換
        var content = contentGenerator.GenerateContentAsync(await pageFullContent);

        // マークダウンを出力
        return $"{frontmatter}{content}";
    }
}
