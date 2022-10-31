using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Models;
using StoryBuilder.Services.Reports;

namespace StoryBuilder.ViewModels.Tools;

public class PrintReportDialogVM : ObservableRecipient
{
    public ContentDialog Dialog;

    private int _LoadingBarOpacity = 0;
    public int LoadingBarOpacity
    {
        get => _LoadingBarOpacity;
        set => SetProperty(ref _LoadingBarOpacity, value);
    }

    private bool _createSummary;
    public bool CreateSummary
    {
        get => _createSummary;
        set => SetProperty(ref _createSummary, value);
    }
    private bool _selectAllProblems;
    public bool SelectAllProblems
    {
        get => _selectAllProblems;
        set => SetProperty(ref _selectAllProblems, value);
    }

    private bool _selectAllCharacters;
    public bool SelectAllCharacters
    {
        get => _selectAllCharacters;
        set => SetProperty(ref _selectAllCharacters, value);
    }

    private bool _selectAllScenes;
    public bool SelectAllScenes
    {
        get => _selectAllScenes;
        set => SetProperty(ref _selectAllScenes, value);
    }

    private bool _selectAllWeb;
    public bool SelectAllWeb
    {
        get => _selectAllWeb;
        set => SetProperty(ref _selectAllWeb, value);
    }

    private bool _selectAllSetting;
    public bool SelectAllSettings
    {
        get => _selectAllSetting;
        set => SetProperty(ref _selectAllSetting, value);
    }

    private bool _createOverview;
    public bool CreateOverview
    {
        get => _createOverview;
        set => SetProperty(ref _createOverview, value);
    }

    private bool _problemList;
    public bool ProblemList
    {
        get => _problemList;
        set => SetProperty(ref _problemList, value);
    }

    private bool _characterList;
    public bool CharacterList
    {
        get => _characterList;
        set => SetProperty(ref _characterList, value);
    }

    private bool _settingList;
    public bool SettingList
    {
        get => _settingList;
        set => SetProperty(ref _settingList, value);
    }

    private bool _sceneList;
    public bool SceneList
    {
        get => _sceneList;
        set => SetProperty(ref _sceneList, value);
    }

    private bool _webList;
    public bool WebList
    {
        get => _webList;
        set => SetProperty(ref _webList, value);
    }

    private List<StoryNodeItem> _selectedNodes = new();
    public List<StoryNodeItem> SelectedNodes
    {
        get => _selectedNodes;
        set => SetProperty(ref _selectedNodes, value);
    }

    private List<StoryNodeItem> _problemNodes = new();
    public List<StoryNodeItem> ProblemNodes 
    {
        get => _problemNodes;
        set => SetProperty(ref _problemNodes, value);
    }

    private List<StoryNodeItem> _characterNodes = new();
    public List<StoryNodeItem> CharacterNodes
    {
        get => _characterNodes;
        set => SetProperty(ref _characterNodes, value);
    }

    private List<StoryNodeItem> _settingNodes = new();
    public List<StoryNodeItem> SettingNodes
    {
        get => _settingNodes;
        set => SetProperty(ref _settingNodes, value);
    }

    private List<StoryNodeItem> _sceneNodes = new();
    public List<StoryNodeItem> SceneNodes
    {
        get => _sceneNodes;
        set => SetProperty(ref _sceneNodes, value);
    }

    private List<StoryNodeItem> _webNodes = new();
    public List<StoryNodeItem> WebNodes
    {
        get => _webNodes;
        set => SetProperty(ref _webNodes, value);
    }

    public void TraverseNode(StoryNodeItem node)
    {
        switch (node.Type)
        {
            case StoryItemType.Problem: ProblemNodes.Add(node); break;
            case StoryItemType.Character: CharacterNodes.Add(node); break;
            case StoryItemType.Setting: SettingNodes.Add(node); break;
            case StoryItemType.Scene: SceneNodes.Add(node); break;
            case StoryItemType.Web: WebNodes.Add(node); break;
        }

        //Recurs until children are empty 
        foreach (StoryNodeItem storyNodeItem in node.Children) { TraverseNode(storyNodeItem); }
    }

    public async void StartGeneratingReports()
    {
        DispatcherQueue.GetForCurrentThread().TryEnqueue(() => { LoadingBarOpacity = 1; });
        BackgroundWorker backgroundthread = new();
        backgroundthread.DoWork += GenerateReports;
        backgroundthread.RunWorkerAsync();
    }

    private async void GenerateReports(object sender, DoWorkEventArgs e)
    {
        PrintReportDialogVM ReportVM = Ioc.Default.GetRequiredService<PrintReportDialogVM>();
        ShellViewModel ShellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
        PrintReports rpt = new(ReportVM, ShellVM.StoryModel);
        await rpt.Generate();
        DispatcherQueue.GetForCurrentThread().TryEnqueue(() => { Dialog.Hide(); })
;    }

    public void CloseDialog() { Dialog.Hide(); }
}