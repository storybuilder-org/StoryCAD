# StoryBuilder Roadmap

#### Last Updated: 2023-02-08

Our next Release will be 2.8.0.0. 

We expect that this release will include the following features:

## New Features

### Launch StoryBuilder from .stbx file (#94)

A user should be able to launch StoryBuilder by clicking on a .stbx file.
Currently he/she must launch StoryBuilder and navigate to the file to
open.

The Windows App SDK Rich Activation API includes this capability:
https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/applifecycle/applifecycle-rich-activation

### Integrate unit test validation in CI/CD pipeline (#494)

Modify the CI/CD pipeline to run all StoryBuilderTest scripts 
from Test Explorer as a part of every PR review prior to merge. If
any of the tests file, the merge is blocked. 

We'll also continue to code new scripts or change old 
ones, making it a routine part of the development process.


### Codebase cleanup (#496)

The codebase cleanup process continues. For consistency 
we're using ReSharper to identify and in many cases correct the warnings and 
standards violations. 

This doesn't mean that a contributor needs to have ReSharper installed, but
Pull Requests won't be merged until code conforms to the standards which
ReSharper reports on.

With unit testing active we'll begin increasing test coverage for high-risk 
code.

### MarkdownSplitter emits invalid <br/> break closure tags for bulletpoints (#487)

Fix an issue in MarkDownSplitter which processes bullet lists incorrectly.

Generate prev / next links on each split page.

Move MarkDownSplitter into the approprate repo.

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

### Automate back-end email address validation (#497)

Email addresses for contacts and newsletter mailings come from two places: storybuilder.org
contains a MailChimp signup form, and users installing StoryBuilder can enter an email address.
However, the process of merging these lists is currently manual, involving
interactive MySQL commands and editing the MailChimp list manually.

Additionally, the MailChimp email addresses aren't currently validated.

Adding back-end code and use the MailChimp API, automate these processes.