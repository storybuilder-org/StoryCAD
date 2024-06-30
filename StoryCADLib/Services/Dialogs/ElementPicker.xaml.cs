﻿using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Models;
using StoryCAD.ViewModels;

namespace StoryCAD.Services.Dialogs;

/// <summary>
/// A simple picker allowing a user to pick the
/// type of element they want
/// </summary>
public sealed partial class ElementPicker : Page
{
	private ElementPickerVM PickerVM = Ioc.Default.GetRequiredService<ElementPickerVM>();

	/// <summary>
	/// Don't spawn this on its own, use ElementPickerVM.ShowPicker()
	/// </summary>
	public ElementPicker() { InitializeComponent(); }

	/// <summary>
	/// This just handles the UI when the type Combobox is changed
	/// </summary>
	private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		//Get elements
		ComboBoxItem Type = PickerVM.SelectedType as ComboBoxItem;
		StoryItemType type = Enum.Parse<StoryItemType>(Type.Content.ToString()!, 
			true);

		//Reset the UIs, so we can't enter an invalid state
		PickerVM.SelectedElement = null;
		ElementBox.ItemsSource = null;
		NewButton.IsEnabled = true;

		var elements = ShellViewModel.GetModel().StoryElements
			.Where(element => element.Type == type);
		if (elements.Count() == 0) // no elements so disable picker
		{
			ElementBox.IsEnabled = false;
		}
		else
		{
			//Update element box
			ElementBox.IsEnabled = true; //Enable elements box
			ElementBox.ItemsSource = elements;
		}
	}
}