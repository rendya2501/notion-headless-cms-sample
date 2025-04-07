using Notion.Client;
using System.Text;

namespace hoge.Utils;

public static partial class MarkdownUtils
{
    /// <summary>
    /// リッチテキストをMarkdownに変換
    /// </summary>
    public static string RichTextsToMarkdown(
        IEnumerable<RichTextBase> richTexts,
        EnableAnnotations? enableAnnotations = null,
        ColorMap? colorMap = null)
    {
        enableAnnotations ??= new EnableAnnotations();
        var result = new StringBuilder();

        foreach (var text in richTexts)
        {
            var markdown = text.PlainText;

            // コードブロックの場合
            if (text.Annotations.IsCode && enableAnnotations.Code)
            {
                markdown = InlineCode(markdown);
            }

            // 数式の場合
            if (text.Type == RichTextType.Equation && enableAnnotations.Equation)
            {
                markdown = InlineEquation(markdown);
            }

            // 太字の場合
            if (text.Annotations.IsBold && enableAnnotations.Bold)
            {
                markdown = Bold(markdown);
            }

            // 斜体の場合
            if (text.Annotations.IsItalic && enableAnnotations.Italic)
            {
                markdown = Italic(markdown);
            }

            // 打ち消し線の場合
            if (text.Annotations.IsStrikeThrough && enableAnnotations.Strikethrough)
            {
                markdown = Strikethrough(markdown);
            }

            // 下線の場合
            if (text.Annotations.IsUnderline && enableAnnotations.Underline)
            {
                markdown = Underline(markdown);
            }

            // 色の場合
            if (text.Annotations.Color is not Notion.Client.Color.Default && enableAnnotations.Color)
            {
                markdown = Color(markdown, text.Annotations.Color, COLOR_MAP);
            }

            // リンクの場合
            if (!string.IsNullOrEmpty(text.Href) && Utils.IsURL(text.Href) && enableAnnotations.Link)
            {
                markdown = Link(markdown, text.Href);
            }

            result.Append(markdown);
        }

        return result.ToString();
    }

    /// <summary>
    /// 改行を追加
    /// </summary>
    /// <param name="richTexts"></param>
    /// <returns></returns>
    public static string AppendNewLine(string text)
    {
        var lines = text.Split('\n');
        var indentedLines = lines.Select(line =>
            string.IsNullOrWhiteSpace(line)
                ? line
                : WithLineBreak(line));
        return string.Join("\n", indentedLines);

        //var result = new StringBuilder();

        //// テキストが空の場合は改行を追加しない
        //if (texts.Length == 0)
        //{
        //    return texts;
        //}

        //var lines = texts.Split([Environment.NewLine, "\n"], StringSplitOptions.None);
        //foreach (var (line, index) in lines.Select((line, index) => (line, index)))
        //{
        //    // その行が空白の場合は処理終了
        //    if (string.IsNullOrWhiteSpace(line))
        //    {
        //        result.Append(line);
        //        result.Append('\n');
        //        continue;
        //    }

        //    // 文字列の末尾にマークダウンの改行であるスペース2つを追記
        //    result.Append(WithLineBreak(line));

        //    // 最後の要素に到達したらループを終了
        //    if (index == lines.Length - 1)
        //    {
        //        break;
        //    }

        //    result.AppendLine();
        //}

        //return result.ToString();
    }
}