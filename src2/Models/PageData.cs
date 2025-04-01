namespace hoge.Models;

/// <summary>
/// ページデータ
/// </summary>
public class PageData
{
    public string PageId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public DateTime? PublishedDateTime { get; set; }
    public DateTime? LastCrawledDateTime { get; set; }
    public bool RequestPublishing { get; set; }
}
