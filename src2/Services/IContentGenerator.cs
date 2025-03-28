using System.Text;

namespace hoge.Services;

public interface IContentGenerator
{
    Task<StringBuilder> GenerateContentAsync(string pageId, string outputDirectory);
}
