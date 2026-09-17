using GameVisionTool.Common.Domain;
using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Logic.Domain.AgentSettings;
using GameVisionTool.Logic.Domain.MainSettings;
using GameVisionTool.Logic.Helpers;
using GameVisionTool.Messages.Commands.Agents;
using GameVisionTool.Messages.Queries.Agents;
using Serilog;
using System.Collections.ObjectModel;
using System.Globalization;

namespace GameVisionTool.UI.Maui.Views;

// One entry per ApiLlmType, used to drive the API tab bar in a loop rather than a hardcoded button
// per provider - see ApiLlmTabOptions/OnSelectApiTab in Agents.
public sealed class ApiLlmTabOption(ApiLlmType type, string description)
{
    public ApiLlmType Type { get; } = type;
    public string Description { get; } = description;
}

public partial class Agents
{
    #region private fields

    private const string AddAgentHeading = "Add Agent";
    private const string EditAgentHeading = "Edit Agent";

    private readonly Dictionary<AgentGroupType, string> _agentGroupTypeDescriptions = GenericHelpers.ToDictionaryWithDescriptionAttribute<AgentGroupType>();
    private readonly Dictionary<ApiLlmType, string> _apiLlmTypeDescriptions = GenericHelpers.ToDictionaryWithDescriptionAttribute<ApiLlmType>();

    private readonly IProcessCommand _commandProcessor;
    private readonly IProcessQuery _queryProcessor;

    #endregion

    public Agents(IProcessCommand commandProcessor, IProcessQuery queryProcessor)
    {
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;

        InitializeAgentGroupTypeOptions();
        InitializeApiLlmTabOptions();

        InitializeComponent();
        BindingContext = this;
    }

    private void InitializeAgentGroupTypeOptions()
    {
        AgentGroupTypeOptions.Clear();

        foreach (var description in _agentGroupTypeDescriptions.Values)
        {
            AgentGroupTypeOptions.Add(description);
        }
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
            LlamaAgentFormSection.IsVisible = true;
            LlamaAgentCollectionGrid.IsVisible = true;
            LlamaAgentCollectionView.IsVisible = true;
            GeminiAgentFormSection.IsVisible = true;
            GeminiAgentCollectionGrid.IsVisible = true;
            GeminiAgentCollectionView.IsVisible = true;
            RefreshApiTabButtonColors();
            await LoadLlamaAgentGrid();
            await LoadGeminiAgentGrid();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex.Message);
            await DisplayAlertAsync("Error", $"Failed to load agents: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Provider tab

    // false = Local (Llama), true = one of the API tabs below. Everything the two top-level sections
    // show about which is active hangs off this one flag; which API tab within that is tracked
    // separately by SelectedApiLlmType.
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
            }
        }
    }

    public bool IsLocalTabSelected => !IsApiTabSelected;

    // One tab per ApiLlmType, looped rather than hardcoded so a new provider only needs a new enum
    // member - see ApiLlmTabOption and InitializeApiLlmTabOptions.
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

    // Which API tab is active. Defaults to GoogleGemini - the only provider with a form today - but
    // any ApiLlmType can be selected once its tab is clicked.
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
            }
        }
    } = ApiLlmType.GoogleGemini;

    // Gemini is the only provider with a form built so far. A future ApiLlmType lands on
    // IsUnsupportedApiTabActive (a work-in-progress placeholder) until a form exists for it - same
    // fallback idea AppShell.xaml.cs uses per AgentGroupType.
    public bool IsGeminiTabActive => IsApiTabSelected && SelectedApiLlmType == ApiLlmType.GoogleGemini;

    public bool IsUnsupportedApiTabActive => IsApiTabSelected && SelectedApiLlmType != ApiLlmType.GoogleGemini;

    private void OnSelectLocalTab(object? sender, EventArgs e)
    {
        IsApiTabSelected = false;
        RefreshApiTabButtonColors();
    }

    private void OnSelectApiTab(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: ApiLlmTabOption option })
        {
            SelectedApiLlmType = option.Type;
        }

        IsApiTabSelected = true;
        RefreshApiTabButtonColors();
    }

    // BindableLayout-generated buttons have no x:Name to bind a DataTrigger against each other, so the
    // highlight is pushed imperatively instead - the same approach MainSettings.xaml.cs already uses
    // for its Local/API LLM buttons.
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

    #region Shared

    public ObservableCollection<string> AgentGroupTypeOptions
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

    #endregion

    #region Llama agent form

    public ObservableCollection<AgentViewModel> LlamaAgentModels
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

    public string LlamaAgentName
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveLlamaAgent));
            }
        }
    } = string.Empty;

    public AgentGroupType SelectedLlamaAgentGroupType
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedLlamaAgentGroupTypeDescription));
            }
        }
    } = AgentGroupType.Backstory;

    public string SelectedLlamaAgentGroupTypeDescription
    {
        get => _agentGroupTypeDescriptions[SelectedLlamaAgentGroupType];
        set
        {
            SelectedLlamaAgentGroupType = _agentGroupTypeDescriptions.Single(x => x.Value == value).Key;
            OnPropertyChanged();
        }
    }

    public string LlamaAgentSystemPrompt
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveLlamaAgent));
            }
        }
    } = string.Empty;

    // Text-backed so the Entry can hold an invalid in-progress edit without the binding throwing;
    // parsed on Save.
    public string LlamaAgentMaxTokens
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveLlamaAgent));
            }
        }
    } = "4096";

    public string LlamaAgentTemperature
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveLlamaAgent));
            }
        }
    } = "0.9";

    // Only correct for a model whose chat template actually uses <think> tags, so it starts unticked -
    // most local models do not, and the tags then land where the reply should start and can leave the
    // model running on past the end of its turn. Not text-backed like the two above: a checkbox cannot
    // hold an invalid in-progress value, so there is nothing to parse on Save.
    public bool LlamaAgentSuppressThinking
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

    // The string the agent's system prompt is told to end with, and which generation stops on. Optional,
    // so it is absent from CanSaveLlamaAgent - blank just means this agent relies on the model ending
    // its own turn, which is what every agent did before the field existed.
    public string LlamaAgentStopMarker
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

    // Null means the form is adding; a value means it is editing that row. Everything the form shows
    // about which mode it is in hangs off this.
    public Guid? EditingLlamaAgentId
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEditingLlamaAgent));
                OnPropertyChanged(nameof(LlamaAgentFormHeading));
                OnPropertyChanged(nameof(SaveLlamaAgentButtonText));
            }
        }
    }

    public bool IsEditingLlamaAgent => EditingLlamaAgentId.HasValue;

    public string LlamaAgentFormHeading => IsEditingLlamaAgent ? EditAgentHeading : AddAgentHeading;

    public string SaveLlamaAgentButtonText => IsEditingLlamaAgent ? "Save Changes" : "Add Agent";

    // A name or prompt of nothing but whitespace would store a row the list cannot meaningfully show
    // or run, so neither counts as present.
    public bool CanSaveLlamaAgent =>
        !string.IsNullOrWhiteSpace(LlamaAgentName)
        && !string.IsNullOrWhiteSpace(LlamaAgentSystemPrompt)
        && int.TryParse(LlamaAgentMaxTokens, out _)
        && float.TryParse(LlamaAgentTemperature, NumberStyles.Float, CultureInfo.InvariantCulture, out _);

    private async Task LoadLlamaAgentGrid()
    {
        LlamaAgentModels.Clear();

        var query = new GetLlamaAgents();
        var result = _queryProcessor.Process(query);

        if (result.IsSuccess)
        {
            LlamaAgentModels = new ObservableCollection<AgentViewModel>(result.Value.Items);
        }
        else
        {
            await DisplayAlertAsync("Error", $"Unable to load agents: {result.Error}. Please try again.", "OK");
        }
    }

    private void ResetLlamaAgentForm()
    {
        EditingLlamaAgentId = null;
        LlamaAgentName = string.Empty;
        SelectedLlamaAgentGroupType = AgentGroupType.Backstory;
        LlamaAgentSystemPrompt = string.Empty;
        LlamaAgentMaxTokens = "4096";
        LlamaAgentTemperature = "0.9";
        LlamaAgentSuppressThinking = false;
        LlamaAgentStopMarker = string.Empty;
    }

    #endregion

    #region Llama create and update

    private async void OnSaveLlamaAgent(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(LlamaAgentName))
            {
                await DisplayAlertAsync("Error", "Please enter a name for the agent.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(LlamaAgentSystemPrompt))
            {
                await DisplayAlertAsync("Error", "Please enter a system prompt for the agent.", "OK");
                return;
            }

            if (!int.TryParse(LlamaAgentMaxTokens, out var maxTokens)
                || !float.TryParse(LlamaAgentTemperature, NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature))
            {
                await DisplayAlertAsync("Error", "Max Tokens and Temperature must be valid numbers.", "OK");
                return;
            }

            var name = LlamaAgentName.Trim();
            var systemPrompt = LlamaAgentSystemPrompt.Trim();

            // Trimmed because the marker is matched as a plain substring of the model's output: a
            // trailing space typed into the Entry would be part of what the model has to emit exactly.
            var stopMarker = LlamaAgentStopMarker.Trim();

            if (!string.IsNullOrEmpty(stopMarker) && !systemPrompt.Contains(stopMarker, StringComparison.Ordinal))
            {
                var proceed = await DisplayAlertAsync(
                    "Stop marker not in the system prompt",
                    $"The system prompt never mentions '{stopMarker}', so the model is not being told to write it and generation will not stop on it. Save anyway?",
                    "Save anyway",
                    "Go back");

                if (!proceed)
                    return;
            }

            if (EditingLlamaAgentId.HasValue)
            {
                await UpdateExistingLlamaAgent(EditingLlamaAgentId.Value, name, SelectedLlamaAgentGroupType, systemPrompt, maxTokens, temperature, LlamaAgentSuppressThinking, stopMarker);
                return;
            }

            await AddNewLlamaAgent(name, SelectedLlamaAgentGroupType, systemPrompt, maxTokens, temperature, LlamaAgentSuppressThinking, stopMarker);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Unable to save agent: {ex.Message}. Please try again.", "OK");
        }
    }

    private async Task AddNewLlamaAgent(string name, AgentGroupType groupType, string systemPrompt, int maxTokens, float temperature, bool suppressThinking, string stopMarker)
    {
        var result = _commandProcessor.Process(new AddLlamaAgent(name, groupType, systemPrompt, maxTokens, temperature, suppressThinking, stopMarker));

        if (result.IsSuccess)
        {
            await DisplayAlertAsync("Success", $"Agent '{name}' added successfully!", "OK");

            ResetLlamaAgentForm();

            await LoadLlamaAgentGrid();
        }
        else
        {
            await DisplayAlertAsync("Error", $"Unable to add agent: {result.Error}. Please try again.", "OK");
        }
    }

    private async Task UpdateExistingLlamaAgent(Guid id, string name, AgentGroupType groupType, string systemPrompt, int maxTokens, float temperature, bool suppressThinking, string stopMarker)
    {
        var result = _commandProcessor.Process(new UpdateLlamaAgent(id, name, groupType, systemPrompt, maxTokens, temperature, suppressThinking, stopMarker));

        if (result.IsSuccess)
        {
            await DisplayAlertAsync("Success", $"Agent '{name}' updated successfully!", "OK");

            ResetLlamaAgentForm();

            await LoadLlamaAgentGrid();
        }
        else
        {
            // The row can be gone by now - another window, or a delete from this list - so drop out
            // of edit mode rather than leaving the form pointed at an id that no longer resolves.
            ResetLlamaAgentForm();

            await DisplayAlertAsync("Error", $"Unable to update agent: {result.Error}. Please try again.", "OK");

            await LoadLlamaAgentGrid();
        }
    }

    private async void OnEditLlamaAgent(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: AgentViewModel model })
            {
                // Read the row back rather than trusting the bound copy, so the form starts from what
                // is actually stored.
                var result = _queryProcessor.Process(new GetLlamaAgent(model.Id));

                if (result.IsFailure)
                {
                    await DisplayAlertAsync("Error", $"Unable to load agent: {result.Error}. Please try again.", "OK");
                    await LoadLlamaAgentGrid();
                    return;
                }

                EditingLlamaAgentId = result.Value.Id;
                LlamaAgentName = result.Value.Name;
                SelectedLlamaAgentGroupType = result.Value.GroupType;
                LlamaAgentSystemPrompt = result.Value.SystemPrompt;
                LlamaAgentMaxTokens = result.Value.MaxTokens.ToString();
                LlamaAgentTemperature = result.Value.Temperature.ToString(CultureInfo.InvariantCulture);
                LlamaAgentSuppressThinking = result.Value.SuppressThinking;
                LlamaAgentStopMarker = result.Value.StopMarker;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load agent: {ex.Message}", "OK");
        }
    }

    private void OnCancelEditLlamaAgent(object? sender, EventArgs e) => ResetLlamaAgentForm();

    #endregion

    #region Llama delete

    private async void OnDeleteLlamaAgent(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: AgentViewModel model })
            {
                var confirmDelete = await DisplayAlertAsync(
                    "Confirm Delete",
                    $"Are you sure you want to remove '{model.Name}'?",
                    "Delete",
                    "Cancel");

                if (!confirmDelete)
                    return; // User cancelled

                var deleteCommand = new RemoveLlamaAgent(model.Id);

                var result = _commandProcessor.Process(deleteCommand);

                if (result.IsSuccess)
                {
                    // The form would otherwise stay pointed at a row that no longer exists.
                    if (EditingLlamaAgentId == model.Id)
                        ResetLlamaAgentForm();

                    await DisplayAlertAsync("Success", $"Agent '{model.Name}' removed successfully!", "OK");

                    await LoadLlamaAgentGrid();
                }
                else
                {
                    await DisplayAlertAsync("Error", $"Unable to remove agent: {result.Error}. Please try again.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to remove agent: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Gemini agent form

    public ObservableCollection<GeminiAgentViewModel> GeminiAgentModels
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

    public string GeminiAgentName
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveGeminiAgent));
            }
        }
    } = string.Empty;

    public AgentGroupType SelectedGeminiAgentGroupType
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedGeminiAgentGroupTypeDescription));
            }
        }
    } = AgentGroupType.Backstory;

    public string SelectedGeminiAgentGroupTypeDescription
    {
        get => _agentGroupTypeDescriptions[SelectedGeminiAgentGroupType];
        set
        {
            SelectedGeminiAgentGroupType = _agentGroupTypeDescriptions.Single(x => x.Value == value).Key;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> GeminiModelOptions { get; } = new(GeminiAgentSettingHelpers.GetCurrentSupportedModels);

    public string GeminiModelsAsOf => GeminiAgentSettingHelpers.AsOf;

    public string GeminiAgentModel
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveGeminiAgent));
            }
        }
    } = GeminiAgentSettingHelpers.GetCurrentSupportedModels[0];

    public string GeminiAgentSystemInstructions
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveGeminiAgent));
            }
        }
    } = string.Empty;

    // Text-backed so the Entry can hold an invalid in-progress edit without the binding throwing;
    // parsed on Save.
    public string GeminiAgentMaxTokens
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveGeminiAgent));
            }
        }
    } = "2048";

    // Resolved to the SDK's own ThinkingLevel by ThinkingLevelParser when a generation actually runs.
    public ObservableCollection<string> GeminiThinkingLevelOptions { get; } = new(GeminiAgentSettingHelpers.GetCurrentThinkingLevels);

    public string GeminiAgentThinkingLevel
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveGeminiAgent));
            }
        }
    } = "Minimal";

    // Null means the form is adding; a value means it is editing that row. Everything the form shows
    // about which mode it is in hangs off this.
    public Guid? EditingGeminiAgentId
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEditingGeminiAgent));
                OnPropertyChanged(nameof(GeminiAgentFormHeading));
                OnPropertyChanged(nameof(SaveGeminiAgentButtonText));
            }
        }
    }

    public bool IsEditingGeminiAgent => EditingGeminiAgentId.HasValue;

    public string GeminiAgentFormHeading => IsEditingGeminiAgent ? EditAgentHeading : AddAgentHeading;

    public string SaveGeminiAgentButtonText => IsEditingGeminiAgent ? "Save Changes" : "Add Agent";

    // A name, model or system instructions of nothing but whitespace would store a row the list
    // cannot meaningfully show or run, so none of them count as present.
    public bool CanSaveGeminiAgent =>
        !string.IsNullOrWhiteSpace(GeminiAgentName)
        && !string.IsNullOrWhiteSpace(GeminiAgentModel)
        && !string.IsNullOrWhiteSpace(GeminiAgentSystemInstructions)
        && int.TryParse(GeminiAgentMaxTokens, out _);

    private async Task LoadGeminiAgentGrid()
    {
        GeminiAgentModels.Clear();

        var query = new GetGeminiAgents();
        var result = _queryProcessor.Process(query);

        if (result.IsSuccess)
        {
            GeminiAgentModels = new ObservableCollection<GeminiAgentViewModel>(result.Value.Items);
        }
        else
        {
            await DisplayAlertAsync("Error", $"Unable to load agents: {result.Error}. Please try again.", "OK");
        }
    }

    private void ResetGeminiAgentForm()
    {
        EditingGeminiAgentId = null;
        GeminiAgentName = string.Empty;
        SelectedGeminiAgentGroupType = AgentGroupType.Backstory;
        GeminiAgentModel = GeminiAgentSettingHelpers.GetCurrentSupportedModels[0];
        GeminiAgentSystemInstructions = string.Empty;
        GeminiAgentMaxTokens = "2048";
        GeminiAgentThinkingLevel = "Minimal";
    }

    #endregion

    #region Gemini create and update

    private async void OnSaveGeminiAgent(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(GeminiAgentName))
            {
                await DisplayAlertAsync("Error", "Please enter a name for the agent.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(GeminiAgentModel))
            {
                await DisplayAlertAsync("Error", "Please enter a model for the agent.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(GeminiAgentSystemInstructions))
            {
                await DisplayAlertAsync("Error", "Please enter system instructions for the agent.", "OK");
                return;
            }

            if (!int.TryParse(GeminiAgentMaxTokens, out var maxTokens))
            {
                await DisplayAlertAsync("Error", "Max Tokens must be a valid number.", "OK");
                return;
            }

            var name = GeminiAgentName.Trim();
            var model = GeminiAgentModel.Trim();
            var systemInstructions = GeminiAgentSystemInstructions.Trim();

            if (EditingGeminiAgentId.HasValue)
            {
                await UpdateExistingGeminiAgent(EditingGeminiAgentId.Value, name, SelectedGeminiAgentGroupType, model, systemInstructions, maxTokens, GeminiAgentThinkingLevel);
                return;
            }

            await AddNewGeminiAgent(name, SelectedGeminiAgentGroupType, model, systemInstructions, maxTokens, GeminiAgentThinkingLevel);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Unable to save agent: {ex.Message}. Please try again.", "OK");
        }
    }

    private async Task AddNewGeminiAgent(string name, AgentGroupType groupType, string model, string systemInstructions, int maxTokens, string thinkingLevel)
    {
        var result = _commandProcessor.Process(new AddGeminiAgent(name, groupType, model, systemInstructions, maxTokens, thinkingLevel));

        if (result.IsSuccess)
        {
            await DisplayAlertAsync("Success", $"Agent '{name}' added successfully!", "OK");

            ResetGeminiAgentForm();

            await LoadGeminiAgentGrid();
        }
        else
        {
            await DisplayAlertAsync("Error", $"Unable to add agent: {result.Error}. Please try again.", "OK");
        }
    }

    private async Task UpdateExistingGeminiAgent(Guid id, string name, AgentGroupType groupType, string model, string systemInstructions, int maxTokens, string thinkingLevel)
    {
        var result = _commandProcessor.Process(new UpdateGeminiAgent(id, name, groupType, model, systemInstructions, maxTokens, thinkingLevel));

        if (result.IsSuccess)
        {
            await DisplayAlertAsync("Success", $"Agent '{name}' updated successfully!", "OK");

            ResetGeminiAgentForm();

            await LoadGeminiAgentGrid();
        }
        else
        {
            // The row can be gone by now - another window, or a delete from this list - so drop out
            // of edit mode rather than leaving the form pointed at an id that no longer resolves.
            ResetGeminiAgentForm();

            await DisplayAlertAsync("Error", $"Unable to update agent: {result.Error}. Please try again.", "OK");

            await LoadGeminiAgentGrid();
        }
    }

    private async void OnEditGeminiAgent(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: GeminiAgentViewModel model })
            {
                // Read the row back rather than trusting the bound copy, so the form starts from what
                // is actually stored.
                var result = _queryProcessor.Process(new GetGeminiAgent(model.Id));

                if (result.IsFailure)
                {
                    await DisplayAlertAsync("Error", $"Unable to load agent: {result.Error}. Please try again.", "OK");
                    await LoadGeminiAgentGrid();
                    return;
                }

                EditingGeminiAgentId = result.Value.Id;
                GeminiAgentName = result.Value.Name;
                SelectedGeminiAgentGroupType = result.Value.GroupType;
                GeminiAgentModel = result.Value.Model;
                GeminiAgentSystemInstructions = result.Value.SystemInstructions;
                GeminiAgentMaxTokens = result.Value.MaxTokens.ToString();
                GeminiAgentThinkingLevel = result.Value.ThinkingLevel;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load agent: {ex.Message}", "OK");
        }
    }

    private void OnCancelEditGeminiAgent(object? sender, EventArgs e) => ResetGeminiAgentForm();

    #endregion

    #region Gemini delete

    private async void OnDeleteGeminiAgent(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: GeminiAgentViewModel model })
            {
                var confirmDelete = await DisplayAlertAsync(
                    "Confirm Delete",
                    $"Are you sure you want to remove '{model.Name}'?",
                    "Delete",
                    "Cancel");

                if (!confirmDelete)
                    return; // User cancelled

                var deleteCommand = new RemoveGeminiAgent(model.Id);

                var result = _commandProcessor.Process(deleteCommand);

                if (result.IsSuccess)
                {
                    // The form would otherwise stay pointed at a row that no longer exists.
                    if (EditingGeminiAgentId == model.Id)
                        ResetGeminiAgentForm();

                    await DisplayAlertAsync("Success", $"Agent '{model.Name}' removed successfully!", "OK");

                    await LoadGeminiAgentGrid();
                }
                else
                {
                    await DisplayAlertAsync("Error", $"Unable to remove agent: {result.Error}. Please try again.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to remove agent: {ex.Message}", "OK");
        }
    }

    #endregion
}
