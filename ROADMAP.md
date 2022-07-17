# StoryBuilder Roadmap

## Release Planning

We are now delivering software via a Windows Store 'flight'. 

We anticipate that our next release will include:


## Distribute StoryBuilder via Windows Store

This is a major milestone and is descdribed in a Project in 
StoryBuilder's repository: https://github.com/orgs/storybuilder-org/projects/3

#### Status:

StoryBuilder is available for free download from the Store as a 
'restricted flight'. It's restricted to a list of Windows userid
email addresses, while we perform additional testing and complete
several tasks. 

We will update (notably the back-end
server), enough work has been done that we've submitted our app to the 
Store.

Although all requirements appear to be met, the actual submission fails 
with a Microsoft internal error. This is being researched by Microsoft
as incident 2206130040005500. 



## Add an Autosave Feature (#351)

Users can get wrapped up in their creative process and forget simple 
things like saving their work frequently. We have various backup options, 
but one feature that would compliment these is an AutoSave feature, 
which saves the current outline exactly like Save Story / Ctrl+S / 
Clicking the edit status button. AutoSave's save interval should be 
saved in specified in seconds rather than minutes.

We should recommend or require the backup at the start of edit session 
if AutoSave is selected, so that the user can revert changes back 
easily if he or she decides they're unwanted.

#### Status:

Completed and implemented.

## Creating a Story' in help is not matching options in Storybuilder (#349)

The Tutorial in the StoryBuilder User Manual is from the old (V1) StoryBuilder,
and needs revised / rewritten.

Not only the screenshots, but the actual story design process, need revised
to account for the many changes in StoryBuilder V2.

#### Status:

Still in Progress.

## Drag and Drop is buggy (#312)

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

## Show Changelog on Update (#267)

When the app is updated show a notice that explains whats changed, this could then be accessed from another place to see whats changed.

#### Status:

Completed and implemented.

## ARM64 is broken (#108)

ARM devices are the way forward for more affordable comptuter systems, 
and Microsoft permits compiles to ARM64 as well as Intel x86 and x64
architectures.  We will build for all three device types.

#### Status: 

We've acquired a test computer system and worked on this since January 2022 with 
no success. We continue to work on it.

## Implement a Print Manager for printed reports (#157)

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

## Back-end server (#255, 256)

We need a server to hold deployment information for integration with our 
web server for a StoryBuilder newsletter as well
as for deployment tracking (consistent with our user data and 
privacy policy.)

#### Status:

FOSSHost.com accepted a server request for a free server 
and has made one available on AARCH64.com. We initially installed 
Ubuntu Server on it and have contracted a contractor (Caresort) 
to install and configure LAMP software and code
(Parse Server) to receive and save the required data from 
StoryBuilder clients. We badly interpreted how complicated the 
Linux Parse Server software stack would be to implment, and have
decided to take a simpler approach, holding the required information
in a SQL database and writing batch code to process it.

We've coded the new StoryBuilder interface and are in the process of
accessing the server from the client code.

## Create a 'copy to narrative' tool (#23)

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

## Program demo videos (#145)

#### Status:

In process. We need a minor modification to our website to link to
these videos.

## Picture pickers for characters and settings (#53)

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

## Launch StoryBuilder from .stbx file (#94)

A user should be able to launch StoryBuilder by clicking on a .stbx file.

The Windows App SDK Rich Activation API includes this capability:
https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-rich-activation

#### Status:

Not started.

## Alternative use of Hamburger button for small screens (#25)

#### Status

## Unit testing / additional unit tests (#17)
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