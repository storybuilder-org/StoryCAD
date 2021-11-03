# What is StoryBuilder?

StoryBuilder is an odd duck, with its feet in two ponds: the user community of writers
and the community of developers. And, perhaps, in the Venn diagram of these two communities,
there are a few writer-develpers....

If you're strictly in the writer camp, welcome! You may want to mosey over to 
[User Documentation][2].

If you're developer, welcome! The rest of this document is for you. Read on. Once you're
set up, take a look at [Programmer Notes][3] (If you're a writer, you're
a reader, and these are words on a page, so feel free to read on too.)

StoryBuilder is written in C# and is a Windows desktop application and uses WinUI 3, MVVM, 
Project Reunion APIs, and .NET5.) It's currently distributed via side-loading but the plan
is to distribute via Windows Store in the near future.


Copyright
---------

StoryBuilder was initially developed by Terry Cox for Seven Valleys Software and
is @Copyright Sevens Valley Software.

Except where otherwise noted, StiryBuilder is released under the [GNU GPLv3][1] license.
See the LICENSE file located in this directory.

GPLv3 was selected for the same reason car dealerships don't leave the keys in 
the cars while they're on the lot and before they're sold. 
The intent is to move to an MIT License in the near future, once this site is 
up and running.

How to install
---------------

1. StoryBuilder can be installed and updated using Visual Studio as per this guide:
https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/set-up-your-development-environment?tabs=experimental
You can test your installation by building and running a new app using the Blank App, Packaged (WinUI 3 in Deskto
template as described in the document.

2. In Visual Studio, clone the Storybuilder repository from the GitHub repository at 
https://github.com/terrycox/StoryBuilder-2.git

3. Build and deploy the solution.

4. Distribution via Publish and side loading (and eventualy, with luck, Windows Store) hasn't been worked out yet.

Ackknowledgements
-----------------

There are so many, we needed a separate file:
[Acknowledgements][4]


[1]:https://choosealicense.com/licenses/gpl-3.0/
[2]:https://github.com/terrycox/StoryBuilder-2/blob/master/docs/USERNOTES.md
[3]:https://github.com/terrycox/StoryBuilder-2/blob/master/docs/DEVNOTES.md
[4]:https://github.com/terrycox/StoryBuilder-2/blob/master/docs/Acknowledgements.md
