using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Net.Sgoliver.NRtfTree.Core;
using StoryBuilder.Models.Scrivener;
using XmlDocument = Windows.Data.Xml.Dom.XmlDocument;
using XmlElement = Windows.Data.Xml.Dom.XmlElement;
using XmlText = Windows.Data.Xml.Dom.XmlText;

namespace StoryBuilder.DAL
{
    /// <summary>
    /// A data source that provides methods to read, parse, 
    /// and update a Scrivener project. 
    /// </summary>
    public class ScrivenerIo
    {
        // The .scrivx document
        public XmlDocument XmlDocument;
        public StorageFile ScrivenerFile;
        public string ProjectPath;
        public IXmlNode ScrivenerProject;
        public IXmlNode Binder;
        public IXmlNode StoryBuilder;
        public IXmlNode Research;
        public IXmlNode LabelSettings;
        public IXmlNode StatusSettings;
        public IXmlNode CustomMetaDataSettings;
        public IXmlNode StbUuidSetting;
        public IXmlNode ProjectBookMarks;


        /// <summary>
        /// Create a BinderItem tree from a .scrivx Scrivener project file.
        ///
        /// This method builds the tree via recursive descent through
        /// the XML.
        ///
        /// Note that this is not currently a complete parse of the
        /// Scrivener project file. It focuses on what's needed to add
        /// or replace a StoryBuilder project into the file.
        /// </summary>
        /// <returns>model's root BinderItem</returns>
        public async Task LoadScrivenerProject()
        {
            XmlDocument = await XmlDocument.LoadFromFileAsync(ScrivenerFile);
            ScrivenerProject = XmlDocument.SelectSingleNode("ScrivenerProject");
            Binder = XmlDocument.SelectSingleNode("ScrivenerProject//Binder");
            StoryBuilder = XmlDocument.SelectSingleNode("ScrivenerProject//Binder//BinderItem[Title='StoryBuilder']");
            Research = XmlDocument.SelectSingleNode("ScrivenerProject//Binder//BinderItem[Title='Research']");
            LabelSettings = XmlDocument.SelectSingleNode("ScrivenerProject//LabelSettings");
            StatusSettings = XmlDocument.SelectSingleNode("ScrivenerProject//StatusSettings");
            CustomMetaDataSettings = XmlDocument.SelectSingleNode("ScrivenerProject//CustomMetaDataSettings");
            StbUuidSetting = XmlDocument.SelectSingleNode("ScrivenerProject//CustomMetaDataSettings//MetaDataField[Title='stbuuid']");
            ProjectBookMarks = XmlDocument.SelectSingleNode("ScrivenerProject//ProjectBookmarks");
        }

        public async Task SaveScrivenerProject(StorageFile file)
        {
            await XmlDocument.SaveToFileAsync(file);
        }

        public BinderItem BuildBinderItemTree()
        {
            BinderItem root = new BinderItem(0, "", BinderItemType.Root, "Root");
            RecurseXmlNode(Binder, root);
            return root;
        }

        /// <summary>
        /// Parse a node in a Scrivener project file, adding it to
        /// its parent, and then parse the node's children recursively.
        ///
        /// Called from LoadBinderFromScrivener().
        /// </summary>
        /// <param name="node">The XmlElement to parse</param>
        /// <param name="parent">node's parent BinderItem</param>
        private void RecurseXmlNode(IXmlNode node, BinderItem parent)
        {
            if (node.NodeType != NodeType.ElementNode)
                return;

            int id = 0;
            string uuid = string.Empty;
            string created = string.Empty;
            string modified = string.Empty;
            BinderItemType type = BinderItemType.Unknown;
            string title;

            if (node.NodeName.Equals("Binder"))
                for (int i = 0; i < node.ChildNodes.Count; i++)
                    RecurseXmlNode(node.ChildNodes[i], parent);
            else if (node.NodeName.Equals("BinderItem"))
            {
                // Search for the attributes we want on a BindItem.
                foreach (var attr in node.Attributes)
                {
                    switch (attr.NodeName)
                    {
                        case "ID":
                            id = Convert.ToInt32(attr.InnerText);
                            break;
                        case "UUID":
                            uuid = attr.InnerText;
                            break;
                        case "Created":
                            created = attr.InnerText;
                            break;
                        case "Modified":
                            modified = attr.InnerText;
                            break;
                        case "Type":
                            switch (attr.InnerText)
                            {
                                case "Text":
                                    type = BinderItemType.Text;
                                    break;
                                case "Folder":
                                    type = BinderItemType.Folder;
                                    break;
                                case "DraftFolder":
                                    type = BinderItemType.DraftFolder;
                                    break;
                                case "ResearchFolder":
                                    type = BinderItemType.ResearchFolder;
                                    break;
                                case "TrashFolder":
                                    type = BinderItemType.TrashFolder;
                                    break;
                                case "PDF":
                                    type = BinderItemType.Pdf;
                                    break;
                                case "WebArchive":
                                    type = BinderItemType.WebArchive;
                                    break;
                                case "Root":
                                    type = BinderItemType.Root;
                                    break;
                            }
                            break;
                    }
                }
                var titleNode = node.SelectSingleNode("./Title");
                title = titleNode != null ? titleNode.InnerText : string.Empty;
                var children = node.SelectSingleNode("./Children");
                // Process stbuuid
                var metaData = node.SelectSingleNode("./MetaData");
                if (metaData != null)
                {
                    var customMetaData = metaData.SelectSingleNode("./CustomMetaData");
                    XmlElement stbUuidNode = null;
                    if (customMetaData != null)
                        stbUuidNode = (XmlElement)customMetaData.SelectSingleNode("./MetaDataItem[FieldID='stbuuid']");
                    string stbUuid = string.Empty;
                    XmlElement value;
                    if (stbUuidNode != null)
                    {
                        value = (XmlElement) stbUuidNode.SelectSingleNode("./Value");
                        if (value != null) stbUuid = value.InnerText;
                    }
                    var newNode = new BinderItem(id, uuid, type, title, parent, created, modified, stbUuid);
                    newNode.Node = node;
                    if (children != null)
                        foreach (IXmlNode child in children.ChildNodes)
                            if (child.NodeName.Equals("BinderItem"))
                                RecurseXmlNode(child, newNode);
                }
            }
        }

        /// <summary>
        /// Replace an IXmlNode with a newer version.
        /// </summary>
        /// <param name="parentBinderItem">ex, Storybuilder</param>
        /// <param name="newChild">the node to replace with</param>
        /// <param name="referenceChild">the node to be replaced</param>
        public void ReplaceXmlNode(IXmlNode parentBinderItem, IXmlNode newChild, IXmlNode referenceChild)
        {
            IXmlNode parent = parentBinderItem.ParentNode;  // The actual parent
            parent.ReplaceChild(newChild, referenceChild);
        }

        /// <summary>
        /// Create an XmlElement tree or subtree from a BinderItem model.
        ///
        /// This method builds the tree via recursive descent through
        /// the BinderItem model.
        /// </summary>
        /// <param name="rootItem"></param>
        /// <returns></returns>
        public XmlElement CreateFromBinder(BinderItem rootItem)
        {
            XmlElement xmlRootElement = CreateNode(rootItem);
            RecurseBinderNode(rootItem,xmlRootElement);

            return xmlRootElement;
        }

        private void RecurseBinderNode(BinderItem binderNode, IXmlNode xmlParentNode)
        {
            IXmlNode parentChildren = xmlParentNode.SelectSingleNode(".//Children");
            // Traverse and create XmlNodes for the binderNode subtree
            foreach (BinderItem child in binderNode.Children)
            {
                IXmlNode newNode = CreateNode(child);
                if (parentChildren != null)
                    parentChildren.AppendChild(newNode);
                //xmlParentNode.AppendChild(newNode);
                RecurseBinderNode(child, newNode);
            }
        }

        /// <summary>
        /// Create a new XmlElement corresponding to a BinderItem node.
        /// </summary>
        /// <param name="binderItem"></param>
        /// <returns></returns>
        public XmlElement CreateNode(BinderItem binderItem)
        {
            // Create new XmlElement for BinderItem
            XmlElement element = XmlDocument.CreateElement("BinderItem");
            // Set attributes
            element.SetAttribute("UUID", binderItem.Uuid);
            element.SetAttribute("Type", GetType(binderItem.Type));
            DateTime now = DateTime.Now;
            element.SetAttribute("Created", now.ToString("yyyy-MM-dd - HH:mm:ss - K"));
            element.SetAttribute("Modified", now.ToString("yyyy-MM-dd - HH:mm:ss - K"));
            // Add Title child
            XmlElement title = XmlDocument.CreateElement("Title");
            XmlText titleText = XmlDocument.CreateTextNode(binderItem.Title);
            title.AppendChild(titleText);
            element.AppendChild(title);
            // Add MetaData child and IncludeInCompile grandchild
            XmlElement meta = XmlDocument.CreateElement("MetaData");
            XmlElement include = XmlDocument.CreateElement("IncludeInCompile");
            XmlText includeText = XmlDocument.CreateTextNode("Yes");
            include.AppendChild(includeText);
            meta.AppendChild(include);
            element.AppendChild(meta);
            // Add TextSettings child and TextSelection grandchild
            XmlElement textSettings = XmlDocument.CreateElement("TextSettings");
            XmlElement textSelection = XmlDocument.CreateElement("TextSelection");
            XmlText textSelectionText = XmlDocument.CreateTextNode("0,0");
            textSelection.AppendChild(textSelectionText);
            textSettings.AppendChild(textSelection);
            element.AppendChild(textSelection);
            // Note: In Scrivener text nodes can have subdocuments and folders
            // can have their own text.
            XmlElement children = XmlDocument.CreateElement("Children");
            element.AppendChild(children);
            //if (binderItem.Type.Equals((BinderItemType.Folder)))
            //{
            //    XmlElement children = XmlDocument.CreateElement("Children");
            //    element.AppendChild(children);
            //}
            binderItem.Node = element;
            return element;
        }

        /// <summary>
        /// Return a string representation of a BinderItemType enum.
        /// </summary>
        /// <param name="type">The enum to parse</param>
        /// <returns>A string representation of the enum value.</returns>
        private static string GetType(BinderItemType type)
        {
            string value = string.Empty;
            switch (type)
            {
                case BinderItemType.Text:
                    value = "Text";
                    break;
                case BinderItemType.Folder:
                    value = "Folder";
                    break;
                case BinderItemType.DraftFolder:
                    value = "DraftFolder";
                    break;
                case BinderItemType.ResearchFolder:
                    value = "ResearchFolder";
                    break;
                case BinderItemType.TrashFolder:
                    value = "TrashFolder";
                    break;
                case BinderItemType.Pdf:
                    value = "PDF";
                    break;
                case BinderItemType.WebArchive:
                    value = "WebArchive";
                    break;
                case BinderItemType.Root:
                    value = "Root";
                    break;
            }
            return value;
        }

        public async Task<bool> IsScrivenerRelease3()
        {
            string path = Path.Combine(ProjectPath, "Files");
            StorageFolder filesFolder = await StorageFolder.GetFolderFromPathAsync(path);
            IReadOnlyList<StorageFolder> folders = await filesFolder.GetFoldersAsync();
            foreach (StorageFolder folder in folders)
                if (folder.Name.Equals("Data"))
                    return true;    // The project is a Scrivener Release 3 folder
            return false;
        }

        /// <summary>
        /// Get the Scrivener document subfolder for a UUID.
        /// 
        /// If the folder already exists, return it.
        /// If the subfolder doesn't exist, create and return it.
        /// </summary>
        /// <param name="uuid">The UUID of a StoryBuilder StoryElement or list</param>
        /// <returns>StorageFolder</returns>
        public async Task<StorageFolder> GetSubFolder(string uuid)
        {
            string path = Path.Combine(ProjectPath, "Files", "Data");
            StorageFolder parent = await StorageFolder.GetFolderFromPathAsync(path);

            try
            {
                return await parent.GetFolderAsync(uuid);
            }
            catch (Exception)
            {
                return await parent.CreateFolderAsync(uuid);
            }
        }

        public ScrivenerIo()
        {
            XmlDocument = new XmlDocument();
            ProjectPath = string.Empty;
        }

        public async Task<string> ReadRtfText(string path)
        {
            string result = string.Empty;
            await Task.Run(() =>
            {
                //Create an RTF object
                RtfTree tree = new RtfTree();

                //Load an RTF document from a file
                tree.LoadRtfFile(path);

                // Get the file's contents
                return tree.Text;
            });
            return result;
        }

        /// <summary>
        /// Write an Xml file or subtree to disk.
        ///
        /// This routine is intended as a debugging aid. 
        /// </summary>
        public async Task WriteTestFile(string fileName, XmlElement root)
        {
            StorageFolder projectFolder = await StorageFolder.GetFolderFromPathAsync(ProjectPath);
            StorageFile file = await projectFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
            if (root != null)
            {
                XmlDocument doc = new XmlDocument();
                doc.AppendChild(doc.CreateProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\""));
                XmlElement newRoot = doc.CreateElement("Binder");
                doc.AppendChild(newRoot);
                IXmlNode newNode = doc.ImportNode(root, true);
                newRoot.AppendChild(newNode);
                await doc.SaveToFileAsync(file);
            }
        }
    }
}