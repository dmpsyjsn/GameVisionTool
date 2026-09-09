using System.ComponentModel;

namespace GameVisionTool.Logic.Helpers;

public static class GenericHelpers
{
    public static string GetDescriptionAttribute<T>(this T? source)
    {
        if (source == null) return string.Empty;
        var name = source.ToString();

        if (string.IsNullOrEmpty(name)) return string.Empty;

        var fi = source.GetType().GetField(name);

        if (fi == null) return string.Empty;

        var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes.Length > 0) return attributes[0].Description;

        return name;
    }

    public static Dictionary<T, string> ToDictionaryWithDescriptionAttribute<T>() where T : Enum
    {
        var enumType = typeof(T);
        var values = Enum.GetValues(enumType).Cast<T>();
        return values.ToDictionary(value => value, value => value.GetDescriptionAttribute());
    }
}