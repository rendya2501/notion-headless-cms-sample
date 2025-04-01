namespace hoge.Models;

/// <summary>
/// コンテキスト
/// </summary>
public class Context
{
    public Func<List<NotionBlock>, string> Execute { get; set; }
    public List<NotionBlock> Blocks { get; set; }
    public NotionBlock CurrentBlock { get; set; }
    public int CurrentBlockIndex { get; set; }
}


//// コンテキスト
//public class Context
//{
//    public required Func<List<Block>, string> Execute { get; set; }
//    public required Func<string, Task<List<Block>>> GetChildrenAsync { get; set; }
//    public required List<Block> Blocks { get; set; }
//    public required Block CurrentBlock { get; set; }
//    public int CurrentBlockIndex { get; set; }
//}

