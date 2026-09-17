using GameVisionTool.Common.Domain;
using GameVisionTool.UI.Maui.Views;

namespace GameVisionTool.UI.Maui
{
    public partial class AppShell : Shell
    {
        // Home and Ideas are always first; every AgentGroupType tab is inserted after them, ahead
        // of Settings and Agents.
        private const int GroupTabInsertIndex = 2;

        public AppShell()
        {
            InitializeComponent();

            var viewsNamespace = typeof(WorkInProgress).Namespace;
            var viewsAssembly = typeof(WorkInProgress).Assembly;

            var insertIndex = GroupTabInsertIndex;

            foreach (var groupType in Enum.GetValues<AgentGroupType>())
            {
                // A dedicated page (e.g. Backstory) is used if one exists for this group;
                // otherwise the tab falls back to the placeholder until one is built.
                var pageType = viewsAssembly.GetType($"{viewsNamespace}.{groupType}") ?? typeof(WorkInProgress);

                MainTabBar.Items.Insert(insertIndex++, new ShellContent
                {
                    Title = groupType.ToString(),
                    Route = groupType.ToString(),
                    ContentTemplate = new DataTemplate(pageType)
                });
            }
        }
    }
}
