namespace hoge.Configuration;

public class NotionPropertyConfig
{
    public string TitlePropertyName { get; set; } = "Title";
    public string TypePropertyName { get; set; } = "Type";
    public string PublishedAtPropertyName { get; set; } = "PublishedAt";
    public string RequestPublishingPropertyName { get; set; } = "RequestPublishing";
    public string CrawledAtPropertyName { get; set; } = "_SystemCrawledAt";
    public string TagsPropertyName { get; set; } = "Tags";
    public string DescriptionPropertyName { get; set; } = "Description";
    public string SlugPropertyName { get; set; } = "Slug";
}
