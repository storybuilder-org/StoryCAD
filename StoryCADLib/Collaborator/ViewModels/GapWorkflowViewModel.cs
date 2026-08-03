using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoryCADLib.Collaborator.Models;

namespace StoryCADLib.Collaborator.ViewModels;

/// <summary>
/// Outline gaps page — navigate only (issue #107 phase 6).
/// Each missing field links to its helper workflow or the host element.
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public partial class GapWorkflowViewModel : ObservableRecipient
{
    public GapWorkflowViewModel()
    {
        Groups = new ObservableCollection<GapElementGroup>();
        OpenElementCommand = new RelayCommand<Guid>(guid => OnOpenElement?.Invoke(guid));
        OpenFieldCommand = new AsyncRelayCommand<GapFieldLink>(OpenFieldAsync);
    }

    public string Title { get; set; } = "Outline gaps";

    public string Description { get; set; } =
        "Required fields that are empty or broken. Click a field to open its helper workflow, or the element name to open it in StoryCAD.";

    public ObservableCollection<GapElementGroup> Groups { get; }

    public bool HasGroups => Groups.Count > 0;

    public string EmptyMessage { get; set; } = "No required-field gaps.";

    /// <summary>Select/focus element in host StoryCAD.</summary>
    public Action<Guid> OnOpenElement { get; set; }

    /// <summary>Start a Collaborator workflow by registry label.</summary>
    public Func<string, Task> OnOpenWorkflow { get; set; }

    public RelayCommand<Guid> OpenElementCommand { get; }
    public IAsyncRelayCommand<GapFieldLink> OpenFieldCommand { get; }

    public void Load(GapWorkflowPayload payload)
    {
        Groups.Clear();
        if (payload?.Groups != null)
        {
            foreach (var g in payload.Groups)
                Groups.Add(g);
        }
        OnPropertyChanged(nameof(HasGroups));
    }

    private async Task OpenFieldAsync(GapFieldLink field)
    {
        if (field == null)
            return;

        if (field.OpensElementOnly)
        {
            OnOpenElement?.Invoke(field.ElementGuid);
            return;
        }

        if (OnOpenWorkflow != null)
            await OnOpenWorkflow(field.WorkflowLabel);
    }
}
