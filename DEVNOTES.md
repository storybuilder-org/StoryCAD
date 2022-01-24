# Programmer Notes

## General notes

StoryBuilder is written in C# and XAML and is a Windows desktop app. 
It's written using [WinUI 3][2], [MVVM][6], [Project Reunion APIs][3], and [.NET6][8]. 
Although the only programming skill you need to get started is some C#,
familiarity with [Windows.Storage IO][5] and [MVVM][6] will be useful. 
The program is maintained as a Visual Studio solution using either
VS2022 or VS2019.

StoryBuilder began as a UWP program and now uses Windows UI
(WinUI 3) [controls][7] and styles. It runs
as a native Windows (Win32) program, but its UWP roots remain: 
it uses [C#/WinRT][3] and [UWP asynchronous IO][4].

## Code Contributions

If you want to clone the repository and hack at it, go right ahead. Feel
free to make any use of the code, consistent with our license and the 
licenses of the packages StoryBuilder is built with, you like.

Be aware that two of the  
careful: two of the 

But to get your changes accepted back into production and distribution, 
we use a more collaboratory approach, based on GitHub branch/merge. 
It lets you play with your changes safely, commit frequently for backup
purposes, and never put the project at risk. It also insures that there's
always an easy way to back changes out, and that multiple
eyes are on any production changes.

Contributions always start from issues (bugs or feature requests)
in order to maintain a history of why the change was made.

### Coding Worflow

1. Create a branch off of master.
2. Code and make commits.
3. Open a pull request. We recommend doing this early.
4. Collaborate through PR comments.
5. Make more commits.
6. Discuss and review code with a reviewer.
7. Deploy for final testing.
8. Merge your branch into the main branch

Although StoryBuilder is a complex program, it's orderly: throughout
the code it does similar things in a similar fashion, and it's 
organizing principle is 'Keep it simple, stupid.'  If you're adding 
something to StoryBuilder, there's a chance what you're
adding is similar to something already there: a Page, A Tab,
a Control, or new or changed installation data.

## Getting Started

### Tools and Setup

You'll need to configure your development environment
as described in the **Installation** section of [README.md][9].

We strongly recommend building and running the [HELLO sample][10]
to verify your installation before proceeding.

### Build

Always work in a branch. Building and debugging can be done in your branch with impunity.

### Test

The StoryBuilderTest project is an MSTEST console application that accesses and runs
scripted unit tests aginst StoryBuilderLib's back-end code and viewmodels. 

There is at present no direct UI testing, but all unit tests will be ran to success
before each merge to the main branch.

Developers are urged to add test cases for their contributed changes. Tests can be
added and ran from your branch while you're developing. It's recommended that you
run the full set of tests to check for side effects of the new code.


### Side-Loading the App (Beta testers)

At present there's no CI/CD pipeline for StoryBuilder. After a branch is coded, tested, the PR 
is generated and reviewed and merged to master, the master bdranch is built with the changes and
it's 'implemented' by performing the following steps:

1. Running the StoryBuilderTest unit tests as a final smoke test (see the next topic.)

2. Publishing the app bundle from the StoryBuilder (Package) project.

3. Zipping the app bundle along with the user's README.TXT file, which contains
instructions for side-loading on hir or her remote machine.

These steps are a part of release management and will be done by an administrator.


## Solution and Project Stucture

The StoryBuilder solution contains the following projects:

### NRtfTree

NRtfTree Library is a set of classes written in C# that may be used to 
manage RTF documents. StoryBuilder uses the library in its Scrivener 
reports interface. It's a .NET 6 DLL project.
 
NRtfTree is licensed under The GNU Lesser General Public License (LGPLv3).

### StoryBuilder

StoryBUilder is a WinUI 3 Win32 application which was orignally a UWP
app. It uses WinUI 3 XAML controls exclusively. It uses StorageFile
(async) IO exclusively This projct contains the App startup logic and all 
control layout (views). All views are declarative (XAML), ecept dialogs.

The primary Page and home screen is **Shell.xaml**.

This project also contains the inline MSIX packaging for StoryBuilder. 

StoryBuilder the normal startup project for the solution. Program 
initializaton is in **App.Xaml.cs**.

### StoryBuilderLib

This .NET 6 DLL contains the non-IO code for the solution. 
The DLL contains the following folders:

**Assets**      The Install subfolder holds runtime data.

**Controls**    UserControls

**Converters**  XAML Value Converters

**DAL**         Data Access Layer

**Models**      StoryBuilder uses the Windows Community Toolkit
MVVM Library. Each Story Element ( which is also anode in the **Shell** Treeview)
is a Model class derived from StoryElement (for example, CharacterModel or
SceneModel). 

**Services**      A collection of microservices, each of which
is callable (usually from a ViewModel.)

**ViewModels**    [Windows Community Toolkit MVVM ViewModels][6]. Each View (Page)
and most dialogs use a ViewModel as both a target for
XAML bindings and a collection point for View-oriented logic. 

### StoryBuilderTest 

This .NET 6 Console application is a collection of MSTest 
unit test classes. 

The tests can be executed by setting StoryBuilderTests
as the startup project and running Test Explorer. 

## Developer Tips

### Adding a New Control

#### Update Page layout to add the new control. 
Add a corresponding property to the Page's ViewModel. 
Add a 2-way binding from the Page control's Text or SelectedItem to the ViewModel    property.
Initialize the property in the ViewModel's constructor.
If the control is a ComboBox or other control that uses an ItemsSource,  you
also need to add a 1-way binding from the page to that list in the ViewModel,
and to provide a source for the list in the ViewModel. The source will usually

be a list in Controls.ini, which is in the \Assets\Install folder. Use an existing
control as an example. Note that the list must be in the form of key/value pairs.
Test this much and very the layout looks okay. Insure that it's responsive 
by resizing the page up and down and checking the layout.

#### Add the corresponding property to the Model. 
Name it identically to the ViewModel's property.
Initialize the property in each of the Model'sconstructors. 
Update the ViewModel's LoadModel method to assign the ViewModel's property
from the Model when the ViewModel is activated (navigated to- see BindablePage).
If the property is a RichEditBox, call StoryReader.GetRtfText instead using a
simple assignment statement (see other rtf fields for an example.)
Update the ViewModel's SaveModel method to assign the Model's property from
the ViewModel when the ViewModel is deactivated (navigated from.) If the 
property is a RichEditBox, call StoryWriter.PutRtfText instead of a simple assignment.
Test that changes to the field persist when you navigate from one StoryElement to
another in the TreeView.

#### Add code to StoryReader to read the Model property from the .stbx file:
   Update the appropriate StoryElement's parse method (called from RecurseStoryElement).
   These methods are case statements to find the property's named attribute in the xml
   node and move its inner text to the Model's property.

#### Add code to StoryWriter to write the Model property to the .stbx file.
   The appropriate method will named 'ParseXElement', ex., ParseSettingElement. 
   Use an existing property as a template.
   Create a new XmlAttribute.
   If the property is a RichEditBox, you must next set the Model's property by calling
   PutRtfText.
   Assign the attribute with the property's value.
   Add the XmlAttribute to the current XmlNode.
   Test by using the new property, saving the story outline, re-opening the story project,
   and verifying that the data entry from the new control is present and correct.

### Dialogs

Interactions with the user are generally done through popup ContentDialogs, which may be 
inline code if small (such as verification requests) or defined in XAML if more complicated.
The XAML is found in StoryBuilderLib in the \Services\Dialogs\ folder. An example is
NewProjectPage, displayed when the user wants to create a new story outline.

Dialogs, like the Shell's main pages, use data binding to a ViewModel (found in StoryBuilderLib
in the ViewModels folder). An example is NewProjectViewModel.

### Creating or Modifying a Tool

A tool is a device to facilitate work, and writing a story is work. StoryBuilder
contains a rich set of tools to assist in outlining. Tools in StoryBuilder

#### Define the tool's dialog layout
Tools are usually pop-ups and are defined as a ContentDialog. The XAML is found in 
StoryBuilderLib in the \Services\Dialogs\Tools folder.

#### Define the tool's ViewModel
All but the simplest tools should use a ViewModel to hold code interactions with the tool
and between the tool and the Page it's invoked from. The ContentDialog code-behind can link
the view and the viewmodel.

#### Define and populate the tool's data
StoryBuilder's tools generally provide data to aid in story or character definition or the 
plotting process. Typically this is reference (read only). Although the data can come from 
any source, such as a web service, much of it will reside in the StoryBuilder 
project's \Assets\Install\Tools.ini file. 

#### Create the model
If the tool creates or modifies data on Story Elements, as is typical,
create an in-memory model of the data in the StoryBuilderLib project's \Models\Tools folder. 
These are Plain Old CLR Object (POCO) classes. T

#### Read the data
Provide a mechanism to read the data  and populate the model. Data in Tools.ini is loaded in 
StoryBuilderLib \DAL\ToolLoader.cs, which is called from LoadTools() in the StoryBuilder 
project's App.Xaml.cs. 

Each tool will generally have its own data layout, and ToolLoader.cs consists of a series of 
methods which load an individual tool's data. If you're accessing data from a different source, 
such as a web service, you'll probably add the service code under the StoryBuilderLib 
project's \Services folder, but it should still be called from LoadTools(). 

#### Create the ViewModel

StoryBuilder uses MVVM for tools as well as regular Page views. We use the Windows Community Toolkit's MVVM library, which is installed as a NuGet package. The ViewModel class must contain a using statement for Microsoft.Toolkit.Mvvm.ComponentModel and derive from ObservableRecipient.

#### Create the View (Dialog)

###The views are generally dialogs and their XAML and 
code-behind are in StoryBuilderLib's \Services\Dialogs 
folder. The dialogs should, like Page views, use 
responsive (self-resizing) layouts.

[1]:https://github.com/terrycox/StoryBuilder-2/blob/master/docs/SOLUTION_PIC.bmp   
[2]:https://docs.microsoft.com/en-us/windows/apps/winui/winui3/
[3]:https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/
[4]:https://docs.microsoft.com/en-us/windows/uwp/threading-async/asynchronous-programming-universal-windows-platform-apps
[5]:https://docs.microsoft.com/en-us/uwp/api/windows.storage?view=winrt-22000
[6]:https://docs.microsoft.com/en-us/windows/communitytoolkit/mvvm/introduction
[7]:https://www.microsoft.com/en-us/p/winui-3-controls-gallery/9p3jfpwwdzrc?activetab=pivot:overviewtab
[8]:https://docs.microsoft.com/en-us/dotnet/api/?view=net-6.0
[9]:https://github.com/storybuilder-org/StoryBuilder-2/blob/master/README.md
[10]:https://docs.microsoft.com/en-us/windows/apps/winui/winui3/create-your-first-winui3-app?pivots=winui3-packaged-csharp