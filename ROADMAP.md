# StoryBuilder Roadmap
#### Last Updated: 2022-11-15

Our next Release will be 2.5.0.0, planned for around December 1st.
We expect that this release will include the following features:

### .Net7 (#438)

Microsoft has released the next iteration in its .NET framework, .NET7, which has improvements in  
ARM64 support, desktop app support, performance improvements, and app size reduction.

### Sidebar index for StoryBuilder User Manual

One serious problem with the documentation is the lack of user-friendly index. We've
looked at several options, but the one we think works best is a sidebar index which is 
always visible. We're working on this for the next release (issue # xxx).

### Allow community to easily recommend documentation changes #384

This feature already exists: each page contains an 'Improve this Page' link. 

We just add a note encouraging users to use and and explaining how.

### ARM64 Support (#109)

ARM64 support is planned for the next release. This will allow StoryBuilder to run on 
ARM64 devices such as the Surface X Pro. We exect to see new, lower-cost Windows 11 computers
in the comming year and this is a way to prepare for them.

We've been working on an ARM-native StoryBuilder for many months without a lot of progress,
but with the release of Windows App SDK 1.2 and .NET7, both of which report improved ARM64
support, we're hopeful that we'll be able to get this working in 2.5.

### Codebase cleanup (#439)

The codebase cleanup from 2.3 and 2.4 continues. For consistencey we're using ReSharper 
to identify and many cases correct the warnings and standards violations.

### Remove Id property #396

We also want to provide for users to report documentation isses and recommendations.
Each page of the User Manual contains an 'Improve this page.' link footer. The
link opens an issue on GitHub. We'll add documention and an example (issues # 384). 

### Master Plots should be copied as a Problem rather than Scene (#260)

Self-descriptive.

### Add an 'Overview and Main Story Problem' template for new story creation (#151)

Self-descriptive.

## Ongoing and Deferred Issues

### Implement a Print Manager for printed reports (#157) (Planned for 2.6)

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

As of 11/15/2022 (with WinAppSdk 1.2) The Print Manager is available and works
with Windows 11 and supposedly with Windows 10. We have a sample project which
uses it. We need to integrate the PrintManager code into our existing reports.

### Drag and Drop is buggy (#312) (Planned for Release 2.6)

While powerful, when dragging and dropping story nodes to reorder them
can and has caused a lot of inappropriate things to happen. For example,
moving stuff to the trashcan, which causes issues as it's not able 
to be track if the node being trashed is used elsewhere in the code.
Deleting a currently-used story element can and does cause issues. 

We've tried using drag and drop events to restrict what and where a
node can be dropped, but the event support has problems.

We're told this may be fixed in Windows App SDK 1.2, and will test this. 
If so, we'll add it to the next release.

#### Status:

This has been reported by us to Microsoft as a WinUI 3 issue:
https://github.com/microsoft/microsoft-ui-xaml/issues/7266

It's one of a series of related issues:
https://github.com/microsoft/microsoft-ui-xaml/issues/7002
https://github.com/microsoft/microsoft-ui-xaml/issues/7007
https://github.com/microsoft/microsoft-ui-xaml/issues/7231

A/O 11/15/2022, awaiting testing.

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

#### Status: 

We've been working on this since January 2022 and aquired a Samsung
tablet to test with. We've made some progress, but don't have a 
functional StoryBuilder deployment that works on ARM64 devices.

We've acquired a second test computer, a Surface X, and are working on this as time permits.

With Windows App SDK 1.2 production and .NET 7, which have improved ARM64 support. we may 
be able to get some more traction, but the Windows App SDK was delayed and we missed Release 2.4
In process for Release 2.5.

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
