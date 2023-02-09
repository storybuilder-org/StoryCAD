# StoryBuilder Roadmap

#### Last Updated: 2023-02-08

Our next Release will be 2.8.0.0. 

We expect that this release will include the following features:

### Launch StoryBuilder from .stbx file (#94)

A user should be able to launch StoryBuilder by clicking on a .stbx file.
Currently he/she must launch StoryBuilder and navigate to the file to
open.

The Windows App SDK Rich Activation API includes this capability:
https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-rich-activation

### Integrate MSTest into CI/CD pipeline (#xx)

Modify the CI/CD pipeline to run all StoryBuilderTest scripts 
from Test Explorer as a part of every PR review prior to merge. If
any of the tests file, the merge is blocked. 

We'll also continue to code new scripts or change old 
ones, making it a routine part of the development process.


### Codebase cleanup (#439)

The codebase cleanup from  previous release continues. For consistency 
we're using ReSharper to identify and many cases correct the warnings and 
standards violations. 

This doesn't mean that a contributor needs to have ReSharper installed, but
Pull Requests won't be merged until code conforms to the standards which
ReSharper reports on.

### Produce first newsletter

The bulk mailing process is mostly in place, using MailChimp.
We've started the newsletter email recipient list. The names
we've collected from both software registrations and the website
are in need of clean-up and verification, notably to remove
junk email addresses. 

The content for the first newsletter, besides the 2.6.0.0 changelog
and this roadmap, are drafted, and a template for the newslett's
being put together.

Some of these are unresolved issues, others, we've deferrred
due to lack of manpower. These are things we think will 
make StoryBuilder feature complete, although we don't think
that's the end of StoryBuilder development, but rather mark
the place where you our users tell us what features you want.


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

### Alternative use of Hamburger button for small screens (#25)
We’d like to support portrait orientation in tables and laptops that support it.
StoryBuilder’s split pane (TreeView on the left and details on the right) works well in landscape mode where the screen is wider than it is tall. For portrait mode, we’d like to use the Hamberger button to toggle back and forth between the TreeView and the detail panel.

#### Status
Not started.

