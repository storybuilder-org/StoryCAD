---
title: Developer Notes
layout: default
nav_enabled: true
nav_order: 117
parent: For Developers
has_toc: false
---

# Programmer Notes

## General notes

StoryCAD is written in C# and XAML and is a Windows desktop app. 
It's written using [WinUI 3][2], [MVVM][6], [Project Reunion APIs][3], and [.NET][8]. 
Although the only programming skill you need to get started is some C#, familiarity with [asynchronous IO][5] and [MVVM][6] will be useful. 
We maintain StoryCAD as a Visual Studio solution using Visual Studio 2022.
StoryCAD began as a UWP program and now uses Windows UI (WinUI 3) [controls][7] and styles. It runs
as a native Windows (Win32) program, but its UWP roots remain; it uses [UWP asynchronous IO][4].
This allows StoryCAD outlines, which are JSON files, to be stored locally or on cloud storage services like OneDrive, Dropbox, Google Drive or Box.


## Installation and Setup

We maintain the StoryCAD repository with Visual Studio 2022. The Community edition of 
Visual Studio is free. You can find Visual Studio 2022 [here][11].

StoryCAD uses the [Windows App SDK][2]. Set up your development as per [this guide][12]. 

We strongly recommend building and running the [HELLO sample][10]
to verify your installation before proceeding.

In Visual Studio, clone the StoryCAD repository from the GitHub repository 
at https://github.com/StoryBuilder-org/StoryCAD.git and build and run the solution.


## Code Contributions

If you want to hack at StoryCAD, go right ahead. Make any use of the code you like, consistent with our 
license and the licenses of the packages StoryCAD is built with.

To get changes accepted into production and distribution, we use an approach based on GitHub branch/merge. 
It lets you play with your changes safely, commit frequently for backup, and never put the project at risk. It ensures that 
there's always an easy way to back changes out, and that multiple eyes are on any production changes.

Contributions should always start from issues (bugs or feature requests) in order to maintain a history of why the change was made.

### Coding Workflow

1. Create a branch/fork based on main.
2. Code and make commits.
3. Open a pull request. We recommend doing this early.
4. Collaborate through PR comments.
5. Make more commits.
6. Discuss and review code with a reviewer.
7. Deploy for final testing.
8. Merge your branch into the main branch

Although StoryCAD is a complex program, we try to keep it orderly: throughout
the code, it does similar things similarly, and our organizing principle is KISS (Keep it simple stupid.) If you're adding something to StoryCAD it could be like something already there: a Page, A Tab, a control, or new or changed installation data. If so, borrow that code.

### Build

Always work in a branch. You can build and debug in your branch with impunity.

### Test

The StoryCADTests project is a MSTEST console application that accesses and runs scripted unit tests against StoryCADLib's back-end code and ViewModels. 
We urge developers to add test cases for their contributed changes. You can add and run unit tests from your branch while you're developing. It's recommended that you run the full set of tests to check for side effects of the new code.
StoryCAD uses a Continuous Integration pipe line.  A Pull Request merge (after review) performs the following steps:
1. Running the StoryCADTest unit tests as a final smoke test. If the tests fail, the merge fails.
2. Publishing the app bundle from the StoryCAD (Package) project.
3. Incrementing the release number.
4. Zipping the app bundle along with the user's README.TXT file, which contains
instructions for side-loading on a remote machine.

## StoryCAD Solution Structure


### StoryCAD

StoryCAD is a WinUI 3 application which was originally a UWP
App. It uses WinUI 3 XAML controls only. It uses UWP’s StorageFile
(async) IO. This project contains the App startup logic and all 
control layout (views). All views are declarative (XAML), except dialogs.

The primary Page and home screen is **Shell.xaml**.

This project also contains the inline MSIX packaging for StoryCAD. 

StoryCAD the normal startup project for the solution. Program 
initializaton is in **App.Xaml.cs**.

### StoryCADLib

This .NET library contains the non-IO code for the solution. 
The library contains the following folders:

**Assets**      The Install sub-folder holds runtime data.

**Controls**    UserControls

**Converters**  XAML Value Converters

**DAL**         Data Access Layer

**Models**      StoryCAD uses the Windows Community Toolkit
MVVM Library. Each Story Element ( which is also anode in the **Shell** Treeview)
is a Model class derived from StoryElement (for example, CharacterModel or
SceneModel). 

**Services**      A collection of microservices, each of which is callable (usually from a ViewModel.)

**ViewModels**    [Windows Community Toolkit MVVM ViewModels][6]. Each View (Page)
and most dialogs use a ViewModel as both a target for
XAML bindings and a collection point for View-oriented logic. 

### StoryCADTests 

This .NET Console application is a collection of MSTest 
unit test classes. 

You run the tests by setting StoryCADTests as the startup project and running Test Explorer. 

## Developer Tips

### Adding a New Control

#### Update Page layout to add the new control. 
Add a corresponding property to the Page's ViewModel. 
Add a 2-way binding from the Page control's Text or SelectedItem to the ViewModel property.
Initialize the property in the ViewModel's constructor.
If the control is a ComboBox or other control that uses an ItemsSource,  you
also need to add a 1-way binding from the page to that list in the ViewModel,
and to provide a source for the list in the ViewModel. The source will usually be a list in Controls.ini, which is in the \Assets\Install folder. Use an existing control as a prototype. Note that the Controls.ini lists are key/value pairs.
Test this much and verify that the layout looks okay. Ensure that it's responsive 
by resizing the page up and down and checking the layout.

#### Add the corresponding property to the Model. 
Name it identically to the ViewModel's property.
Initialize the property in each of the Model'sconstructors. 
Update the ViewModel's LoadModel method to assign the ViewModel's property
from the Model when the ViewModel is activated (navigated to- see BindablePage).
If the property is a RichEditBox, call StoryReader.GetRtfText instead using a
simple assignment statement (see other Rtf fields for an example.)
Update the ViewModel's SaveModel method to assign the Model's property from
the ViewModel when the ViewModel is deactivated (navigated from.) If the 
property is a RichEditBox, call StoryWriter.PutRtfText instead of a simple assignment.
Test that changes to the field persist when you navigate from one StoryElement to
another in the TreeView.


### Dialogs

Interactions with the user are generally done through popup ContentDialogs, which may be 
inline code if small (such as verification requests) or defined in XAML if more complicated.
The XAML is found in StoryCADLib in the \Services\Dialogs\ folder. An example is
NewProjectPage, displayed when the user wants to create a new story outline.

Dialogs, like the Shell's main pages, use data binding to a ViewModel (found in StoryCADLib
in the ViewModels folder). An example is NewProjectViewModel.

### Creating or Modifying a Tool

A tool is a device to facilitate work, and writing a story is work. StoryCAD
contains a rich set of tools to assist in outlining. Tools in StoryCAD

#### Define the tool's dialog layout
Tools are usually pop-ups and are defined as a ContentDialog. The XAML is found in 
StoryCADLib in the \Services\Dialogs\Tools folder.

#### Define the tool's ViewModel
All but the simplest tools should use a ViewModel to hold code interactions with the tool
and between the tool and the Page it's invoked from. The ContentDialog code-behind can link
the view and the viewmodel.

#### Define and populate the tool's data
StoryCAD's tools provide data to aid in story or character definition or the plotting process. Typically, this is reference (read only). Although the data can come from any source, such as a web service, much of it will reside in the StoryCAD project's \Assets\Install\Tools.ini file. 

#### Create the model
If the tool creates or changes data on Story Elements, as is typical,
create an in-memory model of the data in the StoryCADLib project's \Models\Tools folder. 
These are Plain Old CLR Object (POCO) classes. T

#### Read the data
Provide a mechanism to read the data  and populate the model. Data in Tools.ini is loaded in 
StoryCADLib \DAL\ToolLoader.cs, which is called from LoadTools() in the StoryCAD 
project's App.Xaml.cs. 

Each tool will have its own data layout, and ToolLoader.cs consists of a series of methods which load an individual tool's data. If you're accessing data from a different source, such as a web service, you'll probably add the service code under the StoryCADLib project's \Services folder, but it should still be called from LoadTools(). 

#### Create the ViewModel

StoryCAD uses MVVM for tools and regular Page views. We use the Windows Community Toolkit's MVVM library, which is installed as a NuGet package. The ViewModel class must contain a using statement for Microsoft.Toolkit.Mvvm.ComponentModel and derive from ObservableRecipient.

#### Create the View (Dialog)

###The views are dialogs and their XAML and 
code-behind are in StoryCADLib's \Services\Dialogs 
folder. The dialogs should, like Page views, use 
responsive (self-resizing) layouts.

[1]:https://github.com/StoryBuilder-org/StoryCAD/blob/master/docs/SOLUTION_PIC.bmp   
[2]:https://docs.microsoft.com/en-us/windows/apps/winui/winui3/
[3]:https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/
[4]:https://docs.microsoft.com/en-us/windows/uwp/threading-async/asynchronous-programming-universal-windows-platform-apps
[5]:https://docs.microsoft.com/en-us/uwp/api/windows.storage?view=winrt-22000
[6]:https://docs.microsoft.com/en-us/windows/communitytoolkit/mvvm/introduction
[7]:https://www.microsoft.com/en-us/p/winui-3-controls-gallery/9p3jfpwwdzrc?activetab=pivot:overviewtab
[8]:https://docs.microsoft.com/en-us/dotnet/api/?view=net-7.0
[9]:https://github.com/StoryBuilder-org/StoryCAD/blob/master/README.md
[10]:https://docs.microsoft.com/en-us/windows/apps/winui/winui3/create-your-first-winui3-app?pivots=winui3-packaged-csharp
[11]:https://visualstudio.microsoft.com/vs/
[12]:https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/set-up-your-development-environment?tabs=cs-vs-community%2Ccpp-vs-community%2Cvs-2022-17-1-a%2Cvs-2022-17-1-b
