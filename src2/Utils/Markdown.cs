using Notion.Client;
using System.Text;
using System.Text.RegularExpressions;

namespace hoge.Utils;

///// <summary>
///// Notion APIの色タイプ
///// </summary>
//public enum ApiColor
//{
//    Default,
//    DefaultBackground,
//    Red,
//    RedBackground,
//    Orange,
//    OrangeBackground,
//    Yellow,
//    YellowBackground,
//    Brown,
//    BrownBackground,
//    Green,
//    GreenBackground,
//    Blue,
//    BlueBackground,
//    Purple,
//    PurpleBackground,
//    Pink,
//    PinkBackground,
//    Gray,
//    GrayBackground
//}

///// <summary>
///// リッチテキスト構造体
///// </summary>
//public class RichText
//{
//    public string PlainText { get; set; }
//    public string Href { get; set; }
//    public string Type { get; set; }
//    public TextAnnotations Annotations { get; set; }
//}

///// <summary>
///// テキストアノテーション構造体
///// </summary>
//public class TextAnnotations
//{
//    public bool Bold { get; set; }
//    public bool Italic { get; set; }
//    public bool Strikethrough { get; set; }
//    public bool Underline { get; set; }
//    public bool Code { get; set; }
//    public ApiColor Color { get; set; }
//}

/// <summary>
/// 箇条書きスタイル
/// </summary>
public enum BulletStyle
{
    Hyphen = '-',
    Asterisk = '*',
    Plus = '+'
}

/// <summary>
/// テーブルセル
/// </summary>
public class TableCell
{
    public string Content { get; set; }
}

/// <summary>
/// テーブルヘッダー
/// </summary>
public class TableHeader
{
    public string Content { get; set; }
    public Alignment Alignment { get; set; }
}
public enum Alignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// HTMLユーティリティ
/// </summary>
public static class HTMLUtils
{
    public static string ObjectToPropertiesStr(Dictionary<string, string> props)
    {
        if (props == null || props.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var prop in props)
        {
            if (!string.IsNullOrEmpty(prop.Value))
            {
                sb.Append($"{prop.Key}=\"{prop.Value}\" ");
            }
        }
        return sb.ToString().TrimEnd();
    }
}

/// <summary>
/// 一般ユーティリティ
/// </summary>
public static class Utils
{
    public static bool IsURL(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return Uri.TryCreate(text, UriKind.Absolute, out Uri? uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// カラーマップの型
/// </summary>
public class ColorMap : Dictionary<Color, string> { }

/// <summary>
/// Markdown変換ユーティリティ
/// </summary>
public static class MarkdownUtils
{
    private static readonly ColorMap COLOR_MAP = new()
    {
        { Notion.Client.Color.Default, null },
        //{ Notion.Client.Color.DefaultBackground, null },
        { Notion.Client.Color.Red, "#A83232" },
        { Notion.Client.Color.RedBackground, "#E8CCCC" },
        { Notion.Client.Color.Orange, "#C17F46" },
        { Notion.Client.Color.OrangeBackground, "#E8D5C2" },
        { Notion.Client.Color.Yellow, "#9B8D27" },
        { Notion.Client.Color.YellowBackground, "#E6E6C8" },
        { Notion.Client.Color.Brown, "#8B6C55" },
        { Notion.Client.Color.BrownBackground, "#E0D5CC" },
        { Notion.Client.Color.Green, "#4E7548" },
        { Notion.Client.Color.GreenBackground, "#D5E0D1" },
        { Notion.Client.Color.Blue, "#3A6B9F" },
        { Notion.Client.Color.BlueBackground, "#D0DEF0" },
        { Notion.Client.Color.Purple, "#6B5B95" },
        { Notion.Client.Color.PurpleBackground, "#D8D3E6" },
        { Notion.Client.Color.Pink, "#B5787D" },
        { Notion.Client.Color.PinkBackground, "#E8D5D8" },
        { Notion.Client.Color.Gray, "#777777" },
        { Notion.Client.Color.GrayBackground, "#D0D0D0" }
    };

    private static readonly Color[] BACKGROUND_COLOR_KEY =
    [
        Notion.Client.Color.RedBackground,
        Notion.Client.Color.OrangeBackground,
        Notion.Client.Color.YellowBackground,
        Notion.Client.Color.GreenBackground,
        Notion.Client.Color.BlueBackground,
        Notion.Client.Color.PurpleBackground,
        Notion.Client.Color.PinkBackground,
        Notion.Client.Color.GrayBackground,
        Notion.Client.Color.BrownBackground
    ];

    private static readonly Color[] TEXT_COLOR_KEY =
    [
        Notion.Client.Color.Red,
        Notion.Client.Color.Orange,
        Notion.Client.Color.Yellow,
        Notion.Client.Color.Green,
        Notion.Client.Color.Blue,
        Notion.Client.Color.Purple,
        Notion.Client.Color.Pink,
        Notion.Client.Color.Gray,
        Notion.Client.Color.Brown
    ];

    /// <summary>
    /// 見出しを作成
    /// </summary>
    public static string Heading(string text, int level)
    {
        if (level < 1) level = 1;
        if (level > 6) level = 6;
        return $"{new string('#', level)} {text}";
    }

    /// <summary>
    /// テキスト装飾関数（共通処理）
    /// </summary>
    public static string Decoration(string text, string decoration)
    {
        // 空文字列や空白のみの場合は処理しない
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // 正規表現を使って先頭と末尾の空白をキャプチャしながら内部テキストも取得
        var match = Regex.Match(text, @"^(\s*)(.+?)(\s*)$");

        if (!match.Success)
        {
            return text; // マッチしない場合は元のテキストを返す
        }

        var leadingSpaces = match.Groups[1].Value;
        var content = match.Groups[2].Value;
        var trailingSpaces = match.Groups[3].Value;

        // 前後の空白を保持しつつ、内部テキストを装飾記号で囲む
        return $"{leadingSpaces}{decoration}{content}{decoration}{trailingSpaces}";
    }

    /// <summary>
    /// 太字変換
    /// </summary>
    public static string Bold(string text)
    {
        return Decoration(text, "**");
    }

    /// <summary>
    /// イタリック変換
    /// </summary>
    public static string Italic(string text)
    {
        return Decoration(text, "*");
    }

    /// <summary>
    /// 取り消し線変換
    /// </summary>
    public static string Strikethrough(string text)
    {
        return Decoration(text, "~~");
    }

    /// <summary>
    /// インラインコード変換
    /// </summary>
    public static string InlineCode(string text)
    {
        return $"`{text}`";
    }

    /// <summary>
    /// 下線変換
    /// </summary>
    public static string Underline(string text)
    {
        return $"_{text}_";
    }

    /// <summary>
    /// 色付け変換
    /// </summary>
    public static string Color(string text, Color? color, ColorMap? colorMap = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        colorMap ??= COLOR_MAP;
        color ??= Notion.Client.Color.Default;

        var spanProps = new Dictionary<string, string>();

        if (BACKGROUND_COLOR_KEY.Any(w => w == color))
        {
            var bgColor = colorMap.TryGetValue((Color)color, out string? value) ? value : null;
            if (!string.IsNullOrEmpty(bgColor))
            {
                spanProps["style"] = $"background-color: {bgColor};";
            }
        }

        if (TEXT_COLOR_KEY.Any(w => w == color))
        {
            var textColor = colorMap.TryGetValue((Color)color, out string? value) ? value : null;
            if (!string.IsNullOrEmpty(textColor))
            {
                spanProps["style"] = $"color: {textColor};";
            }
        }

        if (spanProps.Count > 0)
        {
            var propsStr = HTMLUtils.ObjectToPropertiesStr(spanProps);
            return $"<span {propsStr}>{text}</span>";
        }

        return text;
    }

    /// <summary>
    /// 箇条書きリスト変換
    /// </summary>
    public static string BulletList(string text, BulletStyle style = BulletStyle.Hyphen)
    {
        //var bullet = style switch
        //{
        //    BulletStyle.Hyphen => "-",
        //    BulletStyle.Asterisk => "*",
        //    BulletStyle.Plus => "+",
        //};
        return $"{(char)style} {text}";
    }

    /// <summary>
    /// 番号付きリスト変換
    /// </summary>
    public static string NumberedList(string text, int number)
    {
        return $"{number}. {text}";
    }

    /// <summary>
    /// チェックリスト変換
    /// </summary>
    public static string CheckList(string text, bool isChecked)
    {
        return $"- [{(isChecked ? "x" : " ")}] {text}";
    }

    /// <summary>
    /// リンク変換
    /// </summary>
    public static string Link(string text, string url)
    {
        return $"[{text}]({url})";
    }

    /// <summary>
    /// 画像変換
    /// </summary>
    public static string Image(string text, string url, string? width = null)
    {
        var urlText = url;
        if (!string.IsNullOrEmpty(width))
        {
            urlText += $" ={width}x";
        }
        return $"![{text}]({urlText})";
    }

    /// <summary>
    /// コードブロック変換
    /// </summary>
    public static string CodeBlock(string code, string? language = null)
    {
        return $"```{language ?? ""}\n{code}\n```";
    }

    /// <summary>
    /// ブロック数式変換
    /// </summary>
    public static string BlockEquation(string equation)
    {
        return $"$$\n{equation}\n$$";
    }

    /// <summary>
    /// インライン数式変換
    /// </summary>
    public static string InlineEquation(string equation)
    {
        return $"${equation}$";
    }

    /// <summary>
    /// 引用変換
    /// </summary>
    public static string Blockquote(string text)
    {
        var lines = text.Split('\n');
        return string.Join("\n", lines.Select(line => $"> {line}"));
    }

    /// <summary>
    /// テーブル変換
    /// </summary>
    public static string Table(TableHeader[] headers, TableCell[][] rows)
    {
        // 各列の最大長を計算
        var columnWidths = new int[headers.Length];

        for (var i = 0; i < headers.Length; i++)
        {
            var cellsInColumn = new List<string> { headers[i].Content };
            cellsInColumn.AddRange(rows.Select(row => row[i].Content));
            var maxLength = cellsInColumn.Max(content => content.Length);
            columnWidths[i] = maxLength < 3 ? 3 : maxLength;
        }

        // ヘッダー行を生成（パディングを追加）
        var headerRow = "| " + string.Join(" | ", headers.Select((h, i) => h.Content.PadRight(columnWidths[i]))) + " |";

        // セパレータ行を生成（長さを合わせる）
        var alignmentRow = "| " + string.Join(" | ", headers.Select((h, i) =>
        {
            var width = columnWidths[i];
            return h.Alignment switch
            {
                Alignment.Left => ":" + new string('-', width - 1),
                Alignment.Center => ":" + new string('-', width - 2) + ":",
                Alignment.Right => new string('-', width - 1) + ":",
                _ => new string('-', width)
            };
        })) + " |";

        // データ行がない場合は、ヘッダーとセパレータ行のみを返す
        if (rows.Length == 0)
        {
            return $"{headerRow}\n{alignmentRow}";
        }

        // データ行を生成（パディングを追加）
        var dataRows = string.Join("\n", rows.Select(row =>
            "| " + string.Join(" | ", row.Select((cell, i) => cell.Content.PadRight(columnWidths[i]))) + " |"
        ));

        return $"{headerRow}\n{alignmentRow}\n{dataRows}";
    }

    /// <summary>
    /// 水平線変換
    /// </summary>
    public static string HorizontalRule(string style = "hyphen")
    {
        return style switch
        {
            "asterisk" => "***",
            "underscore" => "___",
            _ => "---"
        };
    }

    /// <summary>
    /// 文字列を改行で囲む
    /// </summary>
    public static string WrapWithNewLines(string text)
    {
        return $"\n{text}\n";
    }

    /// <summary>
    /// 文字列の各行にインデントを追加
    /// </summary>
    public static string Indent(string text, int spaces = 2)
    {
        var lines = text.Split('\n');
        return string.Join("\n", lines.Select(line => line == "" ? line : $"{new string(' ', spaces)}{line}"));
    }

    /// <summary>
    /// detailsタグに変換
    /// </summary>
    public static string Details(string title, string content)
    {
        // summaryでインデントを入れるとnest構造がおかしくなる時があるので、インデントを入れない
        var result = new[]
        {
            "<details>",
            "<summary>",
            title,
            "</summary>",
            "", // 改行
            content,
            "</details>",
        };
        return string.Join("\n", result);
    }

    /// <summary>
    /// videoタグに変換
    /// </summary>
    public static string Video(string url)
    {
        return $"<video controls src=\"{url}\"></video>";
    }

    /// <summary>
    /// コメント変換
    /// </summary>
    public static string Comment(string text)
    {
        return $"<!-- {text} -->";
    }

    /// <summary>
    /// リッチテキストアノテーション有効化設定
    /// </summary>
    public class EnableAnnotations
    {
        public bool Bold { get; set; } = true;
        public bool Italic { get; set; } = true;
        public bool Strikethrough { get; set; } = true;
        public bool Underline { get; set; } = true;
        public bool Code { get; set; } = true;
        public bool Equation { get; set; } = true;
        public bool Link { get; set; } = true;
        public object Color { get; set; } = false; // bool または ColorMap
    }

    /// <summary>
    /// リッチテキストをMarkdownに変換
    /// </summary>
    public static string RichTextsToMarkdown(IEnumerable<RichTextBase> richTexts, EnableAnnotations? enableAnnotations = null, ColorMap? colorMap = null)
    {
        enableAnnotations ??= new EnableAnnotations();

        string ToMarkdown(RichTextBase text, EnableAnnotations options)
        {
            var markdown = text.PlainText;

            if (text.Annotations.IsCode && options.Code)
            {
                markdown = InlineCode(markdown);
            }
            if (text.Type == RichTextType.Equation && options.Equation)
            {
                markdown = InlineEquation(markdown);
            }
            if (text.Annotations.IsBold && options.Bold)
            {
                markdown = Bold(markdown);
            }
            if (text.Annotations.IsItalic && options.Italic)
            {
                markdown = Italic(markdown);
            }
            if (text.Annotations.IsStrikeThrough && options.Strikethrough)
            {
                markdown = Strikethrough(markdown);
            }
            if (text.Annotations.IsUnderline && options.Underline)
            {
                markdown = Underline(markdown);
            }
            if (options.Color is not null && text.Annotations.Color is not Notion.Client.Color.Default)
            {
                if (options.Color is bool boolValue && boolValue)
                {
                    markdown = Color(markdown, text.Annotations.Color, colorMap);
                }
                else if (options.Color is ColorMap customColorMap)
                {
                    markdown = Color(markdown, text.Annotations.Color, customColorMap);
                }
            }
            if (!string.IsNullOrEmpty(text.Href) && Utils.IsURL(text.Href) && options.Link)
            {
                markdown = Link(markdown, text.Href);
            }

            return markdown;
        }

        return string.Join(string.Empty, richTexts.Select(text => ToMarkdown(text, enableAnnotations))).Trim();
    }
}