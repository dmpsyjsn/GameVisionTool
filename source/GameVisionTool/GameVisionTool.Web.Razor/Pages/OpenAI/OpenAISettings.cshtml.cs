using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Messages.Commands.OpenAi;
using GameVisionTool.Messages.Queries.OpenAi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GameVisionTool.Web.Razor.Pages.OpenAI
{
    public class OpenAISettingsModel(IProcessCommandAsync commandProcessor, IProcessQueryAsync queryProcessor) : PageModel
    {
        [BindProperty]
        public string ApiKey { get; set; }
        
        public async Task OnGet()
        {
            var result = await queryProcessor.ProcessAsync(new GetOpenAiSettings());

            if (result.IsSuccess)
            {
                ApiKey = result.Value.ApiKey;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!string.IsNullOrEmpty(ApiKey))
            {
                var command = new UpsertOpenAiSettings(ApiKey);
                await commandProcessor.ProcessAsync(command);
            }

            return Page();
        }
    }
}
