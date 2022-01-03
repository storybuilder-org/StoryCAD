using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using StoryBuilder.Models;

namespace StoryBuilder.ViewModels.Tools
{
    public class PrintReportDialogVM : ObservableRecipient
    {
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
