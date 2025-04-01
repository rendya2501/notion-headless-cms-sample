using hoge.Utils;

namespace hoge.test;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var bulletText = MarkdownUtils.BulletList("test",BulletStyle.Plus); 
        Assert.Equal("+ test", bulletText);
    }
}