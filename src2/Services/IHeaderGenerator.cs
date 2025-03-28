using hoge.Models;
using System.Text;

namespace hoge.Services;

public interface IHeaderGenerator
{
    Task<StringBuilder> GenerateHeaderAsync(PageData pageData, string outputDirectory);
}
