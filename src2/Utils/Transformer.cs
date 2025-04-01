using hoge.Models;
using Notion.Client;
using static hoge.Utils.MarkdownUtils;

namespace hoge.Utils;

public static class Transformer
{
    public static Func<Context, string> CreateMarkdownBookmarkTransformer(
        BookmarkTransformerOptions? options = null)
    {
        options ??= new BookmarkTransformerOptions();

        string execute(BookmarkBlock block)
        {
            string caption = RichTextsToMarkdown(
                block.Bookmark.Caption,
                options.EnableAnnotations,
                options.ColorMap
            );
            // ブックマークのキャプションが空の場合はURLを表示する
            string text = !string.IsNullOrEmpty(caption)
                ? caption
                : block.Bookmark.Url;
            return Link(text, block.Bookmark.Url);
        }

        return TransformerFactory.CreateBookmarkTransformerFactory(execute);

        return TransformerFactory.CreateBookmarkTransformerFactory(block =>
        {
            string caption = RichTextsToMarkdown(
                block.Bookmark.Caption,
                options.EnableAnnotations,
                options.ColorMap
            );
            var text = string.IsNullOrEmpty(caption)
                ? block.Bookmark.Url
                : caption;
            return Link(text, block.Bookmark.Url);
        });

        //    return context =>
        //    {
        //        var block = context.CurrentBlock as BookmarkBlock;
        //        var caption = MarkdownUtils.RichTextsToMarkdown(block.Bookmark.Caption);
        //        return MarkdownUtils.Link(string.IsNullOrEmpty(caption) ? block.Bookmark.Url : caption, block.Bookmark.Url);

        //        //var block = context.CurrentBlock as BookmarkBlock;
        //        //var caption = block.Bookmark.Caption.FirstOrDefault()?.PlainText;
        //        //var url = block.Bookmark.Url;

        //        //if (string.IsNullOrWhiteSpace(caption))
        //        //{
        //        //    return string.Empty;
        //        //}

        //        //return $"[{caption}]({url})";
        //    };
    }

    public static Func<Context, string> CreateMarkdownBreadcrumbTransformer()
    {
        return context => string.Empty;
    }

    public static Func<Context, string> CreateMarkdownBulletedListItemTransformer(NumberedListItemTransformerOptions? options = null)
    {

        return Context => "";
    }

    public static Func<Context, string> CreateMarkdownCalloutTransformer()
    {
        return Context => "";
    }
    // 他のトランスフォーマーは同様に実装します...
    public static Func<Context, string> CreateMarkdownCodeTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownColumnListTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownDividerTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownEquationTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownHeadingTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownLinkPreviewTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownNumberedListItemTransformer(
        NumberedListItemTransformerOptions? options = null)
    {
        options ??= new NumberedListItemTransformerOptions();

        string execute((NumberedListItemBlock Block, string Children, int Index) args)
        {
            string text = RichTextsToMarkdown(
                args.Block.NumberedListItem.RichText,
                options.EnableAnnotations,
                options.ColorMap
            );

            string formattedChildren = Indent(args.Children, 3);
            string bulletText = NumberedList(text, args.Index);

            if (string.IsNullOrEmpty(args.Children))
            {
                return bulletText;
            }

            return $"{bulletText}\n{formattedChildren}";
        }

        return TransformerFactory.CreateNumberedListItemTransformerFactory(execute);

        //return TransformerFactory.CreateNumberedListItemTransformerFactory(args =>
        //{
        //    string text = RichTextsToMarkdown(
        //        args.Block.NumberedListItem.RichText,
        //        options.EnableAnnotations,
        //        options.ColorMap
        //    );

        //    string formattedChildren = Indent(args.Children, 3);
        //    string bulletText = NumberedList(text, args.Index);

        //    if (string.IsNullOrEmpty(args.Children))
        //    {
        //        return bulletText;
        //    }

        //    return $"{bulletText}\n{formattedChildren}";
        //});

        //return context =>
        //{
        //    var block = context.CurrentBlock as BulletedListItemBlock;
        //    var text = block.BulletedListItem.RichText;

        //    // 子ブロックの処理
        //    var children = string.Empty;
        //    if (block.HasChildren)
        //    {
        //        var childBlocks = notionClient.GetBlocksAsync(block.Id).Result;
        //        children = context.Execute(childBlocks);
        //        //children = context.Execute(block.BulletedListItem.Children);
        //    }

        //    var formattedChildren = $"  {children}";
        //    var bulletText = $"- {text}";

        //    if (string.IsNullOrEmpty(children))
        //    {
        //        return bulletText;
        //    }

        //    return $"{bulletText}\n{formattedChildren}";
        //};
    }

    public static Func<Context, string> CreateMarkdownParagraphTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownQuoteTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownSyncedBlockTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownTableOfContentsTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownTableTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownTodoListItemTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownToggleTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownFileTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownImageTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownPDFTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownVideoTransformer()
    {
        // コード実装
        return context => "";
    }

    public static Func<Context, string> CreateMarkdownEmbedTransformer()
    {
        // コード実装
        return context => "";
    }
}

public class BookmarkTransformerOptions
{
    public EnableAnnotations? EnableAnnotations { get; set; }
    public ColorMap? ColorMap { get; set; }
}

public class NumberedListItemTransformerOptions
{
    public EnableAnnotations EnableAnnotations { get; set; }
    public ColorMap ColorMap { get; set; }
}