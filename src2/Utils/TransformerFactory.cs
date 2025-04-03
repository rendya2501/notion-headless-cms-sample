using hoge.Models;
using Notion.Client;

namespace hoge.Utils;

public static class TransformerFactory
{
    public static Func<Context, string> CreateBookmarkTransformerFactory(
        Func<BookmarkBlock, string> execute)
    {
        string createTransformer(Context context)
        {
            var originalBlock = context.CurrentBlock.GetOriginalBlock<Block>();
            if (originalBlock is not BookmarkBlock bookmarkBlock || string.IsNullOrEmpty(bookmarkBlock.Bookmark.Url))
            {
                return string.Empty;
            }
            return execute(bookmarkBlock);
        }

        return createTransformer;
    }

    public static Func<Context, string> CreateNumberedListItemTransformerFactory(
        Func<(NumberedListItemBlock Block, string Children, int Index), string> execute)
    {
        string createTransfomer(Context context)
        {
            var beforeBlocks = context.Blocks.Take(context.CurrentBlockIndex).ToList();

            // NumberedListItemBlockではないブロックが出てくるまでカウント
            // そのカウント数がリストのインデックスとなる
            var listCount = 1;
            for (var index = beforeBlocks.Count - 1; index >= 0; index--)
            {
                if (beforeBlocks[index].OriginalBlock is not NumberedListItemBlock)
                {
                    break;
                }
                listCount++;
            }

            return execute((
                Block: context.CurrentBlock.GetOriginalBlock<NumberedListItemBlock>(),
                Children: context.ExecuteTransformBlocks(context.CurrentBlock.Children),
                Index: listCount));
        }

        return createTransfomer;

        //return context =>
        //{
        //    var beforeBlocks = context.Blocks.Take(context.CurrentBlockIndex).ToList();
        //    //var listCount = beforeBlocks.Count(b => b.GetOriginalBlock<Block>() is NumberedListItemBlock) + 1;

        //    var listCount = 1;
        //    for (var index = beforeBlocks.Count - 1; index >= 0; index--)
        //    {
        //        if (beforeBlocks[index].OriginalBlock is not NumberedListItemBlock)
        //        {
        //            break;
        //        }
        //        listCount++;
        //    }

        //    var originalBlock = context.CurrentBlock.GetOriginalBlock<Block>();
        //    if (originalBlock is not NumberedListItemBlock numberedListItem)
        //    {
        //        return string.Empty;
        //    }

        //    var children = string.Empty;
        //    if (context.CurrentBlock.HasChildren)
        //    {
        //        children = context.Execute(context.CurrentBlock.Children);
        //    }

        //    var args = (Block: numberedListItem, Children: children, Index: listCount);
        //    return execute(args);
        //};
    }
}
