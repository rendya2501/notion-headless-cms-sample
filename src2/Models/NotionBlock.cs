using Notion.Client;

namespace hoge.Models;

public class NotionBlock
{
    public required string Id { get; set; }
    public BlockType Type { get; private set; }
    public bool HasChildren { get; private set; }
    public List<NotionBlock> Children { get; set; } = [];
    public required object OriginalBlock { get; set; }

    public static NotionBlock FromBlock(Block block)
    {
        return new NotionBlock
        {
            Id = block.Id,
            Type = block.Type,
            HasChildren = block.HasChildren,
            OriginalBlock = block
        };
    }

    public T GetOriginalBlock<T>() where T : Block
    {
        return (T)OriginalBlock;
    }
}