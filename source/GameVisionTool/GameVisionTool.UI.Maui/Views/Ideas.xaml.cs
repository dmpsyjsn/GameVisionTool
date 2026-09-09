using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Messages.Commands.Ideas;
using GameVisionTool.Messages.Queries.Ideas;
using Serilog;
using System.Collections.ObjectModel;

namespace GameVisionTool.UI.Maui.Views;

public partial class Ideas
{
    #region private fields

    private const string AddIdeaHeading = "Add Idea";
    private const string EditIdeaHeading = "Edit Idea";

    private readonly IProcessCommand _commandProcessor;
    private readonly IProcessQuery _queryProcessor;

    #endregion

    public Ideas(IProcessCommand commandProcessor, IProcessQuery queryProcessor)
    {
        _commandProcessor = commandProcessor;
        _queryProcessor = queryProcessor;

        InitializeComponent();
        BindingContext = this;
    }

    #region Initialization

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();
            IdeaFormSection.IsVisible = true;
            IdeaCollectionGrid.IsVisible = true;
            IdeaCollectionView.IsVisible = true;
            await LoadIdeaGrid();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex.Message);
            await DisplayAlertAsync("Error", $"Failed to load ideas: {ex.Message}", "OK");
        }
    }

    #endregion

    #region Idea form

    public ObservableCollection<IdeaViewModel> IdeaModels
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

    public string IdeaTitle
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanSaveIdea));
            }
        }
    } = string.Empty;

    // Null means the form is adding; a value means it is editing that row. Everything the form shows
    // about which mode it is in hangs off this.
    public Guid? EditingIdeaId
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEditingIdea));
                OnPropertyChanged(nameof(IdeaFormHeading));
                OnPropertyChanged(nameof(SaveIdeaButtonText));
            }
        }
    }

    public bool IsEditingIdea => EditingIdeaId.HasValue;

    public string IdeaFormHeading => IsEditingIdea ? EditIdeaHeading : AddIdeaHeading;

    public string SaveIdeaButtonText => IsEditingIdea ? "Save Changes" : "Add Idea";

    // A title of nothing but whitespace would store a row the list cannot show, so it does not count
    // as a title.
    public bool CanSaveIdea => !string.IsNullOrWhiteSpace(IdeaTitle);

    private async Task LoadIdeaGrid()
    {
        IdeaModels.Clear();

        var query = new GetIdeas();
        var result = _queryProcessor.Process(query);

        if (result.IsSuccess)
        {
            IdeaModels = new ObservableCollection<IdeaViewModel>(result.Value.Items);
        }
        else
        {
            await DisplayAlertAsync("Error", $"Unable to load ideas: {result.Error}. Please try again.", "OK");
        }
    }

    private void ResetIdeaForm()
    {
        EditingIdeaId = null;
        IdeaTitle = string.Empty;
    }

    #endregion

    #region Create and Update

    private async void OnSaveIdea(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(IdeaTitle))
            {
                await DisplayAlertAsync("Error", "Please enter a title for the idea.", "OK");
                return;
            }

            var title = IdeaTitle.Trim();

            if (EditingIdeaId.HasValue)
            {
                await UpdateExistingIdea(EditingIdeaId.Value, title);
                return;
            }

            await AddNewIdea(title);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Unable to save idea: {ex.Message}. Please try again.", "OK");
        }
    }

    private async Task AddNewIdea(string title)
    {
        var result = _commandProcessor.Process(new AddIdea(title));

        if (result.IsSuccess)
        {
            await DisplayAlertAsync("Success", $"Idea '{title}' added successfully!", "OK");

            ResetIdeaForm();

            await LoadIdeaGrid();
        }
        else
        {
            await DisplayAlertAsync("Error", $"Unable to add idea: {result.Error}. Please try again.", "OK");
        }
    }

    private async Task UpdateExistingIdea(Guid id, string title)
    {
        var result = _commandProcessor.Process(new UpdateIdea(id, title));

        if (result.IsSuccess)
        {
            await DisplayAlertAsync("Success", $"Idea '{title}' updated successfully!", "OK");

            ResetIdeaForm();

            await LoadIdeaGrid();
        }
        else
        {
            // The row can be gone by now - another window, or a delete from this list - so drop out
            // of edit mode rather than leaving the form pointed at an id that no longer resolves.
            ResetIdeaForm();

            await DisplayAlertAsync("Error", $"Unable to update idea: {result.Error}. Please try again.", "OK");

            await LoadIdeaGrid();
        }
    }

    private async void OnEditIdea(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: IdeaViewModel model })
            {
                // Read the row back rather than trusting the bound copy, so the form starts from what
                // is actually stored.
                var result = _queryProcessor.Process(new GetIdea(model.Id));

                if (result.IsFailure)
                {
                    await DisplayAlertAsync("Error", $"Unable to load idea: {result.Error}. Please try again.", "OK");
                    await LoadIdeaGrid();
                    return;
                }

                EditingIdeaId = result.Value.Id;
                IdeaTitle = result.Value.Title;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to load idea: {ex.Message}", "OK");
        }
    }

    private void OnCancelEditIdea(object? sender, EventArgs e) => ResetIdeaForm();

    #endregion

    #region Delete

    private async void OnDeleteIdea(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Button { BindingContext: IdeaViewModel model })
            {
                // Show confirmation dialog
                var confirmDelete = await DisplayAlertAsync(
                    "Confirm Delete",
                    $"Are you sure you want to remove '{model.Title}'?",
                    "Delete",
                    "Cancel");

                if (!confirmDelete)
                    return; // User cancelled

                var deleteCommand = new RemoveIdea(model.Id);

                var result = _commandProcessor.Process(deleteCommand);

                if (result.IsSuccess)
                {
                    // The form would otherwise stay pointed at a row that no longer exists.
                    if (EditingIdeaId == model.Id)
                        ResetIdeaForm();

                    await DisplayAlertAsync("Success", $"Idea '{model.Title}' removed successfully!", "OK");

                    await LoadIdeaGrid();
                }
                else
                {
                    await DisplayAlertAsync("Error", $"Unable to remove idea: {result.Error}. Please try again.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to remove idea: {ex.Message}", "OK");
        }
    }

    #endregion
}
