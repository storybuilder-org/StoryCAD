# StoryBuilder Roadmap

## Current Release

As of July 29, 2022, we've olled out Release 2.1.0.0. This is the 
completion of a major milestone, the distribution of StoryBuilder 
via Windows Store, and is described in a Project:
https://github.com/orgs/storybuilder-org/projects/3

Windows Store client installations are currently allowed for any 
Windows user who has a direct link to the download. We're providing the
link through the website (https://storybuilder.org) and other channels,
as a broader public beta. As a reminder, StoryBuilder is a Free and Open
Source (FOSS) product.

## Next Release

Our next Release will be 2.2.0.0 and should be deployed on or abiyt August 31st. 

We anticipate that this next release will include the following features:

### Create a 'copy to narrative' tool (#23)

A user can switch between two views when working in StoryBuilder, 
'Story Explorer View' and 'Story Narrator View', and can switch 
back and forth between them using a 'current view' dropdown on the 
Shell status bar. Story Explorer shows every story element the user 
has created, in whatever order he likes. The Narrator's view, on the 
other hand, shows just Scene (Plot Point) and Section story elements. 
Sections are groupings of scenes such as chapters in a book or 
acts in a play- folders, in other words. Story Narrative View 
displays the story as it will look being written or told- the narrative. 
Every scene Story Narrator View contains is and will remain 
in Story Explorer View, which is where it's created; you can't 
create scenes in the Narrator. In other words, a scene in Story 
Narrator is a link to that scene in Story Explorer.

Today, the only way to add a scene to the Narrator is to use the 
right-click flyout on the Navigation pane.

Create a 'Copy to Narrative' tool which opens a dialog. The 
dialog should display two TreeViews side by side. The left side 
will contain the Story Explorer View, and the right side, 
the Story Narrator View. Between the two TreeView controls, 
four buttons will allow a scene to be copied to or from the Story 
Narrator View and moved up and down in the Story Narrator View.
In other words, this is a tool to create or update the Story Narrator 
view in bulk.

#### Status:

In process.

### Update to WinAppSDK 1.1.3 (#376)

Maintenance to update StoryBuilder dependencies.

#### Status:

Not started.

Other NuGet package dependencies also need reviewed.

### 'Creating a Story' tutorial in Help is out of date (#349)

The Tutorial in the StoryBuilder User Manual is from the old (V1) StoryBuilder,
and needs revised / rewritten.

Not only the screenshots, but the actual story design process, need revised
to account for the many changes in StoryBuilder V2.

#### Status:

In progress.

## Program demo videos (#145)

A short (two minutes) 'StoryBuilder Concepts'
This will describe what StoryBuilder is, why an outline, how outlines are grown, how to edit and modify them,
and reports and exporting.

A longer (ten minutes) 'Introduction to StoryBuilder'.
This will contain everything you need to know to get started.
We're working on two YouTube videos:

#### Status:

In process. We also need a minor modification to our website to link to
these videos.

## Ongoing Issues

### Drag and Drop is buggy (#312)

Dragging and dropping isn't limited, the user can do a lot of things, such as moving stuff to the
trashcan, which shouldnt be allowed.

#### Status:

This has been reported by us to Microsoft as a WinUI 3 issue:
https://github.com/microsoft/microsoft-ui-xaml/issues/7266

It's one of a series of possibly related issues:
https://github.com/microsoft/microsoft-ui-xaml/issues/7002
https://github.com/microsoft/microsoft-ui-xaml/issues/7007
https://github.com/microsoft/microsoft-ui-xaml/issues/7231

In the meantime, we recommend being very careful when using Drag-and-Drop
to rearrange the Navigation Tree nodes (this is the only Drag-and-Drop
StoryBuilder currently supports.)

### ARM64 is broken (#108)

ARM devices are the way forward for more affordable comptuter systems, 
and Microsoft permits compiles to ARM64 as well as Intel x86 and x64
architectures.  We will build for all three device types.

#### Status: 

We've been working on this since January 2022 and aquired a Samsung
tablet to test with. We've made some progress, but don't have a 
functional StoryBuilder deployment that works on ARM64 devices.

We've recently acquired a second test computer and are working on this as
resources permit.

### Implement a Print Manager for printed reports (#157)

The ability to select a printer and its options is essential for 
StoryBuilder print reports.

A Print Manager dialog should be part of the Print Report menu. 
The current mechanism, printing to the default printer, is a work-around but not a long-term solution.

#### Status:

This is a reported WInUI issue:
https://github.com/microsoft/CsWinRT/issues/968#issuecomment-918923141

It's been highly requested on the Windows SDK API product board:
https://portal.productboard.com/winappsdk/1-windows-app-sdk/c/50-support-printmanager-api
It looks like this may be available in the API 1.2.

We'll implment this as soon as the API supports it. In the meantime,
use the workaround, which is to set the default printer to the printer
you'd like to print StoryBuilder reports on. This can include PDFs.

## Selected Future Updates 

### Picture pickers for characters and settings (#53)

The saying "A picture is worth a thousand words" may have been invented 
to describe this proposed feature. Provide a
mechanism to select images from the web and browse them (perhaps 
similar to XAML Control Gallery's FlipView.) The user
can browse and find images (for example, of actors) that represent 
his or her concept of the character's appearance. This could 
replace (or better, supplement) the RichEditBox.

If this is useful for Character appearance, why not for Setting as well?

#### Status:

Not started.

### Launch StoryBuilder from .stbx file (#94)

A user should be able to launch StoryBuilder by clicking on a .stbx file.

The Windows App SDK Rich Activation API includes this capability:
https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-rich-activation

#### Status:

Not started.

### Alternative use of Hamburger button for small screens (#25)

#### Status

### Unit testing / additional unit tests (#17)
Run all StoryBuilderTest scripts from Test Explorer as a part of 
PR review prior to and after merge. Code new scripts or modify old 
ones as a routine part of the development process. If not for 
red/green/red testing, consider it a documentation task.

As time permits, tests need to be generated to fill in code coverage, 
especially for user interactions.

#### Status:

We have an MSTest project added to our solution, and a small number
of tests, dating back to the UWP version of StoryBuilder. The MSTest 
interface didn't work with WinUI 3 for quite a while and xUnit testing
languished. We need to restart this and catch up.

We in particular need a series of smoke tests for high-level 
user interactions, such as creating a new outline, opening and 
modifying an existing outline, printing a report, etc. These can be 
based on file comparisons from program output to previously ran 
and verified program output. Smoke tests are vital for the purpose
of CI in automating PR reviews.