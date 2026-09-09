using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Messages.Queries.Backstory.Llama;
using GameVisionTool.Messages.Queries.Ideas;
using GameVisionTool.Messages.Queries.Llama;
using GameVisionTool.Messages.Queries.MainSettings;
using Serilog;
using System.Collections.ObjectModel;

namespace GameVisionTool.UI.Maui.Views;

public partial class Backstory
{
    #region private fields

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

        InitializeComponent();
        BindingContext = this;
    }

    #region Initialization

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            IdeaSelectionColumn.IsVisible = true;
            LocalLlmSelectionColumn.IsVisible = true;

            // Both lists are owned by other tabs, so they are reloaded every time this page comes
            // back into view rather than once in the constructor.
            await LoadIdeaOptions();
            await LoadLocalLlmOptions();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex.Message);
            await DisplayAlertAsync("Error", $"Failed to load backstory options: {ex.Message}", "OK");
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
        var modelPath = SelectedLocalLlm?.FullFilePath;

        if (string.IsNullOrWhiteSpace(modelPath) || modelPath == _requestedModelPath)
        {
            return;
        }

        _requestedModelPath = modelPath;
        IsLoadingModel = true;

        try
        {
            var result = await _asyncQueryProcessor.ProcessAsync(new LoadLlamaModel(modelPath));

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

    // A blank brief is allowed - the model is asked to invent one. A model is not optional.
    public bool CanGenerate => SelectedLocalLlm is not null && !IsWorking;

    // Loading weights and generating both hold native state that the inputs can invalidate, so
    // every control on the page is gated on the pair rather than on whichever one owns it.
    public bool IsWorking => IsLoadingModel || IsGenerating;

    private void OnBusyChanged()
    {
        OnPropertyChanged(nameof(IsWorking));
        OnPropertyChanged(nameof(CanSelectIdea));
        OnPropertyChanged(nameof(CanSelectLocalLlm));
        OnPropertyChanged(nameof(CanEditBrief));
        OnPropertyChanged(nameof(CanGenerate));
    }

    private async void OnGenerateBackstoryClicked(object? sender, EventArgs e)
    {
        var modelPath = SelectedLocalLlm?.FullFilePath;

        if (string.IsNullOrWhiteSpace(modelPath))
        {
            return;
        }

        IsGenerating = true;

        try
        {
            var result = await _asyncQueryProcessor.ProcessAsync(
                new GetLlamaScienceFictionBackstoryResponse(modelPath, BackstoryBrief));

            if (result.IsFailure)
            {
                await DisplayAlertAsync("Error", $"Unable to generate backstory: {result.Error}. Please try again.", "OK");
                return;
            }

            GeneratedBackstory = result.Value.Response;
            HasGeneratedBackstory = true;
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

    #endregion
}
