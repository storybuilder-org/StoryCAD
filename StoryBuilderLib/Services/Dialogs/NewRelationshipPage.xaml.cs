using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Services.Dialogs
{
    public sealed partial class NewRelationshipPage : Page
    {

        public NewRelationshipViewModel NewRelVM;

        #region public Properties

        public StoryElementCollection StoryElements;
        public StoryElement Member { get; set; }

        public ObservableCollection<StoryElement> ProspectivePartners;

        public ObservableCollection<RelationshipModel> Relationships;

        public StoryElement SelectedPartner { get; set; }

        public ObservableCollection<RelationType> RelationTypes;

        #endregion  
        public NewRelationshipPage(NewRelationshipViewModel vm)
        {
            InitializeComponent();
            RelationTypes = new ObservableCollection<RelationType>();
            NewRelVM = vm;
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }

        [ComImport]
        [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IInitializeWithWindow
        {
            void Initialize(IntPtr hwnd);
        }

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = true, SetLastError = false)]
        public static extern IntPtr GetActiveWindow();
    }

}