using GameVisionTool.Common.Domain;
using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.Domain.AgentSettings;
using GameVisionTool.Logic.Domain.MainSettings;
using GameVisionTool.Logic.Helpers;
using GameVisionTool.Messages.Queries.Agents;
using GameVisionTool.Messages.Queries.Backstory.Gemini;
using GameVisionTool.Messages.Queries.Backstory.Llama;
using GameVisionTool.Messages.Queries.Ideas;
using GameVisionTool.Messages.Queries.Llama;
using GameVisionTool.Messages.Queries.MainSettings;
using Serilog;
using System.Collections.ObjectModel;
using System.Globalization;

namespace GameVisionTool.UI.Maui.Views;

public partial class Backstory
{
    #region private fields

    private readonly Dictionary<ApiLlmType, string> _apiLlmTypeDescriptions = GenericHelpers.ToDictionaryWithDescriptionAttribute<ApiLlmType>();

    private readonly IProcessQuery _queryProcessor;
    private readonly IProcessQueryAsync _asyncQueryProcessor;

    // The picker fires on reload as well as on user selection - LoadLocalLlmOptions rebuilds the
    // view models, so the same model comes back as a new instance. Tracking the path stops a tab
    // switch from re-dispatching a load for the model that is already current.
    private string? _requestedModelPath;

    #endregion

    public Backstory(IProcessQuery queryProcessor, IProcessQueryAsync asyncQueryProcessor)
    {
        _queryProcessor = queryProcessor;
        _asyncQueryProcessor = asyncQueryProcessor;

        InitializeApiLlmTabOptions();

        InitializeComponent();
        BindingContext = this;
    }

    private void InitializeApiLlmTabOptions()
    {
        ApiLlmTabOptions.Clear();

        foreach (var (type, description) in _apiLlmTypeDescriptions)
        {
            ApiLlmTabOptions.Add(new ApiLlmTabOption(type, description));
        }
    }

    #region Initialization

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            IdeaSelectionColumn.IsVisible = true;
            LlmSelectionColumn.IsVisible = true;
            AgentSelectionColumn.IsVisible = true;
            RefreshApiTabButtonColors();

            // All lists are owned by other tabs, so they are reloaded every time this page comes
            // back into view rather than once in the constructor. Both providers load up front so
            // switching tabs never shows a blank picker while a query round-trips.
            await LoadIdeaOptions();
            await LoadLocalLlmOptions();
            await LoadLlamaAgentOptions();
            await LoadApiLlmSettingOptions();
            await LoadGeminiAgentOptions();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex.Message);
            await DisplayAlertAsync("Error", $"Failed to load backstory options: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Provider tab

    // false = Local (Llama), true = one of the API tabs below. Which API tab is tracked separately
    // by SelectedApiLlmType. Mirrors the same tab mechanism built for Agents.xaml.
    public bool IsApiTabSelected
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLocalTabSelected));
                OnPropertyChanged(nameof(IsGeminiTabActive));
                OnPropertyChanged(nameof(IsUnsupportedApiTabActive));
                OnPropertyChanged(nameof(IsGenerationSupported));
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    public bool IsLocalTabSelected => !IsApiTabSelected;

    // One tab per ApiLlmType, looped rather than hardcoded - same as ApiLlmTabOptions in Agents.xaml.cs.
    public ObservableCollection<ApiLlmTabOption> ApiLlmTabOptions
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = [];

    public ApiLlmType SelectedApiLlmType
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGeminiTabActive));
                OnPropertyChanged(nameof(IsUnsupportedApiTabActive));
                OnPropertyChanged(nameof(IsGenerationSupported));
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    } = ApiLlmType.GoogleGemini;

    // Gemini is the only provider wired up for backstory generation so far. Any other ApiLlmType
    // lands on IsUnsupportedApiTabActive until a form and a generation path exist for it.
    public bool IsGeminiTabActive => IsApiTabSelected && SelectedApiLlmType == ApiLlmType.GoogleGemini;

    public bool IsUnsupportedApiTabActive => IsApiTabSelected && SelectedApiLlmType != ApiLlmType.GoogleGemini;

    // Whether the Brief/Generate section is shown at all. Local and Gemini both have a generation
    // path; anything else gets the work-in-progress note instead.
    public bool IsGenerationSupported => IsLocalTabSelected || IsGeminiTabActive;

    private void OnSelectLocalTab(object? sender, EventArgs e)
    {
        IsApiTabSelected = false;
        RefreshApiTabButtonColors();
    }

    private async void OnSelectApiTab(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: ApiLlmTabOption option })
            {
                SelectedApiLlmType = option.Type;
            }

            IsApiTabSelected = true;
            RefreshApiTabButtonColors();

            if (SelectedApiLlmType == ApiLlmType.GoogleGemini)
            {
                await LoadApiLlmSettingOptions();
                await LoadGeminiAgentOptions();
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex.Message);
            await DisplayAlertAsync("Error", $"Failed to load API options: {ex.Message}", "OK");
        }
    }

    // BindableLayout-generated buttons have no x:Name to bind a DataTrigger against each other, so the
    // highlight is pushed imperatively - same approach used in Agents.xaml.cs and, originally,
    // MainSettings.xaml.cs's Local/API LLM buttons.
    private void RefreshApiTabButtonColors()
    {
        if (Application.Current == null)
            return;

        var isDark = Application.Current.RequestedTheme == AppTheme.Dark;
        var activeColor = (Color)Application.Current.Resources[isDark ? "ButtonActiveDark" : "ButtonActiveLight"];
        var inactiveColor = (Color)Application.Current.Resources[isDark ? "ButtonInactiveDark" : "ButtonInactiveLight"];

        foreach (var button in ApiTabButtonsLayout.Children.OfType<Button>())
        {
            if (button.BindingContext is ApiLlmTabOption option)
            {
                button.BackgroundColor = IsApiTabSelected && option.Type == SelectedApiLlmType ? activeColor : inactiveColor;
            }
        }
    }

    #endregion

    #region Idea selection

    public ObservableCollection<IdeaViewModel> IdeaModels
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasIdeas));
                OnPropertyChanged(nameof(CanSelectIdea));
            }
        }
    } = [];

    public IdeaViewModel? SelectedIdea
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasIdeas => IdeaModels.Count > 0;

    public bool CanSelectIdea => HasIdeas && !IsWorking;

    private async Task LoadIdeaOptions()
    {
        var previouslySelectedId = SelectedIdea?.Id;

        var result = _queryProcessor.Process(new GetIdeas());

        if (result.IsFailure)
        {
            await DisplayAlertAsync("Error", $"Unable to load ideas: {result.Error}. Please try again.", "OK");
            return;
        }

        IdeaModels = new ObservableCollection<IdeaViewModel>(result.Value.Items);

        // Reloading builds fresh view models, so the picker's selection has to be re-pointed at the
        // new instance carrying the same id - otherwise a reload silently clears it.
        SelectedIdea = previouslySelectedId.HasValue
            ? IdeaModels.FirstOrDefault(x => x.Id == previouslySelectedId.Value)
            : null;
    }

    #endregion

    #region Local LLM selection

    public ObservableCollection<LocalLlmFilePathViewModel> LocalLlmModels
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasLocalLlms));
                OnPropertyChanged(nameof(CanSelectLocalLlm));
            }
        }
    } = [];

    public LocalLlmFilePathViewModel? SelectedLocalLlm
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedLocalLlmFilePath));
                OnPropertyChanged(nameof(CanGenerate));

                // A property setter cannot await, and loading weights is a long native call.
                // LoadSelectedModelAsync reports its own failures, so nothing escapes here.
                _ = LoadSelectedModelAsync();
            }
        }
    }

    // The picker shows the name the user gave the model; the path is what actually gets loaded, so
    // it is worth showing before a generation is kicked off against a stale one.
    public string SelectedLocalLlmFilePath => SelectedLocalLlm?.FullFilePath ?? string.Empty;

    // Text-backed so the Entry can hold an invalid in-progress edit (e.g. "0.") without the binding
    // throwing; parsed on Generate. Pre-filled from the selected model's saved defaults, editable
    // per generation.
    public string MaxTokensInput
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    } = "4096";

    public string TemperatureInput
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    } = "0.9";

    // The Gemini counterparts to Temperature. Both are pickers rather than Entries, so unlike the two
    // above they cannot hold an unparseable value and need no check in CanGenerate.
    public ObservableCollection<string> ThinkingLevelOptions { get; } = new(GeminiAgentSettingHelpers.GetCurrentThinkingLevels);

    public string ThinkingLevelInput
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = "Minimal";

    public ObservableCollection<string> ModelOptions { get; } = new(GeminiAgentSettingHelpers.GetCurrentSupportedModels);

    public string ModelsAsOf => GeminiAgentSettingHelpers.AsOf;

    public string ModelInput
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = GeminiAgentSettingHelpers.GetCurrentSupportedModels[0];

    public bool HasLocalLlms => LocalLlmModels.Count > 0;

    // Changing the model mid-load would dispose weights the provider is still loading against, so
    // the picker is held shut for the duration rather than relying on the user not to touch it.
    public bool CanSelectLocalLlm => HasLocalLlms && !IsWorking;

    public bool IsLoadingModel
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnBusyChanged();
            }
        }
    }

    private async Task LoadLocalLlmOptions()
    {
        var previouslySelectedId = SelectedLocalLlm?.Id;

        var result = _queryProcessor.Process(new GetLocalLlmFilePaths());

        if (result.IsFailure)
        {
            await DisplayAlertAsync("Error", $"Unable to load local LLMs: {result.Error}. Please try again.", "OK");
            return;
        }

        LocalLlmModels = new ObservableCollection<LocalLlmFilePathViewModel>(result.Value.Items);

        SelectedLocalLlm = previouslySelectedId.HasValue
            ? LocalLlmModels.FirstOrDefault(x => x.Id == previouslySelectedId.Value)
            : null;
    }

    // Warms the selected model so the first generation does not pay for the weight load. The view
    // holds no LlamaSharp state of its own - the path goes out as a message and the model lives
    // behind the handler.
    private async Task LoadSelectedModelAsync()
    {
        var selected = SelectedLocalLlm;
        var modelPath = selected?.FullFilePath;

        if (string.IsNullOrWhiteSpace(modelPath) || modelPath == _requestedModelPath)
        {
            return;
        }

        _requestedModelPath = modelPath;
        IsLoadingModel = true;

        try
        {
            var result = await _asyncQueryProcessor.ProcessAsync(
                new LoadLlamaModel(modelPath, selected!.ContextSize, selected.GpuLayerCount));

            if (result.IsFailure)
            {
                // Clear the cached path so selecting the same model again retries the load.
                _requestedModelPath = null;
                await DisplayAlertAsync("Error", $"Unable to load model: {result.Error}. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            _requestedModelPath = null;
            Log.Logger.Error(ex.Message);
            await DisplayAlertAsync("Error", $"Failed to load model: {ex.Message}", "OK");
        }
        finally
        {
            IsLoadingModel = false;
        }
    }

    #endregion

    #region Llama agent selection

    public ObservableCollection<AgentViewModel> LlamaAgentModels
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasLlamaAgents));
                OnPropertyChanged(nameof(CanSelectLlamaAgent));
            }
        }
    } = [];

    public AgentViewModel? SelectedLlamaAgent
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGenerate));

                // Pre-fill the per-generation fields from this agent's saved defaults. The user can
                // still edit them before clicking Generate; whatever is in the field at that point is
                // what gets sent.
                if (SelectedLlamaAgent is not null)
                {
                    MaxTokensInput = SelectedLlamaAgent.MaxTokens.ToString();
                    TemperatureInput = SelectedLlamaAgent.Temperature.ToString(CultureInfo.InvariantCulture);
                }
            }
        }
    }

    public bool HasLlamaAgents => LlamaAgentModels.Count > 0;

    public bool CanSelectLlamaAgent => HasLlamaAgents && !IsWorking;

    // Only Backstory-group agents make sense here - a Character agent's system prompt is written for
    // a different task and would produce the wrong kind of response.
    private async Task LoadLlamaAgentOptions()
    {
        var previouslySelectedId = SelectedLlamaAgent?.Id;

        var result = _queryProcessor.Process(new GetLlamaAgents());

        if (result.IsFailure)
        {
            await DisplayAlertAsync("Error", $"Unable to load agents: {result.Error}. Please try again.", "OK");
            return;
        }

        LlamaAgentModels = new ObservableCollection<AgentViewModel>(
            result.Value.Items.Where(x => x.GroupType == AgentGroupType.Backstory));

        SelectedLlamaAgent = previouslySelectedId.HasValue
            ? LlamaAgentModels.FirstOrDefault(x => x.Id == previouslySelectedId.Value)
            : null;
    }

    #endregion

    #region Gemini agent selection

    public ObservableCollection<GeminiAgentViewModel> GeminiAgentModels
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasGeminiAgents));
                OnPropertyChanged(nameof(CanSelectGeminiAgent));
            }
        }
    } = [];

    public GeminiAgentViewModel? SelectedGeminiAgent
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGenerate));

                // Same pre-fill as the Llama side, with the agent's model and thinking level joining
                // max tokens - Gemini 3.x ignores temperature, which is why a Gemini agent carries a
                // thinking level instead. Blank stored values fall back to the form's defaults rather
                // than emptying the pickers.
                if (SelectedGeminiAgent is not null)
                {
                    MaxTokensInput = SelectedGeminiAgent.MaxTokens.ToString();
                    ThinkingLevelInput = KeepSelectable(SelectedGeminiAgent.ThinkingLevel, ThinkingLevelOptions, ThinkingLevelInput);
                    ModelInput = KeepSelectable(SelectedGeminiAgent.Model, ModelOptions, ModelInput);
                }
            }
        }
    }

    /// <summary>
    /// Pre-fills a picker-backed field from an agent's saved value without the picker silently
    /// dropping it. A Picker whose SelectedItem is not in its ItemsSource shows nothing selected and
    /// pushes the binding back to null, which would quietly generate against something other than what
    /// the agent was configured for. An agent saved against a model since dropped from the list is the
    /// case that matters: it is still the model that agent means, so it is added back rather than lost.
    /// A blank saved value keeps the form's current default instead.
    /// </summary>
    private static string KeepSelectable(string savedValue, ObservableCollection<string> options, string fallback)
    {
        if (string.IsNullOrWhiteSpace(savedValue))
        {
            return fallback;
        }

        // Case-insensitive, matching how ThinkingLevelParser resolves a stored level - otherwise a
        // value stored in the API's own casing ("MINIMAL") would count as missing and be duplicated.
        var existing = options.FirstOrDefault(x => string.Equals(x, savedValue, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return existing;
        }

        options.Add(savedValue);

        return savedValue;
    }

    public bool HasGeminiAgents => GeminiAgentModels.Count > 0;

    public bool CanSelectGeminiAgent => HasGeminiAgents && !IsWorking;

    // Only Backstory-group agents make sense here - same reasoning as LoadLlamaAgentOptions.
    private async Task LoadGeminiAgentOptions()
    {
        var previouslySelectedId = SelectedGeminiAgent?.Id;

        var result = _queryProcessor.Process(new GetGeminiAgents());

        if (result.IsFailure)
        {
            await DisplayAlertAsync("Error", $"Unable to load agents: {result.Error}. Please try again.", "OK");
            return;
        }

        GeminiAgentModels = new ObservableCollection<GeminiAgentViewModel>(
            result.Value.Items.Where(x => x.GroupType == AgentGroupType.Backstory));

        SelectedGeminiAgent = previouslySelectedId.HasValue
            ? GeminiAgentModels.FirstOrDefault(x => x.Id == previouslySelectedId.Value)
            : null;
    }

    #endregion

    #region API LLM selection

    // Mirrors the Local LLM picker: which stored API credential (AddOrUpdateApiSetting upserts by
    // type, so there is normally one entry per provider) to generate through, filtered to whichever
    // API tab is active.
    public ObservableCollection<ApiLlmSettingViewModel> ApiLlmSettingModels
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasApiLlmSettings));
                OnPropertyChanged(nameof(CanSelectApiLlmSetting));
            }
        }
    } = [];

    public ApiLlmSettingViewModel? SelectedApiLlmSetting
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanGenerate));
            }
        }
    }

    public bool HasApiLlmSettings => ApiLlmSettingModels.Count > 0;

    public bool CanSelectApiLlmSetting => HasApiLlmSettings && !IsWorking;

    private async Task LoadApiLlmSettingOptions()
    {
        var previouslySelectedId = SelectedApiLlmSetting?.Id;

        var result = _queryProcessor.Process(new GetApiLlmSettings());

        if (result.IsFailure)
        {
            await DisplayAlertAsync("Error", $"Unable to load API LLM settings: {result.Error}. Please try again.", "OK");
            return;
        }

        var typeName = SelectedApiLlmType.ToString();

        ApiLlmSettingModels = new ObservableCollection<ApiLlmSettingViewModel>(
            result.Value.Items.Where(x => x.Type == typeName));

        SelectedApiLlmSetting = previouslySelectedId.HasValue
            ? ApiLlmSettingModels.FirstOrDefault(x => x.Id == previouslySelectedId.Value)
            : null;
    }

    #endregion

    #region Backstory generation

    public string BackstoryBrief
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = string.Empty;

    // Two-way bound so the result can be selected, copied and edited in place before it is used.
    public string GeneratedBackstory
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    } = string.Empty;

    // Set when a generation succeeds rather than computed from the text. Clearing the editor to
    // paste something else would otherwise make the section vanish with no way to bring it back.
    public bool HasGeneratedBackstory
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
            }
        }
    }

    // A generation holds the provider's weights for its whole run, so the model picker has to stay
    // shut until it finishes - reselecting would dispose weights out from under a live inference.
    public bool IsGenerating
    {
        get;
        private set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnBusyChanged();
            }
        }
    }

    public bool CanEditBrief => !IsWorking;

    // A blank brief is allowed - the model is asked to invent one. An LLM and an agent are not.
    // Temperature is only part of the contract on the Local side: Gemini 3.x ignores it, so the field
    // is hidden there and requiring it would leave the button permanently disabled.
    public bool CanGenerate => !IsWorking
                               && int.TryParse(MaxTokensInput, out _)
                               && (IsLocalTabSelected
                                   ? SelectedLocalLlm is not null
                                     && SelectedLlamaAgent is not null
                                     && float.TryParse(TemperatureInput, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                                   : IsGeminiTabActive
                                     && SelectedApiLlmSetting is not null
                                     && SelectedGeminiAgent is not null);

    // Loading weights and generating both hold native state that the inputs can invalidate, so
    // every control on the page is gated on the pair rather than on whichever one owns it.
    public bool IsWorking => IsLoadingModel || IsGenerating;

    private void OnBusyChanged()
    {
        OnPropertyChanged(nameof(IsWorking));
        OnPropertyChanged(nameof(CanSelectIdea));
        OnPropertyChanged(nameof(CanSelectLocalLlm));
        OnPropertyChanged(nameof(CanSelectLlamaAgent));
        OnPropertyChanged(nameof(CanSelectGeminiAgent));
        OnPropertyChanged(nameof(CanSelectApiLlmSetting));
        OnPropertyChanged(nameof(CanEditBrief));
        OnPropertyChanged(nameof(CanGenerate));
    }

    // Both providers end at the same place - a string in the editor - so the two paths only differ in
    // which query goes out. Each re-checks its own inputs rather than trusting CanGenerate, since the
    // button is only one of the ways this can be reached.
    private async void OnGenerateBackstoryClicked(object? sender, EventArgs e)
    {
        if (!int.TryParse(MaxTokensInput, out var maxTokens))
        {
            return;
        }

        IsGenerating = true;

        try
        {
            var result = IsLocalTabSelected
                ? await GenerateWithLocalLlm(maxTokens)
                : await GenerateWithGemini(maxTokens);

            // Null means the inputs for that provider were not all present - nothing was dispatched,
            // so there is nothing to report.
            if (result is null)
            {
                return;
            }

            if (result.IsFailure)
            {
                await DisplayAlertAsync("Error", $"Unable to generate backstory: {result.Error}. Please try again.", "OK");
                return;
            }

            GeneratedBackstory = result.Value.Text;
            HasGeneratedBackstory = true;

            // Shown after the text is in the editor, not instead of it: a truncated draft is still
            // worth keeping, so this is a notice rather than an error. Without it a backstory that
            // stops mid-sentence just reads as the model writing a bad ending.
            if (result.Value.WasTruncated)
            {
                await DisplayAlertAsync(
                    "Response cut off",
                    "The model used its entire token budget, so the backstory stops mid-response. Raise Max Tokens and generate again for a complete one.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex.Message);
            await DisplayAlertAsync("Error", $"Failed to generate backstory: {ex.Message}", "OK");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    // Both providers end at the same place, so they hand back the same shape: the text, plus whether
    // the model ran out of budget partway through it.
    private sealed record GenerationOutcome(string Text, bool WasTruncated);

    private async Task<Result<GenerationOutcome>?> GenerateWithLocalLlm(int maxTokens)
    {
        var selected = SelectedLocalLlm;
        var modelPath = selected?.FullFilePath;
        var agent = SelectedLlamaAgent;

        if (string.IsNullOrWhiteSpace(modelPath)
            || agent is null
            || !float.TryParse(TemperatureInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature))
        {
            return null;
        }

        var result = await _asyncQueryProcessor.ProcessAsync(
            new GetLlamaAgentResponse(
                modelPath, BackstoryBrief, selected!.ContextSize, selected.GpuLayerCount, maxTokens, temperature, agent.SystemPrompt, agent.SuppressThinking, agent.StopMarker));

        return result.IsSuccess
            ? Result.Ok(new GenerationOutcome(result.Value.Response, result.Value.WasTruncated))
            : Result.Fail<GenerationOutcome>(result.Error);
    }

    private async Task<Result<GenerationOutcome>?> GenerateWithGemini(int maxTokens)
    {
        var apiSetting = SelectedApiLlmSetting;
        var agent = SelectedGeminiAgent;

        if (apiSetting is null || agent is null)
        {
            return null;
        }

        // Model, max tokens and thinking level all come from the form - pre-filled from the agent, then
        // overridable for one generation without editing the agent. Only the system instructions are
        // taken straight off the agent, since that is the part the agent actually is.
        var result = await _asyncQueryProcessor.ProcessAsync(
            new GetGeminiAgentResponse(
                apiSetting.ApiKey, ModelInput, BackstoryBrief, maxTokens, agent.SystemInstructions, ThinkingLevelInput));

        // Always false for now. GenerateResponseHelper already detects a MAX_TOKENS finish reason but
        // only logs it - wiring that through to here is a separate change on the Gemini side.
        return result.IsSuccess
            ? Result.Ok(new GenerationOutcome(result.Value.Response, WasTruncated: false))
            : Result.Fail<GenerationOutcome>(result.Error);
    }

    #endregion
}
