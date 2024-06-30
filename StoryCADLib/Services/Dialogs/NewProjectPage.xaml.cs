﻿using System.Runtime.InteropServices;
using Windows.Storage;
using Windows.Storage.Pickers;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Models;
using StoryCAD.ViewModels;
using WinRT;
namespace StoryCAD.Services.Dialogs;

public sealed partial class NewProjectPage : Page
{
	public NewProjectPage(UnifiedVM vm)
	{
		InitializeComponent();
		UnifiedVM = vm;
	}

	public UnifiedVM UnifiedVM;

	public bool BrowseButtonClicked { get; set; }
	public bool ProjectFolderExists { get; set; }
	public StorageFolder ParentFolder { get; set; }
	public string ParentFolderPath { get; set; }
	public string ProjectFolderPath { get; set; }

	private async void Browse_Click(object sender, RoutedEventArgs e)
	{
		// Find a home for the new project
		StorageFolder folder = await Ioc.Default.GetRequiredService<Windowing>().ShowFolderPicker();
		if (folder != null)
		{
			ParentFolderPath = folder.Path;
			UnifiedVM.ProjectPath = folder.Path;
		}
	}
}