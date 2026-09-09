namespace GameVisionTool.IntegrationTests.Helpers;

internal static class StringHelpers
{
    internal static string GenerateRandomString()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();

        // Efficiently allocates the 8-character span on the stack
        return string.Create(8, random, (span, rand) =>
        {
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = chars[rand.Next(chars.Length)];
            }
        });
    }
}