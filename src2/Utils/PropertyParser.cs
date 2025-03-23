using Notion.Client;

namespace hoge.Utils;

public static class PropertyParser
{
    public static bool TryParseAsDateTime(PropertyValue value, out DateTime dateTime)
    {
        dateTime = default;

        switch (value)
        {
            case DatePropertyValue dateProperty:
                if (dateProperty.Date?.Start == null)
                {
                    return false;
                }
                dateTime = dateProperty.Date.Start.Value;
                return true;

            case CreatedTimePropertyValue createdTimeProperty:
                return DateTime.TryParse(createdTimeProperty.CreatedTime, out dateTime);

            case LastEditedTimePropertyValue lastEditedTimeProperty:
                return DateTime.TryParse(lastEditedTimeProperty.LastEditedTime, out dateTime);

            default:
                if (TryParseAsPlainText(value, out var text) &&
                    DateTime.TryParse(text, out dateTime))
                {
                    return true;
                }
                return false;
        }
    }

    public static bool TryParseAsPlainText(PropertyValue value, out string text)
    {
        text = string.Empty;

        switch (value)
        {
            case RichTextPropertyValue richTextProperty:
                text = string.Join("", richTextProperty.RichText.Select(rt => rt.PlainText));
                return true;

            case TitlePropertyValue titleProperty:
                text = string.Join("", titleProperty.Title.Select(t => t.PlainText));
                return true;

            case SelectPropertyValue selectProperty:
                text = selectProperty.Select?.Name ?? string.Empty;
                return true;

            default:
                return false;
        }
    }

    public static bool TryParseAsStringList(PropertyValue value, out List<string> items)
    {
        items = new List<string>();

        if (value is MultiSelectPropertyValue multiSelectProperty)
        {
            items.AddRange(multiSelectProperty.MultiSelect.Select(s => s.Name));
            return true;
        }

        return false;
    }

    public static bool TryParseAsBoolean(PropertyValue value, out bool result)
    {
        result = false;

        if (value is CheckboxPropertyValue checkboxProperty)
        {
            result = checkboxProperty.Checkbox;
            return true;
        }

        return false;
    }
}
