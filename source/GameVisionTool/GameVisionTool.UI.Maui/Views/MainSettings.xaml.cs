using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Logic.Domain.MainSettings;
using GameVisionTool.Logic.Helpers;
using GameVisionTool.Messages.Commands.MainSettings;
using GameVisionTool.Messages.Queries.MainSettings;
using Serilog;
using System.Collections.ObjectModel;

namespace GameVisionTool.UI.Maui.Views;

public partial class MainSettings
{
    #region private fields

    private readonly Dictionary<DevicePlatform, IEnumerable<string>> _llmFileTypes =
        new()
        {
            {DevicePlatform.WinUI, [ ".gguf" ]},
            {DevicePlatform.macOS, [ "gguf" ]}
        };

    private readonly Dictionary<ApiLlmType, string> _apiLlmTypeDescriptions = GenericHelpers.ToDictionaryWithDescriptionAttribute<ApiLlmType>();

    private const string DefaultApiSelectionText = "-- Select API LLM Type --";

    private readonly IProcessCommand _commandProcessor;
    private readonly IProcessQuery _queryProcessor;

    #endregion


    public MainSettings(IProcessCommand commandProcessor, IProcessQuery queryProcessor)
    {
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;

        // Initialize API LLM Type options
        InitializeApiLlmTypeOptions();

        InitializeComponent();
        BindingContext = this;
    }

    #region Initialization

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            LocalLlmColumn.IsVisible = true;
            ApiLlmColumn.IsVisible = true;
            LlmCollectionGrid.IsVisible = true;
            LocalLlmCollectionView.IsVisible = true;
            await LoadLocalLlmGrid();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex.Message);
            await DisplayAlertAsync("Error", $"Failed to load settings: {ex.Message}", "OK");
        }
    }

    private void InitializeApiLlmTypeOptions()
    {
        ApiLlmTypeOptions.Clear();

        // Add blank/null entry first
        ApiLlmTypeOptions.Add(DefaultApiSelectionText);

        foreach (var description in _apiLlmTypeDescriptions.Values)
        {
            ApiLlmTypeOptions.Add(description);
        }
    }

    #endregion
    
    #region Local LLM Settings

    public ObservableCollection<LocalLlmFilePathViewModel> LocalLlmModels
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

    public string SelectedLocalLlmFilePath
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAddLocalLlm));
            }
        }
    } = string.Empty;

    public string LocalLlmName
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAddLocalLlm));
            }
        }
    } = string.Empty;

    public bool CanAddLocalLlm => !string.IsNullOrEmpty(SelectedLocalLlmFilePath);

    private async Task LoadLocalLlmGrid()
    {
        LocalLlmModels.Clear();

        var query = new GetLocalLlmFilePaths();
        var result = _queryProcessor.Process(query);

        if (result.IsSuccess)
        {
            LocalLlmModels = new ObservableCollection<LocalLlmFilePathViewModel>(result.Value.Items);
        }
        else
        {
            await DisplayAlertAsync("Error", $"Unable to load LLM models: {result.Error}. Please try again.", "OK");
        }
    }

    private async void OnLocalLLMSettingsPickFileClicked(object? sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(_llmFileTypes);

            var options = new PickOptions
            {
                PickerTitle = "Please select a file",
                FileTypes = customFileType,
            };

            var result = await FilePicker.Default.PickAsync(options);

            if (result != null)
            {
                SelectedLocalLlmFilePath = result.FullPath;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to pick file: {ex.Message}", "OK");
        }
    }

    private async void OnAddLocalLLM(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(SelectedLocalLlmFilePath))
            {
                await DisplayAlertAsync("Error", "Please select a file first.", "OK");
                return;
            }

            if (string.IsNullOrEmpty(LocalLlmName))
            {
                await DisplayAlertAsync("Error", "Please enter a name for the LLM.", "OK");
                return;
            }

            var command = new AddOrUpdateLocalLlmPath(Guid.NewGuid(), LocalLlmName, SelectedLocalLlmFilePath);
            var result = _commandProcessor.Process(command);

            if (result.IsSuccess)
            {
                await DisplayAlertAsync("Success", $"LLM '{LocalLlmName}' added successfully!", "OK");

                // Clear the form after successful addition
                SelectedLocalLlmFilePath = string.Empty;
                LocalLlmName = string.Empty;

                await LoadLocalLlmGrid();
                OnLocalLLMsButtonClicked(sender, e);
            }
            else
            {
                await DisplayAlertAsync("Error", $"Unable to add LLM: {result.Error}. Please try again.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Unable to add LLM: {ex.Message}. Please try again.", "OK");
        }
    }

    private async void OnDeleteLocalLLM(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: LocalLlmFilePathViewModel model })
            {
                // Show confirmation dialog
                var confirmDelete = await DisplayAlertAsync(
                    "Confirm Delete",
                    $"Are you sure you want to remove '{model.Name}'?",
                    "Delete",
                    "Cancel");

                if (!confirmDelete)
                    return; // User cancelled

                var deleteCommand = new RemoveLocalLlmPath(model.Id);

                var result = _commandProcessor.Process(deleteCommand);

                if (result.IsSuccess)
                {
                    await DisplayAlertAsync("Success", $"LLM '{model.Name}' removed successfully!", "OK");
                    await LoadLocalLlmGrid();
                    OnLocalLLMsButtonClicked(sender, e);
                }
                else
                {
                    await DisplayAlertAsync("Error", $"Unable to remove LLM: {result.Error}. Please try again.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to remove LLM: {ex.Message}", "OK");
        }
    }

    #endregion

    #region API LLM Settings

    public ObservableCollection<string> ApiLlmTypeOptions
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

    public ObservableCollection<ApiLlmSettingViewModel> ApiSettings
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

    private async Task LoadApiSettingsGrid()
    {
        ApiSettings.Clear();

        var query = new GetApiLlmSettings();
        var result = _queryProcessor.Process(query);

        if (result.IsSuccess)
        {
            ApiSettings = new ObservableCollection<ApiLlmSettingViewModel>(result.Value.Items);
        }
        else
        {
            await DisplayAlertAsync("Error", $"Unable to load API LLM Settings: {result.Error}. Please try again.", "OK");
        }
    }

    public ApiLlmType? SelectedApiLlmType
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;

                // Drop anything already typed into a URL box that is about to be hidden, so a stale
                // value can't be saved against a provider that has no use for it.
                if (!IsApiLlmUrlVisible)
                {
                    ApiLlmUrl = string.Empty;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedApiLlmTypeDescription));
                OnPropertyChanged(nameof(IsApiLlmUrlVisible));
                OnPropertyChanged(nameof(CanSaveApiLlm));
            }
        }
    }

    // Gemini is reached through the Google.GenAI SDK, which resolves its own endpoint from the API key,
    // so there is no URL for the user to supply. Unknown providers default to requiring one: a new enum
    // member gets the field until someone decides otherwise here.
    public bool IsApiLlmUrlVisible => SelectedApiLlmType switch
    {
        null => false,
        ApiLlmType.GoogleGemini => false,
        _ => true,
    };

    public string SelectedApiLlmTypeDescription
    {
        get => SelectedApiLlmType.HasValue ? _apiLlmTypeDescriptions[SelectedApiLlmType.Value] : DefaultApiSelectionText;
        set
        {
            if (value == DefaultApiSelectionText)
            {
                SelectedApiLlmType = null;
            }
            else
            {
                // Find the enum value that matches this description
                var enumValue = _apiLlmTypeDescriptions.Single(x => x.Value == value).Key;
                SelectedApiLlmType = enumValue;
            }
            OnPropertyChanged();
        }
    }

    public string ApiLlmKey
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveApiLlm));
            }
        }
    } = string.Empty;

    public string ApiLlmUrl
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveApiLlm));
            }
        }
    } = string.Empty;

    // The URL is only part of the contract when it is on screen — requiring it for a provider whose box
    // is hidden would leave the Add button permanently disabled.
    public bool CanSaveApiLlm => SelectedApiLlmType.HasValue
                                 && !string.IsNullOrEmpty(ApiLlmKey)
                                 && (!IsApiLlmUrlVisible || !string.IsNullOrEmpty(ApiLlmUrl));

    private async void OnAddApiLlm(object? sender, EventArgs e)
    {
        try
        {
            if (SelectedApiLlmType == null)
            {
                await DisplayAlertAsync("Error", "Please select an entry from the dropdown.", "OK");
                return;
            }
            if (string.IsNullOrEmpty(ApiLlmKey))
            {
                await DisplayAlertAsync("Error", "Please enter in an Api Key.", "OK");
                return;
            }

            if (IsApiLlmUrlVisible && string.IsNullOrEmpty(ApiLlmUrl))
            {
                await DisplayAlertAsync("Error", "Please enter in an Api Url.", "OK");
                return;
            }

            var command = new AddOrUpdateApiSetting(SelectedApiLlmType.Value.ToString(), ApiLlmKey, ApiLlmUrl);
            var result = _commandProcessor.Process(command);

            if (result.IsSuccess)
            {
                SelectedApiLlmType = null;
                ApiLlmKey = string.Empty;
                ApiLlmUrl = string.Empty;

                await LoadApiSettingsGrid();
                OnApiLLMsButtonClicked(sender, e);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Unable to add Api Setting: {ex.Message}. Please try again.", "OK");
        }
    }

    private async void OnDeleteApiLlmSetting(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: ApiLlmSettingViewModel model })
            {
                // Show confirmation dialog
                var confirmDelete = await DisplayAlertAsync(
                    "Confirm Delete",
                    $"Are you sure you want to remove '{model.Type}'?",
                    "Delete",
                    "Cancel");

                if (!confirmDelete)
                    return; // User cancelled

                var deleteCommand = new RemoveApiSetting(model.Id);

                var result = _commandProcessor.Process(deleteCommand);

                if (result.IsSuccess)
                {
                    await DisplayAlertAsync("Success", $"Api LLM '{model.Type}' removed successfully!", "OK");
                    await LoadApiSettingsGrid();
                    OnApiLLMsButtonClicked(sender, e);
                }
                else
                {
                    await DisplayAlertAsync("Error", $"Unable to remove API LLM: {result.Error}. Please try again.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to remove API LLM: {ex.Message}", "OK");
        }
    }


    #endregion

    #region Collection Grid

    private async void OnLocalLLMsButtonClicked(object? sender, EventArgs e)
    {
        try
        {
            if (Application.Current != null)
            {
                LocalLlmButton.BackgroundColor = Application.Current.RequestedTheme == AppTheme.Dark
                    ? (Color)Application.Current.Resources["ButtonActiveDark"]
                    : (Color)Application.Current.Resources["ButtonActiveLight"];

                ApiLlmButton.BackgroundColor = Application.Current.RequestedTheme == AppTheme.Dark
                    ? (Color)Application.Current.Resources["ButtonInactiveDark"]
                    : (Color)Application.Current.Resources["ButtonInactiveLight"];
            }

            await LoadLocalLlmGrid();
            LocalLlmCollectionView.IsVisible = true;
            ApiCollectionGrid.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load API LLM Settings: {ex.Message}", "OK");
        }
    }

    private async void OnApiLLMsButtonClicked(object? sender, EventArgs e)
    {
        try
        {
            if (Application.Current != null)
            {
                ApiLlmButton.BackgroundColor = Application.Current.RequestedTheme == AppTheme.Dark
                    ? (Color)Application.Current.Resources["ButtonActiveDark"]
                    : (Color)Application.Current.Resources["ButtonActiveLight"];

                LocalLlmButton.BackgroundColor = Application.Current.RequestedTheme == AppTheme.Dark
                    ? (Color)Application.Current.Resources["ButtonInactiveDark"]
                    : (Color)Application.Current.Resources["ButtonInactiveLight"];
            }

            await LoadApiSettingsGrid();
            LocalLlmCollectionView.IsVisible = false;
            ApiCollectionGrid.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load API LLM Settings: {ex.Message}", "OK");
        }

    }

    #endregion
}