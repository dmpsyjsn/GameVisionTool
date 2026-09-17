using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;

namespace GameVisionTool.UI.Maui.Platforms.Windows;

/// <summary>
/// WinUI's default <see cref="TextBox"/> template hides both scroll bars, so a multi-line
/// <see cref="Editor"/> scrolls with the wheel and the caret but gives no sign that there is more
/// text below the fold - which is exactly the case for a generated backstory in a fixed-height box.
/// Editor exposes no scroll bar property of its own, so the attached property has to be set on the
/// platform view. Auto rather than Visible: a short result gets no bar it does not need.
/// </summary>
internal static class EditorScrollBars
{
    public static void Enable()
    {
        EditorHandler.Mapper.AppendToMapping(nameof(EditorScrollBars), (handler, _) =>
        {
            if (handler.PlatformView is not TextBox textBox) return;

            ScrollViewer.SetVerticalScrollBarVisibility(
                textBox,
                Microsoft.UI.Xaml.Controls.ScrollBarVisibility.Auto);
        });
    }
}
