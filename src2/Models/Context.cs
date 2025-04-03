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
    public required Func<List<NotionBlock>, string> ExecuteTransformBlocks { get; set; }

    /// <summary>
    /// ブロックのリスト
    /// </summary>
    /// <value></value>
    public required List<NotionBlock> Blocks { get; set; }

    /// <summary>
    /// 現在のブロック
    /// </summary>
    /// <value></value>
    public required NotionBlock CurrentBlock { get; set; }

    /// <summary>
    /// 現在のブロックのインデックス
    /// </summary>
    /// <value></value>
    public int CurrentBlockIndex { get; set; }
}
