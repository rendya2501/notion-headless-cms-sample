namespace hoge.Models;

/// <summary>
/// コンテキスト
/// </summary>
public class Context
{
    /// <summary>
    /// ブロックを変換する
    /// </summary>
    /// <value></value>
    public Func<List<NotionBlock>, string> TransformBlocks { get; set; }

    /// <summary>
    /// ブロックのリスト
    /// </summary>
    /// <value></value>
    public List<NotionBlock> Blocks { get; set; }

    /// <summary>
    /// 現在のブロック
    /// </summary>
    /// <value></value>
    public NotionBlock CurrentBlock { get; set; }

    /// <summary>
    /// 現在のブロックのインデックス
    /// </summary>
    /// <value></value>
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

