# StoryBuilder Roadmap

## Current Release

As of July 29, 2022, we've rolled out Release 2.1.0.0. This is the 
completion of a major milestone, distributing StoryBuilder 
via Windows Store direct link. 

This release has a ton of fixes, adds our privacy policy, and contains documentation improvements.
A point release, 2.1.1.0, on August 1, 2022, fixed a scaling issue we missed in 2.1.0.0. 
We allow Windows Store client installations for any 
Windows user who has a link to the download, from a
link through the website (https://storybuilder.org) and other channels. 

## Next Release

Our next Release will be 2.2.0.0, planned for around August 31st. 

We expect that this next release will include the following features:

### Create a 'copy to narrative' tool (#23)

A user can switch between two views when working in StoryBuilder, 
'Story Explorer View' and 'Story Narrator View', and can switch 
back and forth between them using a 'current view' dropdown on the 
Shell status bar. Story Explorer shows every story element the user 
has created, in whatever order he likes. The Narrator's view, on the 
other hand, shows just Scene (Plot Point) and Section story elements. 
Sections are groupings of scenes such as chapters in a book or 
acts in a play- folders. Story Narrative View 
displays the story in narrative order.
Every scene Story Narrator View contains is and will remain 
in Story Explorer View, which is where it's created; you can't 
create scenes in the Narrator, becausee a scene in Story 
Narrator is just a link to that scene in Story Explorer.

Today, the only way to add a scene to the Narrator is to use the 
right-click flyout on the Navigation pane (the left-hand side of the Shell.)

We want to create a 'Copy to Narrative' tool which opens a dialog that displays two TreeViews, side by side. The left side 
will contain the Story Explorer View, and the right side, 
the Story Narrator View. Between the two TreeView controls, 
buttons will allow a scene to be copied to or removed from the Story 
Narrator View, and moved up and down in the Story Narrator View.
This is a tool to create or update the Story Narrator 
view in bulk.

#### Status:

In Progress.

### Update to WinAppSDK 1.1.3 (#376)

Maintenance to update StoryBuilder dependencies.

#### Status:

DONE, in the point release (2.1.1.0.)

Other NuGet package dependencies were also updated in 2.1.1.0.

### 'Creating a Story' tutorial in Help is out of date (#349)

The Tutorial in the StoryBuilder User Manual is from the old (V1) StoryBuilder,
and needs revised / rewritten.

Not only the screenshots, but the actual story design process, need revised
to account for the many changes in StoryBuilder V2.

#### Status:

In progress.

### Program demo videos (#145)

A short (two minutes) 'StoryBuilder Concepts'
This will describe what StoryBuilder is, why outline,  and how to create, develop and edit outlines.

A longer (ten minutes) 'Introduction to StoryBuilder'.
This will contain everything you need to know to get started.

#### Status:
In process. We also need a minor modification to our website to link to
these videos.

### Windows Store

We'll open StoryBuilder up for full discovery and download on the Store.

Status:

We're waiting to see how stable the current release is. If we encounter
any significant issues, we'll fix them or defer the full Store distribution
to a later release.

## Ongoing Issues

### Drag and Drop is buggy (#312)

Dragging and dropping, the user can do a lot of inappropriate things, such as moving stuff to the trashcan, which s

#### Status:

This has been reported by us to Microsoft as a WinUI 3 issue:
https://github.com/microsoft/microsoft-ui-xaml/issues/7266

It's one of a series of related issues:
https://github.com/microsoft/microsoft-ui-xaml/issues/7002
https://github.com/microsoft/microsoft-ui-xaml/issues/7007
https://github.com/microsoft/microsoft-ui-xaml/issues/7231

In the meantime, we recommend being very careful when using Drag-and-Drop
to rearrange the Navigation Tree nodes (this is the only Drag-and-Drop
StoryBuilder supports.)

### ARM64 is broken (#108)

ARM devices are the way forward for more affordable comptuter systems, 
and Microsoft permits compiles to ARM64 and Intel x86 and x64
architectures.  We will build for all three device types.

#### Status: 

We've been working on this since January 2022 and aquired a Samsung
tablet to test with. We've made some progress, but don't have a 
functional StoryBuilder deployment that works on ARM64 devices.

We've acquired a second test computer, a Surface X, and are working on this as time permist.

### Implement a Print Manager for printed reports (#157)

A Print Manager dialog should be part of the Print Report menu. The ability to select a printer and specify its options is essential for 
StoryBuilder print reports.
The current mechanism, printing to the default printer, is a work-around, but not a long-term solution.

#### Status:

This is a reported WInUI issue:
https://github.com/microsoft/CsWinRT/issues/968#issuecomment-918923141

It's been requested on the Windows SDK API product board:
https://portal.productboard.com/winappsdk/1-windows-app-sdk/c/50-support-printmanager-api
It looks like this may be available in the WIndows SDK API  version 1.2.

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
We’d like to support portrait orientation in tables and laptops that support it.
StoryBuilder’s split pane (TreeView on the left and details on the right) works well in landscape mode where the screen is wider than it is tall. For portrait mode, we’d like to use the Hamberger button to toggle back and forth between the TreeView and the detail panel.
#### Status
Not started.
### Unit testing / additional unit tests (#17)
Run all StoryBuilderTest scripts from Test Explorer as a part of 
PR review prior to and after merge. Code new scripts or change old 
ones as a routine part of the development process. If not for 
red/green/red testing, consider it a documentation task.

As time permits, tests need to be generated to fill in code coverage, 
especially for user interactions.

#### Status:

We have an MSTest project added to our solution, and a small number
of tests, dating back to the UWP version of StoryBuilder. The MSTest 
interface didn't work with WinUI 3 for quite a while and xUnit testing
languished. We need to restart this and catch up.

We need a series of smoke tests for high-level 
user interactions, such as creating a new outline, opening and 
changing an existing outline, printing a report, etc. These can be 
based on file comparisons from program output to previously verified program output. Smoke tests are vital for the purpose
of CI in automating PR reviews.