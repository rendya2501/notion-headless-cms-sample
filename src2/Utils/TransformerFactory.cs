using hoge.Services;
using Notion.Client;

namespace hoge.Utils;

public static class TransformerFactory
{
    public static Func<Context, string> CreateBookmarkTransformerFactory(
        Func<BookmarkBlock, string> execute)
    {
        return context =>
        {
            var block = context.CurrentBlock as BookmarkBlock;
            if (string.IsNullOrEmpty(block?.Bookmark.Url))
            {
                return string.Empty;
            }

            return execute(block);
        };
    }

    public static Func<Context, string> CreateNumberedListItemTransformerFactory(
        Func<(NumberedListItemBlock Block, string Children, int Index), string> execute)
    {
        return context =>
        {
            var beforeBlocks = context.Blocks.Take(context.CurrentBlockIndex).ToList();
            // 前のブロックがNumberedListItemBlockの場合は、そのブロックの数をカウント
            var listCount = beforeBlocks.Count(b => b is NumberedListItemBlock) + 1;
            // int listCount = 1;
            // for (int index = beforeBlocks.Count - 1; index >= 0; index--)
            // {
            //     if (beforeBlocks[index] is not NumberedListItemBlock)
            //     {
            //         break;
            //     }
            //     listCount++;
            // }

            if (context.CurrentBlock is not NumberedListItemBlock block)
            {
                return string.Empty;
            }

            // 子ブロックの処理
            //string children = block.HasChildren
            //    ? context.Execute(async () => await context.GetChildrenAsync(block.Id))
            //    : string.Empty;

            var children = string.Empty;
            if (block.HasChildren)
            {
                var childBlocks = context.GetChildrenAsync(block.Id).Result;
                children = context.Execute(childBlocks);

                //children = context.Execute(block.BulletedListItem.Children);
            }

            //string children = context.Execute(numberedListItemBlock.NumberedListItem.Children);
            //return execute(new NumberedListItemExecuteArgs
            //{
            //    Block = block,
            //    Children = children,
            //    Index = listCount
            //});

            return execute((Block: block, Children: children, Index: listCount));
        };


        //string transformer(Context context)
        Func<Context, string> transformer = context =>
        {
            var beforeBlocks = context.Blocks.Take(context.CurrentBlockIndex).ToList();
            int listCount = beforeBlocks.Count(block => block is NumberedListItemBlock) + 1;

            if (context.CurrentBlock is not NumberedListItemBlock block)
                return string.Empty;

            string children = block.HasChildren
                ? context.Execute(context.GetChildrenAsync(block.Id).Result)
                : string.Empty;

            return execute((Block: block, Children: children, Index: listCount));
        };

        return transformer;
    }
}


public class NumberedListItemExecuteArgs
{
    public NumberedListItemBlock Block { get; set; }
    public string Children { get; set; }
    public int Index { get; set; }
}