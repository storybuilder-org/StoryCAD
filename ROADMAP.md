# StoryBuilder Roadmap

#### Last Updated: 2022-12-08

Our next Release will be 2.6.0.0. We're taking a holiday break toward the end of December, but
still plan for a release date of around January first.

We expect that this release will include the following features:

### Expand market area to all stores

Our Windows Store distribution was limited to 'flights', lists
of beta users, for early testing. For some reason we also 
restricted our market countries to the ones our testers were
in, the US, Canada, and the UK.

We've now expanded our market area to all countries, since
we're no longer in that type of testing.

#### Status:

This was implemented in point release 2.5.1.0.

### Produce first newsletter

Although the bulk mailing process is not yet in place, we plan
on producing and distributing the first StoryBuilder newsletter.

An email newsletter has two components: creating and maintaining
a list of recipients, and producing and formatting the content of
the newsletter. For the newsletter to be viable, there's a need
for regular quality product. For 

### Remove SyncFusion ComboBoxes (#441)

SyncFusion has been very good for us because it fixed a major problem with 
Microsoft's ComboBoxes- the controls were rendered unacceptably clipped. 
SyncFusion's also been generous with its open-source license.

However, there's a problem with using the product: its makes it 
difficult or prohibitative for others to fork and use StoryBuilder because 
they need their own personal SyncFusion licenses. 
(It should be noted that this is also a problem with elmah.io and secrets.)

We'll test to insure that the Microsoft rendering issue is fixed, and if so,
replace SyncFusion's ComboBoxes with Microsoft's again.

There is one additional consideration: the Purpose of Scene is 
a multi-valued ComboBox, which the Microsoft ComboBox doesn't 
support. However, it should be possible to switch to a ListView 
with checkboxes.

### Purpose of Scene rewrite (#457)

To fix the Purpose of Scene issue mentioned above, we need to
replace the multi-value ComboBox with a (Microsoft) Listview 
with a data template that contains a CheckBox. Checked 
values in the ListViewItems can contain multiple values.

### Revise Character Relationships 'Create a new relationship' (#458)

When adding a new relationship, the relationship type is a
non-editable ComboBox. Consequently, the new relationship
type must be one of the entries in the list. The world's
a bit more complicated than that. The user should be able
to create a new relationship type at will.

### Correct 'Contact Us' function on storybuilder.org

The email us option on the website isn't configured properly.
Correct it so that we can receive communications from 
people visiting the website.

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

### Implement a Print Manager for printed reports (#157) 

A Print Manager dialog should be part of the Print Report menu. The ability to
select a printer and specify its options is essential for 
StoryBuilder print reports. The current mechanism, printing to the default 
printer, is a work-around, but not a long-term solution.

This was a reported WInUI issue:
https://github.com/microsoft/CsWinRT/issues/968#issuecomment-918923141

It was requested on the Windows SDK API product board:
https://portal.productboard.com/winappsdk/1-windows-app-sdk/c/50-support

As of 11/15/2022 (with WinAppSdk 1.2) The Print Manager is available and works
with Windows 11 but doesn't work with Windows 10. We have a sample project which
uses it we can adapt for our purposes.

We will explore two options. One is to add the print manager but
disable it (with a status message) if the user is not on Windows 11.
The other is to trace the status of the Windows 10 fix and schedule
according.

### Codebase cleanup (#439)

The codebase cleanup from  previous release continues. For consistency 
we're using ReSharper to identify and many cases correct the warnings and 
standards violations. 

This doesn't mean that a contributor needs to have ReSharper installed, but
Pull Requests won't be merged until code conforms to the standards which
ReSharper reports on.

## Ongoing and Deferred Issues

Some of these are unresolved issues, others, we've deferrred
due to lack of manpower. They're all on our plate.

### Sidebar index for StoryBuilder User Manual

One serious problem with the documentation is the lack of user-friendly index. We've
looked at several options, but the one we think works best is a sidebar index which is 
always visible. We're working on this for the next release (issue # xxx).

#### Status: 

Not started.

### ARM64 Support (#109)

ARM64 support is planned for the next release. This will allow StoryBuilder to run on 
ARM64 devices such as the Surface X Pro. We exect to see new, lower-cost Windows 11 computers
in the comming year and this is a way to prepare for them.

We've been working on an ARM-native StoryBuilder for many months without a lot of progress,
but with the release of Windows App SDK 1.2 and .NET7, both of which report improved ARM64
support, If time is available we'll work on this in 2.6, but don't
hold your breath.

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

