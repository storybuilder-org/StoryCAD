# What is StoryBuilder?

StoryBuilder is an odd duck, with its feet in two ponds: the user community of writers
and the community of developers. And, perhaps, in the Venn diagram of these two communities,
there are a few writer-develpers....

If you're strictly in the writer camp, welcome! You may want to mosey over to 
[User Documentation][2].

If you're developer, welcome! To learn more about StoryBuilder, you should read the
[User Documentation][2] as well. The rest of this document is for you, and describes
how to contribute.

StoryBuilder is written in C# and is a Windows desktop application and uses WinUI 3, MVVM, 
Project Reunion APIs, and .NET5.) But the only skill you need to get started is some C#. 
You'll need to configure your development environment as described in **Tools and Setup**.

Once you've read through this document and are set up, take a look at [Programmer Notes][3].

## Tools and Setup

1. StoryBuilder is installed and updated with Visual Studio. Either VS2019 or VS2022 
will work. The Community editions of these products are free. You can download
from here:
https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio?view=vs-2019

2. StoryBuilder is developed with the Windows App SDK. Set up your development as per this guide:
https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/set-up-your-development-environment?tabs=experimental

You can test your installation by building and running a new app using the Blank App, Packaged (WinUI 3 in Deskto
template as described in the document.

2. In Visual Studio, clone the Storybuilder repository from the GitHub repository at 
https://github.com/terrycox/StoryBuilder-2.git

3. Build and run the solution.

4. Click  the File (document icon) button on the menu and open the HELLO sample to verify.

[1]:https://choosealicense.com/licenses/gpl-3.0/
[2]:https://github.com/terrycox/StoryBuilder-2/blob/master/docs/USERNOTES.md
[3]:https://github.com/terrycox/StoryBuilder-2/blob/master/docs/DEVNOTES.md
[4]:https://github.com/terrycox/StoryBuilder-2/blob/master/docs/ACKNOWLEDGE.md
