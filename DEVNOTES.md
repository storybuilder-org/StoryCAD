# Programmer Notes

## General notes

StoryBuilder is written in C# and XAML and is a Windows desktop app. 
It's written using WinUI 3, MVVM, [Project Reunion APIs][2], and .NET5. 
Although the only programming skill you need to get started is some C#,
familiarity with [Windows.Storage IO][5] and [MVVM][6] will be useful. 
The program is maintained as a Visual Studio solution using either
VS2022 or VS2019. You'll need to configure your development environment
as described in [Tools and Setup](#tools-and-setup).

StoryBuilder began as a UWP program and now uses Windows UI
(WinUI 3) [controls][7] and styles. It runs
as a native Windows (Win32) program, but its UWP roots remain: 
it uses [C#/WinRT][3] and [UWP asynchronous IO][4].

## Code Contributions

If you want to clone the repository and hack at it, go right ahead. Feel
free to make any use of the code, consistent with the license, you like.

But to get your changes accepted back into production and distribution, 
we use a more collaboratory approach, based on GitHub branch/merge. 
It lets you play with your changes safely, commit frequently for backup
purposes, and never put the project at risk. It also insures that there's
always an easy way to back changes out, and that multiple
eyes are on any production changes.

Contributions always start from issues (bugs or feature requests)
to maintain some history of why the change was made.

The main steps are:

1. Create a branch off of main
2. Make commits
3. Open a pull request
4. Collaborate through PR comments
5. Make more commits
6. Discuss and review code with a reviewer
7. Deploy for final testing
8. Merge your branch into the main branch

Although it's a complex program, it's orderly: throughout
the code it does similar things in a similar fashion, and it's 
organizing principle is 'Keep it simple, stupid.'  If you're adding 
something to StoryBuilder, there's a chance what you're
adding is similar to something already there: a Page, A Tab,
a Control, or new or changed installation data.

## Getting Started

### Tools and Setup

1. StoryBuilder is installed and updated with Visual Studio. Either VS2019 or VS2022 
will work. The Community editions of these products are free. You can download
from here:
https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio?view=vs-2019

2. StoryBuilder is developed with the Windows App SDK. Set up your development as per this guide:
https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/set-up-your-development-environment?tabs=experimental

You can test your installation by building and running a new app using the Blank App, Packaged (WinUI 3 in Deskto
template as described in the document.

2. In Visual Studio, clone the Storybuilder repository from the GitHub repository at 
https://github.com/terrycox/StoryBuilder-2.git

3. Build and run the solution.

4. Click  the File (document icon) button on the menu and open the HELLO sample to verify.

### Build

Building and debugging can be done in your branch with impunity.

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

## Developer Tips

### Solution and Project Stucture

The StoryBuilder solution contains the following projects:

#### CreateInstallManifest

This .NET5 console application reads the contents of the StoryBuilder 
application's \Assets\Install folder and it's child folders and 
produces a text document containing each file's relative path names 
and a SHA256 hash of its contents. The list is written into the 
same \Assets\Install folder as 'install.manifest'. When StoryBuilder is launched, install.manifest is read and compared
to the contents of install.manifest saved in the installation folder. Any 
files whose hashes are different are updated. Whenever you change or add 
content to \Assets\Install, set CreateInstallManifest as the startup 
project and run it.

#### NRtfTree

NRtfTree Library is a set of classes written in C# that may be used to 
manage RTF documents. StoryBuilder uses the library in its Scrivener 
reports interface. It's a .NET 5 DLL project.
 
NRtfTree is licensed under The GNU Lesser General Public License (LGPLv3).

#### StoryBuilder

StoryBUilder is a WinUI 3 Win32 application which was orignally a UWP
app using WinUI 3 XAML controls exclusively. It was originally
written as a UWP app and uses async and StorageFile IO 
exclusively. It contains the App startup logic and all views
for the running application except dialogs.

The primary Page and home screen is Shell.xaml

#### StoryBuilder (Package)

This project is the MSIX packaging project for StoryBuilder. 
It's the normal startup project for the solution.

#### StoryBuilderLib

This .NET5 DLL contains the non-IO code for the solution. 
 The DLL contains the following folders:

**Controls**    UserControls

**Converters**    XAML Value Converters

**DAL**         Data Access Layer

**Models**      StoryBuilder uses the Windows Community Toolkit
MVVM Library. Each Story Element (node in the Shell Treeview)
is a Model instance, a class derived from StoryElement. StoryElement
in turn is an ObservableObject.

**Services**      A collection of microservices, each of which
is callable (usually from a ViewModel.)

**ViewModels**    WCT MVVM's ViewModels. Each View (Page)
and most dialogs use a ViewModel as both a target for
XAML bindings and a collection point for View-oriented logic.

#### StoryBuilderTest 
This .NET5 Console application is a collection of MSTest 
unit test classes. 

The tests can be executed by setting StoryBuilderTests
as the startup project and running Test Explorer. 

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

### Creating or Modifying a Tool

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
[2]:https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/
[3]:https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/
[4]:https://docs.microsoft.com/en-us/windows/uwp/threading-async/asynchronous-programming-universal-windows-platform-apps
[5]:https://docs.microsoft.com/en-us/uwp/api/windows.storage?view=winrt-22000
[6]:https://docs.microsoft.com/en-us/windows/communitytoolkit/mvvm/introduction
[7]:https://www.microsoft.com/en-us/p/winui-3-controls-gallery/9p3jfpwwdzrc?activetab=pivot:overviewtab