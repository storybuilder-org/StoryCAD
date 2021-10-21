using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Services.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NewRelationshipDialog : ContentDialog
    {
        public NewRelationshipViewModel NewRelVM;

        #region public Properties

        public StoryElementCollection StoryElements;
        public StoryElement Member { get; set; }

        public ObservableCollection<StoryElement> ProspectivePartners;

        public ObservableCollection<RelationshipModel> Relationships;

        public StoryElement SelectedPartner { get; set; }

        public ObservableCollection<RelationType> RelationTypes;

        public RelationType RelationType;

        #endregion  
        public NewRelationshipDialog()
        {
            InitializeComponent();
            RelationTypes = new ObservableCollection<RelationType>();
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
