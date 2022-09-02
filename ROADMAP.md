# StoryBuilder Roadmap

## Current Release

As of August 31st, 2022, we've rolled out Release 2.2.0.0. We have now opened the app up to general distribution via the Microsoft Store. 

This release has a few fixes and improvements whilst implementing new features such as the Narrative Editor.

## Next Release

Our next Release will be 2.3.0.0, planned for around September 31st. 

We expect that this next release will include the following features:

### Improving StoryBuilder Tests (#17)
StoryBuilder has long needed a way to comprehensively test all features 
of the app to prevent regressions from being introduced accidentally 
when the codebase is changed. Currently StoryBuilder checks that the code 
will compile and create a working version when a Pull Request is created; 
however, implementing a suite of MsTest unit tests and running them to 
verify builds will help prevent or reduce regressions.

### Code Cleanup 
StoryBuilders code is duplicated in places and this can cause issues: if 
the code is flawed, it will need to be fixed in multiple places. There's
also unused (dead) code, overly complicated code, and variance in coding
standards. Cleaning the code should reduce the file size and improve 
the code quality of the app.

### Single Instancing (#43)
Only one instance of StoryBuilder should be open at once as the program
is not designed to have multiple instances open and this can cause error and other various mischief. As such when one instance is open already and another is launched it should bring the current app to the front.

### Update Website
The website is need of updates to ensure that all information is up to date.

### Update Samples
Some samples are outdated and don't properly show off all the features 
of the app. Some samples are also missing information either from creation 
or early updates may have caused issues.

## Experimental Features
These features ideally should make it into 2.3.0.0 however these issues pose complex issues and require extensive research into the features to get working.

### New Node Types (#45)
During a discussion of the roadmap for 2.3.0.0, Issue #45 was discussed. This
add two additional story element types, for notes and web pages, to improve
the ability to collect research relevant to the story.

### ARM64 Support
Support for ARM64 processors has been a longstanding ue to issues 
with figuring out what is reallly wrong with ARM64 Builds as very little 
information is given. With a stronger ARM64 device in our posession, we're
trying to find 'the what really causes the issues.
This may have to wait for Windows App SDK 1.2 production, which has  
has improved ARM64 support.

## Ongoing Issues

### Drag and Drop is buggy (#312)

While powerful, when dragging and dropping story nodes to reorder them
can cause a lot of inappropriate things to happen. For example,
moving stuff to the trashcan which may cause issues as it's not able 
to be track if the node being trashed is used elsewhere in the code.
Deleting a used story element can and does cause issues. 

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
