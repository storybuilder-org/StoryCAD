# Acknowledgements

## Contributors

Any complex undertaking, such as writing a novel, creating a
complex piece of software, or a marriage, requires cooperation.

We would like to express our sincere appreciation and gratitude 
to those whose generous ideas and time helped make StoryBuilder possible.
If we've missed anyone's contribution, our sincere apologies. Let us know
and we'll rectify that.

Thanks go to these wonderful people:

* Rarisma     
* Mylo
* Tina
* Tim        

## Software

StoryBuilder uses or links to the following software:

* [WinUI 3][1]
* [Windows App SDK][2]
* [Windows Community Toolkit][3]
* [Elmah.io][4]
* [NRtfTree][5]
* [Scrivener][6]
* [SyncFusion][7]

We are especially grateful to elmah.io and Syncfusion, commercial products who make their
livelihood freely available to public open source projects like ours.

## Origins

Hi, I'm Terry Cox. 

I started StoryBuilder as a hobby, for my own attempts at fiction writing,
 back  in the late 1980's. It began to attract attention at writer's conferences 
(I had a very early laptop), and in the early 90's I had the dubious idea of polishing
it and selling it commercially. StoryBuilder had modest success, selling over 3,000 
copies, but inevitably software rot set in as my day job consumed more of time and
energy, and first fell into disuse and then became unusable. The original StoryBuilder
is the ancestor of this one version.

After I retired, I had the idea of rewriting StoryBuilder and selling it again,
and started down that road. However, looking at my tired old face in the mirror one 
morning, I came to realize that I wasn't interested in going into business again. I
wasn't interested in scrapping my project either, which realization led to the decision
to distribute StoryBuilder as free and open source software (FOSS.)

I'm pleased with how it's developing and I hope you find it useful.

## Software Influences

Unless I'm deluding myself, version 2.0 of StoryBuilder is a better program
than the original. That's because I know the trick of how to write better
software: You shamelessly shamelessly good ideas and code patterns from other developers.
Five individuals and their work deserve mention for this reason.

Laurent Bugnion, Galasoft, MVVM Light Toolkit
https://github.com/lbugnion/mvvmlight

MVVM Light was one of the most influential early MVVM projects. Although it's fallen
into disuse, it was a direct influence on [Windows Community Toolkit MVVM][3], 

Perigrin66, Pergrine's View, StaffManager
http://peregrinesview.uk/mvvm-bringing-it-all-together/

The navigation tree and details panel design is very old; it was one of the early driving
forces in the creation of C++. StoryBuilder V1 used it (poorly.) When StoryBuilder V2
was just a vague idea, this small project suggested a way forward.

Diederik Krols, XAML Brewer, Using a TreeView Control for Navigation in UWP
https://xamlbrewer.wordpress.com/2018/06/08/using-a-treeview-control-for-navigation-in-uwp/comment-page-1/#comment-296

This project lead me toward UWP, and the birth of the WinUI 3 project cemented the 
way forward. XamlBrewer is a great stylist and his posts are always worth reading.

Ryan Demopoulos
https://docs.microsoft.com/en-us/windows/apps/winui/

The WinUI3 project has a

Michael Hawker (XAML Llama)
https://dotnetfoundation.org/projects/windowscommunitytoolkit

## Writing Influences


GMC: Goal, Motivation & Conflict, Debora Dixon, 

StoryBuilder handles story structure in terms of its building blocks,
story elements: Problem, Character, Setting, and Scene. Story elements are
in turn composed of smaller components. A Problem contains three 


-GMC

##Writing websites


Writer's whatumcallit
'writing scenes' stuff

[1]:https://microsoft.github.io/microsoft-ui-xaml/
[2]:https://github.com/microsoft/WindowsAppSDK
[3]:https://github.com/CommunityToolkit/WindowsCommunityToolkit
[4]:https://elmah.io/
[5]:https://github.com/sgolivernet/nrtftree
[6]:https://www.literatureandlatte.com/scrivener
[7]:https://www.syncfusion.com/winui-controls