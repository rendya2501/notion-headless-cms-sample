using hoge.Models;
using System.Text;

namespace hoge.Services;

public interface IFrontmatterGenerator
{
    StringBuilder GenerateFrontmatter(PageData pageData);
}
