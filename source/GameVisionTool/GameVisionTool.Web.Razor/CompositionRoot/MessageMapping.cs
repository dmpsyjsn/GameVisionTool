namespace GameVisionTool.Web.Razor.CompositionRoot;

public static class MessageMapping
{
    public static IMessageMappingBuilder FlatApi(object dispatcher, string patternFormat = "/api/{0}") =>
        new FlatApiMessageMappingBuilder(dispatcher, patternFormat);
}