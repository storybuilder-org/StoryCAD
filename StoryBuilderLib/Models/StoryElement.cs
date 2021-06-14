using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Data.Xml.Dom;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace StoryBuilder.Models
{
    public class StoryElement : ObservableObject
    {
        //private const SymbolIcon OverviewIcon = new SymbolIcon {Symbol = "Cloud"};

        #region  Properties

        private readonly Guid _uuid;
        public Guid Uuid
        {
            get => _uuid;
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        private StoryItemType _type;
        public StoryItemType Type
        {
            get => _type;
            set => _type = value;
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return _uuid.ToString();
        }

        #endregion

        #region Constructor

        public StoryElement(string name, StoryItemType type, StoryModel model)
        {
            _uuid = Guid.NewGuid();
            _name = name;
            _type = type;
            model.StoryElements.Add(this);
        }

        public StoryElement(IXmlNode xn, StoryModel model)
        {
            Guid uuid = default;
            StoryItemType type = StoryItemType.Unknown;
            string name = string.Empty;
            bool uuidFound = false;
            bool nameFound = false;
            Type = StoryItemType.Unknown;
            switch (xn.NodeName)
            {
                case "Overview":
                    type = StoryItemType.StoryOverview;
                    break;
                case "Problem":
                    type = StoryItemType.Problem;
                    break;
                case "Character":
                    type = StoryItemType.Character;
                    break;
                case "Setting":
                    type = StoryItemType.Setting;
                    break;
                case "PlotPoint":  
                    type = StoryItemType.PlotPoint;
                    break;
                case "Separator":       // Legacy: Separator was renamed to Folder
                    type = StoryItemType.Folder;
                    break;
                case "Folder":   
                    type = StoryItemType.Folder;
                    break;
                case "Section":  
                    type = StoryItemType.Section;
                    break;
                case "TrashCan":
                    type = StoryItemType.TrashCan;
                    break;
            }
            foreach (var attr in xn.Attributes)
            {
                switch (attr.NodeName)
                {
                    case ("UUID"):
                        uuid = new Guid(attr.InnerText);
                        uuidFound = true;
                        break;
                    case ("Name"):
                        name = attr.InnerText;
                        nameFound = true;
                        break;
                }
                if (uuidFound && nameFound)
                    break;
            }
            _uuid = uuid;
            _name = name;
            _type = type;
            model.StoryElements.Add(this);
        }

        #endregion
    }
}
