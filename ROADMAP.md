# StoryBuilder Roadmap

## Current Release

As of October 3, 2022, we've rolled out Release 2.3.0.0 for general distribution 
via the Microsoft Store. 

2.3 has one significant new feature, the addition of researach tools. These take
the form of two new Story Element types which can be added as nodes to your outlines:
WebPage and Note nodes.

WebPage nodes are used to store links to web pages. They're implemented using the
WebView2 control, which is built on the Edge browser. A WebPage node can use search
to find a page and will persist that page's URL in your outline so that when you 
navigate to it the page is loaded and displays in the node's content area.

Note nodes are used to store text notes. They're implemented using the RichEditBox.
Major Story Element nodes (Story Overview, Problem, Character, Setting and Scene) all
have a Notes tabes which can be used to store notes about that particular Story Element,
but the new Note nodes are intended to be used to store notes about any topic  you
wish.

StoryBuilder is a new product, and our priority remains bug fixes and improvements.
2.3 is primarily a fix release, as will be future short-term releases. Some
specific fixes in this release include:

#### Implemented Single Instancing

StoryBuilder now uses Single Instancing. If the app is already open and you launch 
it again, the existing instance will be brought to the foreground rather than having
a new instance launched. While the ability to edit more than one outline at one time
has its uses, it can also cause problems. For example, if you have two instances of
StoryBuilder open and you edit the same Story Element in both instances, the changes
from one instance will overwrite the changes from the other instance. Single Instancing
prevents that from happening.

#### Codebase cleanup

Actively maintained programs tend to accumulate cruft over time. This release we've
started a process of addressing this by working through the codebase and removing
duplicate and unusued code, conforming to some newly-set naming conventions,
and making other improvements.This is a long-term process, which will continue in
future releases, but we're making progress.

#### User Manual and sample updates

We've updated the User Manual and several sample outlines to improve the documentation
and to reflect changes in the app. As with the codebase cleanup, this will be an
ongoing process. The User Manual changes are still mostly structural, and we're
well aware that line and copy edits are needed. We'll be working on those in future
releases.

#### Bug fixes:

* Fixed a bug preventing cast information from being saved.
* Fixes several issues causing progdram crashes.
* Fixed some issues relating to topics. Besides in-line topic
  data (topic/subtopic/notes), it's possible for a topic to 
  launch Notepad to display a file. This fix has that working.
* Fixed some issues with tracking changes.

## Next Release

Our next Release will be 2.4.0.0, planned for around November 1st.

We expect that this next release will include the following features:

### Windows App SDK Api 1.2

We are hopeful that 1.2 contains features which will help with several
StoryBuilder concerns, including drag-and-drop, printing (Print Manager),
and MSTest integration. 1.2 can't be used with Windows Store apps until
it's production, which should be around the November 1 timeframe. We'll
develop our 2.4 release using the experimental channel of the Windows App SDK
specifically for these features.
 
### UI Improvements

#### Revised right-click menu

The right-click menu is used to add new Story Elements, and to add new

#### XAML cleanup

StoryBuilder UI layout is done using Extensible Application Markup Language
(XAML), a declarative language. One feature of XAML is Styles, which allow 
certain layout settings such as margins and text size to be set once and reused 
across the app for consistent appearance. 

We haven't been using Styles in this way, and will use ResourceDictionary 
style settings to standardize our layouts.

#### Cast List revamp

Scene Story Elements contain a Cast List control which, as its name implies,
is used to list the characters in a scene. The Cast List control is based
on a ListView and requires a convoluted set of interactions to add and
delete cast members. We'd like to revise or replace the control with something
easier for users.

#### Highlight in-process node

An outline is a tree of Story Elements which are displayed in the Navigation Pane. 
Clicking (or touching) a Story Element node on the Navigation Pane displays that 
Story Element’s content in the Content Pane. A node can also be right-clicked to
display a commandbar flyout context menu. The current (clicked) node is highighted
in the Navigation Pane, but the highlight is not very visible. Right-clicing a node
momentarily highlights it but with the light theme it's almost impossible to see.
We'd like to make the highlighting of the two nodes more visible.

#### Loading indicator

Several tasks, notably printing reports and loading installation data, take 
a bit of time. We'd like a progress indicator for these tasks.

### Default Preferences

Some Preferences data, such as the default outline and backup folder locations,
should be set to appropriate defaults. 

### Search engine choice

Be able to select the search engine to use with Webpages. The values we'll
start with are:
- Google
- DuckDuckGo
- Bing
- Yahoo

### Codebase cleanup

The codebase cleanup from 2.3 continues. For consistencey we're using ReSharper 
to identify and many cases correct the warnings and standards violations.

## Ongoing Issues

### Drag and Drop is buggy (#312)

While powerful, when dragging and dropping story nodes to reorder them
can and has caused a lot of inappropriate things to happen. For example,
moving stuff to the trashcan, which causes issues as it's not able 
to be track if the node being trashed is used elsewhere in the code.
Deleting a currently-used story element can and does cause issues. 

We've tried using drag and drop events to restrict what and where a
node can be dropped, but the event support has problems. We're told
this may be fixed in Windows App SDK 1.2, and will test this. 

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

### ARM64 Support (Issue #108)

ARM devices are the way forward for more affordable comptuter systems, 
and Microsoft permits compiles to ARM64 and Intel x86 and x64
architectures.  We want to build for all three device families.

Support for ARM64 processors has been a longstanding issue 
for us- specifically, figuring out what's wrong without much
information given. We're trying to find 'the what really causes the issues.

With Windows App SDK 1.2 production, whichhas improved ARM64 support. we may 
be able to get some more traction, but won't be working on it in the 2.4 release.

#### Status: 

We've been working on this since January 2022 and aquired a Samsung
tablet to test with. We've made some progress, but don't have a 
functional StoryBuilder deployment that works on ARM64 devices.

We've acquired a second test computer, a Surface X, and are working on this as time permits.

### Implement a Print Manager for printed reports (#157)

A Print Manager dialog should be part of the Print Report menu. The ability to select a printer and specify its options is essential for 
StoryBuilder print reports.
The current mechanism, printing to the default printer, is a work-around, 
but not a long-term solution.

#### Status:

This is a reported WInUI issue:
https://github.com/microsoft/CsWinRT/issues/968#issuecomment-918923141

It's been requested on the Windows SDK API product board:
https://portal.productboard.com/winappsdk/1-windows-app-sdk/c/50-support-printmanager-api
It looks like this may be available in the WIndows SDK API  version 1.2.

We'll implment this as soon as the API supports it. In the meantime,
use the workaround, which is to set the default printer to the printer
you'd like to print StoryBuilder reports on. This can include PDFs.

Will check on status with Windows App SDK API 1.2.

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
