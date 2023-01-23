# StoryBuilder Roadmap

#### Last Updated: 2023-01-23

Our next Release will be 2.7.0.0. We were very late with
the 2.6.0.0 release, but will make this a short release in
order to try to get back on track.

We expect that this release will include the following features:

### ARM64 Support (#109)

ARM64 support is planned for the next release. This will allow StoryBuilder to run on 
ARM64 devices such as the Surface X Pro. We exect to see new, lower-cost Windows 11 computers
in the comming year and this is a way to prepare for them.

We've been working on an ARM-native StoryBuilder for many months without a lot of progress,
but with the release of Windows App SDK 1.2 and .NET7, both of which report improved ARM64
support, we're confident that we can get this working.

### Improvements to Conflict Builder

Add additional subcategories of criminal conflucts, 
'crimes of passion' and 'professional criminal', with 
appropriate examples, and 'identify conflicts' as a new
major category. 

The existing examples will also be reviewed.
 
### Problem Category

We'll add a new field to the Problem class, ProblemCategory. 
This is a non-editable drop-down list (SfComboBox) with 
the following categories: complications, character flaws, subplots,
antagonist, minor conflicts, relationship conflicts, societal
conflicts, nature conflicts, and time conflicts.

One of these will be the central main story conflict. 

### Produce first newsletter

The bulk mailing process is mostly in place, using MailChimp.
We've started the newsletter email recipient list. The names
we've collected from both software registrations and the website
are in need of clean-up and verification, notably to remove
junk email addresses. 

The content for the first newsletter, besides the 2.6.0.0 changelog
and this roadmap, are drafted, and a template for the newslett's
being put together.


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
interface is now working with WinUI 3 with the exception of UX
tests. We will get the initialization 'plumbing' working and
some of our existing test scripts working.

We need a series of smoke tests for high-level 
user interactions, such as creating a new outline, opening and 
changing an existing outline, printing a report, etc. These can be 
based on file comparisons from program output to previously verified program output. Smoke tests are vital for the purpose
of CI in automating PR reviews. This will come in a future release.

### Codebase cleanup (#439)

The codebase cleanup from  previous release continues. For consistency 
we're using ReSharper to identify and many cases correct the warnings and 
standards violations. 

This doesn't mean that a contributor needs to have ReSharper installed, but
Pull Requests won't be merged until code conforms to the standards which
ReSharper reports on.

## Ongoing and Deferred Issues

Some of these are unresolved issues, others, we've deferrred
due to lack of manpower. These are things we think will 
make StoryBuilder feature complete, although we don't think
that's the end of StoryBuilder development, but rather mark
the place where you our users tell us what features you want.

### Sidebar index for StoryBuilder User Manual

One serious problem with the documentation is the lack of user-friendly index. We've
looked at several options, but the one we think works best is a sidebar index which is 
always visible. We're working on this for the next release (issue # xxx).

#### Status: 

In progress. 


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

