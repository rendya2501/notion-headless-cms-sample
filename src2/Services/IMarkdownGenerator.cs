using hoge.Models;
namespace hoge.Services;

public interface IMarkdownGenerator
{
    Task<string> GenerateMarkdownAsync(PageProperty pageProperty, string outputDirectory);
}
