# Contributing

Hello, and welcome! Thank you for wanting to help improve StoryBuilder. Here's what you need to know.

If you have a question, make sure it wasn't [already answered][1]. If not, you may want to check out [DEVNOTES][2].

> Our goal is to provide answers to the most frequently asked questions somewhere in the documentation.

### Bugs

Bug reports are tricky. Please provide as much context as possible, and if you want to start working on a fix, we'll be forever grateful! Please try and test around for a bit to make sure you're dealing with a bug and not an issue in your implementation.

If possible, provide a demo where the bug is isolated and turned into its smallest possible representation. That would help a lot!

Thanks for reporting bugs, we'd be lost without you.

### Feature Requests

We're always looking for feature requests. We'll try to prioriize new feature development that makes our users happy.
Toward that end, we'll put together a feature voting system in the near future. 
The goal is making StoryBuilder more usable.

# Development

If you want to clone the repository and hack at it, go right ahead. 

But to get your changes accepted back into production and distribution, 
we use a more collaboratory approach, based on GitHub branch/merge. 
It lets you play with your changes safely, commit any time for backup
purposes, and never put the project at risk. It also insures that there's
always an easy way to back changes out, and that multiple
eyes are on any production changes.

Contributions should normally start from issues (bugs or feature requests)
to maintain some history of why the change was made.

The main steps are:

1. Create a branch off of main
2. Make commits
3. Open a pull request
4. Collaborate through PR comments
5. Make more commits
6. Discuss and review code with team members
7. Deploy for final testing
8. Merge your branch into the main branch

### Build

Building and debugging can be done in your branch with impunity. 

At present there's no CI/CD pipeline for StoryBuilder. Manual processes


### Test

The StoryBuilderTest project is an MSTEST console application that accesses and runs
scripted unit tests aginst StoryBuilderLib's back-end code and viewmodels. 

There is at present no direct UI testing, but all unit tests will be ran to success
before each merge to the main branch.

Developers are urged to add test cases for their contributed changes. 

[1]: https://github.com/terrycox/StoryBuilder-2/issues
[2]: http://https://github.com/terrycox/StoryBuilder-2/tree/master/docs/DEVNOTES.md
