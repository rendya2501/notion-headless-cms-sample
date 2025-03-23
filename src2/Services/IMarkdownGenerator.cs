using hoge.Models;
using Notion.Client;

namespace hoge.Services;

public interface IMarkdownGenerator
{
    Task<string> GenerateMarkdownAsync(Page page, PageData pageData, string outputDirectory);
}
