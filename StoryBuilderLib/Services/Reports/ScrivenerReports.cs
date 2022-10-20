using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Scrivener;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Services.Reports;

public class ScrivenerReports
{

    private StoryModel _model;
    private ScrivenerIo _scrivener;
    private ReportFormatter _formatter;

    private BinderItem _binderNode;         // Binder root node (BinderItems' parent)
    private BinderItem _storyBuilderNode;   //   StoryBuilder folder node
    private BinderItem _explorerNode;       //     StoryExplorer subfolder node
    private BinderItem _narratorNode;       //     StoryNarrator subfolder node
    private BinderItem _miscNode;           //     Miscellaneous subfolder node
    private BinderItem _problemListNode;    //       List of Problems report
    private BinderItem _characterListNode;  //       List of Characters report
    private BinderItem _settingListNode;    //       List of Settings report
    private BinderItem _sceneListNode;      //       List of Scenes report
    private BinderItem _synopsisNode;       //       Concatenated Scene synopses (a poor man's story synopsis)
    private XmlElement _newStbRoot;
    //private List<BinderItem> _narratorViewItems;

    #region Constructor

    public ScrivenerReports(StorageFile file, StoryModel model)
    {
        _scrivener = Ioc.Default.GetRequiredService<ScrivenerIo>();
        _scrivener.ScrivenerFile = file;
        _model = model;
        Ioc.Default.GetService<StoryReader>();
        _formatter = new ReportFormatter();
        //_root = root;
        //_misc = miscFolder;
    }

    #endregion

    #region public methods

    public async Task GenerateReports()
    {
        await _scrivener.LoadScrivenerProject();  // Load the Scrivener project
        await _formatter.LoadReportTemplates(); // Load text report templates
        //TODO: load templates from within ReportFormatter
        _binderNode = _scrivener.BuildBinderItemTree(); // Build a BinderItem model
        UpdateStoryBuilderOutline();  // Replace or add StoryBuilder BinderItems to model

        await RecurseStoryElementReports(_explorerNode);
        await RecurseStoryElementReports(_narratorNode);
        await GenerateProblemListReport(_problemListNode);
        await GenerateCharacterListReport(_characterListNode);
        await GenerateSettingListReport(_settingListNode);
        await GenerateSceneListReport(_sceneListNode);
        await GenerateSynopsisReport(_synopsisNode);
        //await ProcessPreviousNotes();
        // Narrative view processing (into manuscript)

        AddCustomMetaDataSettings();  // Add new metadata tag if needed
        MatchDraftFolderToNarrator();

        SetLabelSettings();     // Add or replace my binder Label settings

        _newStbRoot = _scrivener.CreateFromBinder(_storyBuilderNode);
        await _scrivener.WriteTestFile("newstb.xml", _newStbRoot); // Debugging
        UpdateStoryBuilder();
        await _scrivener.SaveScrivenerProject(_scrivener.ScrivenerFile);
    }

    private void UpdateStoryBuilder()
    {
        if (_scrivener.StoryBuilder != null)
        {
            IXmlNode _Parent = _scrivener.StoryBuilder.ParentNode;
            _Parent.ReplaceChild(_newStbRoot, _scrivener.StoryBuilder);
        }
        else
            _scrivener.Binder.InsertBefore(_newStbRoot, _scrivener.Research);
    }

    private void MatchDraftFolderToNarrator()
    {
        ListDraftFolderContents();
        // _narratorViewItems = ListNarratorViewContents();
    }

    private void ListDraftFolderContents()
    {
        BinderItem _DraftFolder = null;
        // Find the root DraftFolder BinderItem
        foreach (BinderItem _Child in _binderNode.Children)
            if (_Child.Type == BinderItemType.DraftFolder)
            {
                _DraftFolder = _Child;
                break;
            }
        List<BinderItem> _DraftFolderItems = new();
        foreach (BinderItem _Node in _DraftFolder!)
        {
            _DraftFolderItems.Add(_Node);
        }
    }

    #endregion

    #region Generate StoryBuilder model in BinderItem format

    public void UpdateStoryBuilderOutline()
    {
        // Locate StoryBuilder BinderItem or insert just before Research folder
        _storyBuilderNode = LocateFolder(_binderNode, "StoryBuilder");
        if (_storyBuilderNode == null)
            _storyBuilderNode = InsertFolderBefore(_binderNode, "Research", "StoryBuilder");
        // Locate or add StoryBuilder's three child folders
        _explorerNode = LocateFolder(_storyBuilderNode, "Explorer");
        if (_explorerNode == null)
            _explorerNode = AddFolder(_storyBuilderNode, "Explorer");
        _explorerNode.Children.Clear();
        _narratorNode = LocateFolder(_storyBuilderNode, "Narrator");
        if (_narratorNode == null)
            _narratorNode = AddFolder(_storyBuilderNode, "Narrator");
        _narratorNode.Children.Clear();
        _miscNode = LocateFolder(_storyBuilderNode, "Miscellaneous");
        if (_miscNode == null)
            _miscNode = AddFolder(_storyBuilderNode, "Miscellaneous");
        // Locate or add the Miscellaneous folder's child reports
        _problemListNode = LocateText(_miscNode, "List of Problems");
        if (_problemListNode == null)
            _problemListNode = AddText(_miscNode, "List of Problems");
        _characterListNode = LocateText(_miscNode, "List of Characters");
        if (_characterListNode == null)
            _characterListNode = AddText(_miscNode, "List of Characters");
        _settingListNode = LocateText(_miscNode, "List of Settings");
        if (_settingListNode == null)
            _settingListNode = AddText(_miscNode, "List of Settings");
        _sceneListNode = LocateText(_miscNode, "List of Scenes");
        if (_sceneListNode == null)
            _sceneListNode = AddText(_miscNode, "List of Scenes");
        _synopsisNode = LocateText(_miscNode, "Story Synopsis");
        if (_synopsisNode == null)
            _synopsisNode = AddText(_miscNode, "Story Synopsis");
        AddStoryExplorerNodes();
        AddStoryNarratorNodes();
    }

    private void AddStoryExplorerNodes()
    {
        RecurseStoryModelNode(_model.ExplorerView[0], _explorerNode);
    }

    private void AddStoryNarratorNodes()
    {
        RecurseStoryModelNode(_model.NarratorView[0], _narratorNode);
    }

    public BinderItem LocateFolder(BinderItem parent, string title)
    {
        // See if there if the desired folder exists under parent
        foreach (BinderItem _Child in parent.Children)
            if (_Child.Title == title)
                return _Child;

        return null;
    }

    public BinderItem AddFolder(BinderItem parent, string title)
    {
        BinderItem _Item = new(0, NewUuid(), BinderItemType.Folder, title);
        parent.Children.Add(_Item);
        return _Item;
    }

    public BinderItem InsertFolderBefore(BinderItem parent, string after, string title)
    {
        BinderItem _Item = new(0, NewUuid(), BinderItemType.Folder, title);
        parent.Children.Insert(FolderIndex(after), _Item);
        return _Item;
    }

    private int FolderIndex(string title)
    {
        for (int _I = 0; _I < _binderNode.Children.Count; _I++)
            if (_binderNode.Children[_I].Title.Equals(title))
                return _I;
        return -1;
    }

    public BinderItem LocateText(BinderItem parent, string title)
    {
        // See if there if the desired file exists under parent
        foreach (BinderItem _Child in parent.Children)
            if (_Child.Title == title)
                return _Child;

        return null;
    }

    public BinderItem AddText(BinderItem parent, string title)
    {
        BinderItem _Item = new(0, NewUuid(), BinderItemType.Text, title);
        parent.Children.Add(_Item);
        return _Item;
    }

    private static void RecurseStoryModelNode(StoryNodeItem node, BinderItem parent)
    {
        BinderItemType _Type;
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (node.Type)
        {
            case StoryItemType.Section:
                _Type = BinderItemType.Folder;
                break;
            case StoryItemType.Folder:
                _Type = BinderItemType.Folder;
                break;
            default:
                _Type = BinderItemType.Text;
                break;
        }
        BinderItem _BinderItem = new(0, StoryWriter.UuidString(node.Uuid), _Type, node.Name, parent);
        foreach (StoryNodeItem _Child in node.Children)
            RecurseStoryModelNode(_Child, _BinderItem);
    }

    #endregion

    #region Generate StoryBuilder reports under StoryBuilder model

    /// <summary>
    /// This method builds the reports under the StoryExplorer
    /// node, which consists of a tree of BinderItem nodes matching
    /// the StoryBuilder's StoryExplorer outline.
    /// 
    /// For each node the corresponding StoryElement is found
    /// (based on the matching Guid) and the report is generated
    /// as a content.rtf file in the BinderItem's subfolder.
    /// </summary>
    /// <param name="node">BinderItem node</param>
    /// <returns></returns>
    private async Task RecurseStoryElementReports(BinderItem node)
    {
        StoryElement _Element = null;
        Guid _Uuid = new(node.Uuid);
        if (_model.StoryElements.StoryElementGuids.ContainsKey(_Uuid))
            _Element = _model.StoryElements.StoryElementGuids[_Uuid];
        if (_Element != null)
        {
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (_Element.Type)
            {
                case StoryItemType.StoryOverview:
                    await GenerateStoryOverviewReport(node, _Element);
                    break;
                case StoryItemType.Problem:
                    await GenerateProblemReport(node, _Element);
                    break;
                case StoryItemType.Character:
                    await GenerateCharacterReport(node, _Element);
                    break;
                case StoryItemType.Setting:
                    await GenerateSettingReport(node, _Element);
                    break;
                case StoryItemType.Scene:
                    await GenerateSceneReport(node, _Element);
                    break;
                case StoryItemType.Folder:
                    await GenerateFolderReport(node, _Element);
                    break;
                case StoryItemType.Section:
                    await GenerateSectionReport(node, _Element);
                    break;
            }
        }
        foreach (BinderItem _Child in node.Children)
            await RecurseStoryElementReports(_Child);
    }

    private async Task GenerateStoryOverviewReport(BinderItem node, StoryElement element)
    {
        OverviewModel _Overview = (OverviewModel)element;

        // Locate and open the output content.rtf report
        StorageFolder _Di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
        StorageFile _Contents = await _Di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
 
        string _Rtf = _formatter.FormatStoryOverviewReport(_Overview);
            
        // Write the report
        await FileIO.WriteTextAsync(_Contents, _Rtf);
    }

    private async Task GenerateProblemListReport(BinderItem node)
    {
        // Locate and open the output content.rtf report
        StorageFolder _Di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
        StorageFile _Contents = await _Di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
        string _Rtf =  _formatter.FormatProblemListReport();
        // Write the report
        await FileIO.WriteTextAsync(_Contents, _Rtf);
    }

    private async Task GenerateProblemReport(BinderItem node, StoryElement element)
    {
        // Locate and open the output content.rtf report
        StorageFolder _Di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
        StorageFile _Contents = await _Di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

        string _Rtf = _formatter.FormatProblemReport(element);
            
        // Write the report
        await FileIO.WriteTextAsync(_Contents, _Rtf);
    }

    private async Task GenerateCharacterListReport(BinderItem node)
    {
        // Locate and open the output content.rtf report
        StorageFolder _Di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
        StorageFile _Contents = await _Di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
        string _Rtf = _formatter.FormatCharacterListReport();
        // Write the report
        await FileIO.WriteTextAsync(_Contents, _Rtf);
    }

    private async Task GenerateCharacterReport(BinderItem node, StoryElement element)
    {
        // Locate and open the output content.rtf report
        StorageFolder _Di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
        StorageFile _Contents = await _Di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

        string _Rtf = _formatter.FormatCharacterReport(element);
            
        // Write the report
        await FileIO.WriteTextAsync(_Contents, _Rtf);
    }

    private async Task GenerateSettingListReport(BinderItem node)
    {
        // Locate and open the output content.rtf report
        StorageFolder _Di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
        StorageFile _Contents = await _Di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

        string _Rtf = _formatter.FormatSettingListReport();
            
        // Write the report
        await FileIO.WriteTextAsync(_Contents, _Rtf);
    }

    private async Task GenerateSettingReport(BinderItem node, StoryElement element)
    {
        // Locate and open the output content.rtf report
        StorageFolder _Di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
        StorageFile _Contents = await _Di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);
 
        string _Rtf = _formatter.FormatSettingReport(element);
            
        // Write the report
        await FileIO.WriteTextAsync(_Contents, _Rtf);
    }

    private async Task GenerateSceneListReport(BinderItem node)
    {
        // Locate and open the output content.rtf report
        StorageFolder _Di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
        StorageFile _Contents = await _Di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

        string _Rtf = _formatter.FormatSceneListReport();
            
        // Write the report
        await FileIO.WriteTextAsync(_Contents, _Rtf);
    }

    private async Task GenerateSceneReport(BinderItem node, StoryElement element)
    {
        // Locate and open the output content.rtf report
        StorageFolder _Di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
        StorageFile _Contents = await _Di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

        string _Rtf = _formatter.FormatSceneReport(element);

        // Write the report
        await FileIO.WriteTextAsync(_Contents, _Rtf);
    }

    private async Task GenerateFolderReport(BinderItem node, StoryElement element)
    {
        // Locate and open the output content.rtf report
        StorageFolder _Di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
        StorageFile _Contents = await _Di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

        string _Rtf = _formatter.FormatFolderReport(element);

        // Write the report
        await FileIO.WriteTextAsync(_Contents, _Rtf);
    }

    private async Task GenerateSectionReport(BinderItem node, StoryElement element)
    {
        // Locate and open the output content.rtf report
        StorageFolder _Di = await _scrivener.GetSubFolder(node.Uuid); // Get subfolder path
        StorageFile _Contents = await _Di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

        string _Rtf = _formatter.FormatSectionReport(element);

        // Write the report
        await FileIO.WriteTextAsync(_Contents, _Rtf);
    }

    private async Task GenerateSynopsisReport(BinderItem node)
    {
        // Locate and open the output content.rtf report
        StorageFolder _Di = await _scrivener.GetSubFolder(node.Uuid);
        StorageFile _Contents = await _Di.CreateFileAsync("content.rtf", CreationCollisionOption.ReplaceExisting);

        string _Rtf = _formatter.FormatSynopsisReport();

        // Write the report
        await FileIO.WriteTextAsync(_Contents, _Rtf);
    }


    private static string NewUuid()
    {
        string _Id = Guid.NewGuid().ToString("B").ToUpper();
        _Id = _Id.Replace("{", string.Empty);
        _Id = _Id.Replace("}", string.Empty);
        return _Id;
    }

    #endregion

    #region Generate StoryBuilder metadata and scene BinderItems under DraftFolder

    private void AddCustomMetaDataSettings()
    {
        XmlElement _CustomMetaData = (XmlElement)_scrivener.CustomMetaDataSettings;
        IXmlNode _StbUuid = (XmlElement)_scrivener.StbUuidSetting;

        if (_StbUuid != null)        // the setting already exits
            return;

        if (_CustomMetaData == null)
        {
            _CustomMetaData = _scrivener.XmlDocument.CreateElement("CustomMetaDataSettings");
            _scrivener.ScrivenerProject.InsertBefore(_CustomMetaData, _scrivener.ProjectBookMarks);
        }

        _StbUuid = _scrivener.XmlDocument.CreateElement("MetaDataField");

        XmlAttribute _Attr = _scrivener.XmlDocument.CreateAttribute("Type");
        _Attr.Value = "Text";
        _StbUuid.Attributes.SetNamedItem(_Attr);
        _Attr = _scrivener.XmlDocument.CreateAttribute("ID");
        _Attr.Value = "stbuuid";
        _StbUuid.Attributes.SetNamedItem(_Attr);
        _Attr = _scrivener.XmlDocument.CreateAttribute("Wraps");
        _Attr.Value = "No";
        _StbUuid.Attributes.SetNamedItem(_Attr);
        _Attr = _scrivener.XmlDocument.CreateAttribute("Color");
        _Attr.Value = "1.000000 0.666667 0.498039";
        _StbUuid.Attributes.SetNamedItem(_Attr);
        IXmlNode _Title = _scrivener.XmlDocument.CreateElement("Title");
        _Title.InnerText = "stbuuid";
        _StbUuid.AppendChild(_Title);

        _CustomMetaData.AppendChild(_StbUuid);
    }

    #endregion

    #region Generate my binder LabelSettings

    private void SetLabelSettings()
    {
        // Create a replacement LabelSettings node with my label values
        XmlElement _LabelSettings = _scrivener.XmlDocument.CreateElement("LabelSettings");

        IXmlNode _Title = _scrivener.XmlDocument.CreateElement("Title");
        _Title.InnerText = "Binder Labels";
        _LabelSettings.AppendChild(_Title);
        IXmlNode _DefaultId = _scrivener.XmlDocument.CreateElement("DefaultLabelID");
        _Title.InnerText = "-1";
        _LabelSettings.AppendChild(_DefaultId);
        IXmlNode _Labels = _scrivener.XmlDocument.CreateElement("Labels");
        _LabelSettings.AppendChild(_Labels);
        // Generate each label 
        IXmlNode _Label = _scrivener.XmlDocument.CreateElement("Label");
        XmlAttribute _Attr = _scrivener.XmlDocument.CreateAttribute("ID");
        _Attr.Value = "-1";
        _Label.Attributes.SetNamedItem(_Attr);
        _Label.InnerText = "No Label";
        _Labels.AppendChild(_Label);
        _Label = _scrivener.XmlDocument.CreateElement("Label");
        _Attr = _scrivener.XmlDocument.CreateAttribute("ID");
        _Attr.Value = "1";
        _Label.Attributes.SetNamedItem(_Attr);
        _Labels.AppendChild(_Label);
        _Attr = _scrivener.XmlDocument.CreateAttribute("Color");
        _Attr.Value = "1.000000 1.000000 1.000000";
        _Label.Attributes.SetNamedItem(_Attr);
        _Label.InnerText = "Not Started";
        _Labels.AppendChild(_Label);
        _Label = _scrivener.XmlDocument.CreateElement("Label");
        _Attr = _scrivener.XmlDocument.CreateAttribute("ID");
        _Attr.Value = "2";
        _Labels.AppendChild(_Label);
        _Attr = _scrivener.XmlDocument.CreateAttribute("Color");
        _Attr.Value = "1.000000 1.000000 0.000000";
        _Label.Attributes.SetNamedItem(_Attr);
        _Labels.AppendChild(_Label);
        _Attr = _scrivener.XmlDocument.CreateAttribute("Color");
        _Attr.Value = "1.000000 1.000000 0.000000";
        _Label.Attributes.SetNamedItem(_Attr);
        _Label.InnerText = "In Progress";
        _Labels.AppendChild(_Label);
        _Label = _scrivener.XmlDocument.CreateElement("Label");
        _Attr = _scrivener.XmlDocument.CreateAttribute("ID");
        _Attr.Value = "3";
        _Label.Attributes.SetNamedItem(_Attr);
        _Labels.AppendChild(_Label);
        _Attr = _scrivener.XmlDocument.CreateAttribute("Color");
        _Attr.Value = "1.000000 0.000000 0.000000";
        _Label.Attributes.SetNamedItem(_Attr);
        _Label.InnerText = "First Draft";
        _Labels.AppendChild(_Label);
        _Label = _scrivener.XmlDocument.CreateElement("Label");
        _Attr = _scrivener.XmlDocument.CreateAttribute("ID");
        _Attr.Value = "4";
        _Label.Attributes.SetNamedItem(_Attr);
        _Labels.AppendChild(_Label);
        _Attr = _scrivener.XmlDocument.CreateAttribute("Color");
        _Attr.Value = "0.333333 1.000000 0.000000";
        _Label.Attributes.SetNamedItem(_Attr);
        _Label.InnerText = "Reviewed";
        _Labels.AppendChild(_Label);
        _Label = _scrivener.XmlDocument.CreateElement("Label");
        _Attr = _scrivener.XmlDocument.CreateAttribute("ID");
        _Attr.Value = "5";
        _Label.Attributes.SetNamedItem(_Attr);
        _Labels.AppendChild(_Label);
        _Attr = _scrivener.XmlDocument.CreateAttribute("Color");
        _Attr.Value = "0.000000 0.666667 0.000000";
        _Label.Attributes.SetNamedItem(_Attr);
        _Label.InnerText = "Revised Draft";
        _Labels.AppendChild(_Label);
        _Label = _scrivener.XmlDocument.CreateElement("Label");
        _Attr = _scrivener.XmlDocument.CreateAttribute("ID");
        _Attr.Value = "6";
        _Label.Attributes.SetNamedItem(_Attr);
        _Labels.AppendChild(_Label);
        _Attr = _scrivener.XmlDocument.CreateAttribute("Color");
        _Attr.Value = "0.000000 0.666667 0.000000";
        _Label.Attributes.SetNamedItem(_Attr);
        _Label.InnerText = "Final Draft";
        _Labels.AppendChild(_Label);
        _Label = _scrivener.XmlDocument.CreateElement("Label");
        _Attr = _scrivener.XmlDocument.CreateAttribute("ID");
        _Attr.Value = "7";
        _Label.Attributes.SetNamedItem(_Attr);
        _Labels.AppendChild(_Label);
        _Attr = _scrivener.XmlDocument.CreateAttribute("Color");
        _Attr.Value = "0.262745 0.262745 0.396078";
        _Label.Attributes.SetNamedItem(_Attr);
        _Label.InnerText = "Done";
        _Labels.AppendChild(_Label);

        if (_scrivener.LabelSettings != null)
        {
            IXmlNode _Parent = _scrivener.LabelSettings.ParentNode;
            _Parent.ReplaceChild(_LabelSettings, _scrivener.LabelSettings);
        }
        else
            _scrivener.ScrivenerProject.InsertBefore(_LabelSettings, _scrivener.StatusSettings);

    }

    #endregion
}