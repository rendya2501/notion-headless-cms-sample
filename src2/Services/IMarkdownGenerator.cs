using hoge.Models;
namespace hoge.Services;

public interface IMarkdownGenerator
{
    Task<string> GenerateMarkdownAsync(PageData pageData, string outputDirectory);
}
