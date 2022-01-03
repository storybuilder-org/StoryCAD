using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using ABI.Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using StoryBuilder.Models;
using WinRT;

namespace StoryBuilder.ViewModels.Tools
{
    public class PrintReportDialogVM : ObservableRecipient
    {

        private string _reportType;
        /// <summary>
        /// What type of report should be made
        ///
        /// Possible values
        /// Scrivener, Preview, printer
        /// </summary>
        public string ReportType
        {
            get => _reportType;
            set => SetProperty(ref _reportType, value);
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

        private bool _selectAllSetting;
        public bool SelectAllSettings
        {
            get => _selectAllScenes;
            set => SetProperty(ref _selectAllSetting, value);
        }

        private bool _createOverview;
        public bool CreateOverview
        {
            get => _createOverview;
            set => SetProperty(ref _createOverview, value);
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

        public void TraverseNode(StoryNodeItem node)
        {
            switch (node.Type)
            {
                case StoryItemType.Problem: ProblemNodes.Add(node); break;
                case StoryItemType.Character: CharacterNodes.Add(node); break;
                case StoryItemType.Setting: SettingNodes.Add(node); break;
                case StoryItemType.Scene: SceneNodes.Add(node); break;
            }

            //Recurs until children are empty 
            foreach (StoryNodeItem storyNodeItem in node.Children) { TraverseNode(storyNodeItem); }
        }

    }
}
