using LiteDB;

namespace GameVisionTool.IntegrationTests;

public class CustomLiteDbTestDriver : IDisposable
{
    internal readonly LiteDatabase Db = new(new MemoryStream());

    public void Dispose() => Db.Dispose();
}