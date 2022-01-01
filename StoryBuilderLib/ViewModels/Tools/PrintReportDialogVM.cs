using System;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;


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

        private List<StoryNodeItem> _allProblemNodes = new();
        public List<StoryNodeItem> AllProblemNodes
        {
            get => _allProblemNodes;
            set => SetProperty(ref _allProblemNodes, value);
        }

        private List<StoryNodeItem> _allCharacterNodes = new();
        public List<StoryNodeItem> AllCharacterNodes
        {
            get => _allCharacterNodes;
            set => SetProperty(ref _allCharacterNodes, value);
        }

        private List<StoryNodeItem> _allSettingNodes = new();
        public List<StoryNodeItem> AllSettingNodes
        {
            get => _allSettingNodes;
            set => SetProperty(ref _allSettingNodes, value);
        }

        private List<StoryNodeItem> _allSceneNodes = new();
        public List<StoryNodeItem> AllSceneNodes
        {
            get => _allSceneNodes;
            set => SetProperty(ref _allSceneNodes, value);
        }

        public void TraverseNode(StoryNodeItem node)
        {
            switch (node.Type)
            {
                case StoryItemType.Problem: AllProblemNodes.Add(node); break;
                case StoryItemType.Character: AllCharacterNodes.Add(node); break;
                case StoryItemType.Setting: AllSettingNodes.Add(node); break;
                case StoryItemType.Scene: AllSceneNodes.Add(node); break;
            }

            //Recurs until children are empty 
            foreach (StoryNodeItem storyNodeItem in node.Children) { TraverseNode(storyNodeItem); }
        }
    }
}
