---
title: Changelog
layout: default
nav_enabled: true
nav_order: 116
parent: For Developers
has_toc: false
---
## StoryCAD ChangeLog

#### Release 3.3.0.0
StoryCAD 3.3.0.0 was released in September 2025.
- Code Clean up (#1074, #1075, #1079, #1086, #1090, #1093, #1094, #1095, #1096, #1097, #1098, #1099, #1101, #1103 #1104, #1105)
- Printing is no longer supported on Windows builds older than Windows 10 22H2 (#1059)
- Outlines created before 3.0.0 now require manual migration
- Update Tests (#1079, #1091)
- Update Dependencies (#1082)
- Upgrade Search (#1085)
- Update to .NET9
- Fix ReleaseBuilder

**API Changes:**
- Data source has been removed and replaced (#1074)
- Fix Assets missing issue (#1080)
- Added search to API (SearchForText and SearchForGUID) (#1085)
- A text field in every property field has been renamed to Description (See #1102 for more info)

#### Release 3.2.1.0
StoryCAD 3.2.1.0 was released in July 2025.
- Fix backup folder missing crash
- Fix many DND issues
- Fix error in reports

#### Release 3.2.0.0
StoryCAD 3.2.0.0 was released in July 2025.
- Add custom beat sheets (#1011, #1026)
- Add Problem/Scene conversion option (#994)
- Add treeview and file open dialog resizing (#1001)
- Add preference to hide key file warning (#1002)
- Add Backup tab (#1007, #1020)
- Update CI/CD to Windows Server 2025 (#1013, #1017, #1027)
- Rework Structure Tab (#1011, #1019)
- Validate path before creating outline file (#1021)
- Increase size of narrative editor UI (#1033)
- Block creation of new root nodes in narrative editor (#1033)
- Improve docs (#999)
- Improve tests (#997, #1022, #1025)
- Allow SK to override the GUID of a given element (#1008)
- Fix crash on shutdown (#1024)
- Fix empty trash index crash (#1023)
- Fix issues with drag and drop (#1034)
- Fix issues with templates (#1031)
- Fix issue with inserting dramatic scenes (#1031)
- Fix issues with master plots (#1031)
- Fix crash when print fails (#1031)
- Fix name shown when copying element in narrative editor (#1031)

#### Release 3.1.4.0
StoryCAD 3.1.4.0 was released in May 2025.
- Update documentation
- Fix issue when creating story

#### Release 3.1.3.0
StoryCAD 3.1.3.0 was released in May 2025.
- Maintenance release

#### Release 3.1.2.0
StoryCAD 3.1.2.0 was released in May 2025.
- Fix issue with lock
- Fix issue with prefs file being unwritable

#### Release 3.1.1.0
StoryCAD 3.1.1.0 was released in May 2025.
- Fix crash when open outline is clicked and no outline is selected
- Fix crash when deleting nodes

#### Release 3.1.0.0
StoryCAD 3.1.0.0 was released in April 2025.
- Add StoryCAD API
- Update Dependencies
- Clean compile warnings
- Fix crash when clicking generate reports multiple times
- Increase Log Size
- Clean up unified menu
- Update Documentation
- Fix crash in save as menu

#### Release 3.0.2.0
StoryCAD 3.0.2.0 was released in February 2025.
This is a maintenance release to fix reported bugs.
- Fix Migration issue (#919)
- Update Story Elements lists not loading correct lists on outline reload (#920)

#### Release 3.0.1.0
StoryCAD 3.0.1.0 was released in February 2025.
- Fixed warnings (#896)
- Fixed issues when opening story elements (#907)
- Fixed scaling issue with feedback prompt (#906)
- Fixed issue with rocky sample (#911)

#### Release 3.0.0.0
StoryCAD 3.0.0.0 was released in February 2025.
- Updated manual and documentation (#838, #851, #854, #857)
- Updated getting started dialog (#855)
- Updated Dependencies (#856)
- Save data as JSON (#865, #866, #872, #875)
- Store references to elements as GUIDs (#874, #889)
- Added backup now option (#876)
- Fixed report crash (#887)
- Fixed RTF Extend Box titles on dark theme (#873)
- Fixed various bugs in 3.0 beta (#894)
- Fixed Memory leak (#895)
- Fixed dead discord link
- Fixed issue with displaying changelog
- Fixed issue with storyreader in invalid paths (#853, #870)
- Fixed ItemsSource list issue with ViewpointCharacter (#892)
- Removed first time using StoryCAD prompt in initialization page

#### Release 2.15.0.0
StoryCAD 2.15.0.0 was released in November 2024.
- Update Dependencies
- Add Structure tab to Problem Page
- Update samples
- Added additional help page
- Update top bar to make all buttons fit

#### Release 2.14.6.0
StoryCAD 2.14.6.0 was released in August 2024.
- Fix issue where story names could contain invalid characters, causing errors (#790)
- Fixed crash trying to delete nodes

#### Release 2.14.5.0
StoryCAD 2.14.5.0 was released in August 2024.
- Fix issue where text inside comboboxes could be invisible until clicked once

#### Release 2.14.4.0
StoryCAD 2.14.4.0 was released in July 2024.
- Fixed issues with buildtools
- Fixed issue with File/Folder open Menus
- Various changes for future releases of StoryCAD

#### Release 2.14.3.0
StoryCAD 2.14.3.0 was released in July 2024.
- Clean up codebase (#750, #751, #753)
- Fix issues with saving (#761)
- Groundwork for future updates (#748, #749, #752, #754, #758)

#### Release 2.14.2.0
StoryCAD 2.14.2.0 was released in June 2024.
- Updated icons

#### Release 2.14.1.0
StoryCAD 2.14.1.0 was released in June 2024.
- Fix how StoryCAD handles cloud files when they are unavailable (#738)

#### Release 2.14.0.0
StoryCAD 2.14.0.0 was released in May 2024.
- Clean up code
- Removed Patreon Code
- Add rating prompt
- Added report feedback button
- Updated file writes
- Updated user manual
- Fixed potential issue with print reports menu
- Update Drag and Drop to prevent issues
- Updated Publisher Field

#### Release 2.13.1.0
StoryCAD 2.13.1.0 Was released in January 2024.
- Fixed bug when using templates and you haven't reloaded your outline yet (#692)
- Fixed issue in lists.ini (#693)
- Upgraded tests to prevent regressions (#695)
- Updated Dependencies

#### Release 2.13.0.0
StoryCAD 2.13.0.0 was release on November 28th 2023.
**This version removes support for versions of Windows older than Windows 10 20H2**

 - Added option to set StoryCAD's theme independently of the system theme (#668, 666)
 -  Removed Install Service to boost load times (#660)
 - Updated StoryCAD to use an automated testing suite to prevent regressions (#661, #658, #655)
 - Updated dependencies (#665)
 - Updated StoryCAD to use .NET8 (#671)
 - Updated and fixed issues with Samples and the Samples menu (#678, #669, #664)
 - Split the name option into a first name and surname option (#663)
 - Cleaned up code (#680)
 - Fixed issues connecting to the backend server (#682, #677)
 - Fixed potential issue with file open launches (#686)
 - Fixed rare crash that could occur when opening a content dialog too fast after initialization (#679)


#### Release 2.12.0.0
StoryCAD 2.12.0.0 was released on September 29th, it mainly focused on fixing issues.

- Fixed Crashes with Drag and Drop (##594)
- Updated Manual (##595, ##616)
- Removed ability to create blank traits (##600)
- Fixed an issue where deleting a trait would always remove the topmost one
- Fixed a crash that could occur in the Right-Click Menu (##592)
- Fixed issue where master plots could cause the app to enter a locked state (##598)
- Update Packages (##601, ##608, ##630, ##635)
- Updated Relationships dialog to fix a potential issue (##601)
- Cleaned up code and fixed warnings (##609)
- Fixed title not updating properly (##607)
- Updated change log to not show on developer builds (##605)
- Fixed crashes in the print menu (##603)
- Windows 10 Users on the latest version can now use the updated print UI (##604)
- Fixed some issues with model loads (##606)
- Updated how developer builds are numbered (##611)
- Updated About page to show all StoryCAD Social URLS (##615)
- Updated Readme (##614)
- Fixed previously opened story page staying open when creating new file (##618)
- Updated how node highlighting works to be more intuitive (##623)
- Fixed issues with Scene Cast (##625, ##647)
- Fixed crash when navigating between two scene development tabs (##632)
- Fixed crashes when searching (##631)
- Fixed problems occurring when storing outlines in cloud storage (##633)
- Fixed folder picker boxes in the initialisation screen not updating (##640)
- Fixed issue preventing backend communication (##643)
- Fixed duplicated 'Psychiatrist' role (##652)
- Updated saving to save the model even if no changes have been made (##653)
- Fixed version number not showing properly (##654)

#### Release 2.11.0.0
As of June 6th 2023, we have released version 2.11.0.0
- Added feature where new nodes will automatically be navigated to
- Added warning for if a story synopsis is empty but selected anyway
- Updated dependencies and story templates
- Update backup and autosave to fix issues and warnings if they fail
- Updated the premise box update the original story problem
- Updated right click menu to show keybinds and fixed node visibility
- Updated save pen to fix a possible issue
- Update key question dialog to not be empty when opened.
- Updated some text boxes to use the full space available
- Fixed issues relating to the StoryCAD Rebrand
- Fixed an issue where the cast list would not properly update
- Fixed issue with logging
- Fixed bug where opening a story from a different date format would crash

#### Release 2.10.0.0
As of April 18th 2023, we have released version 2.10.0.0
This release simply rebrands StoryBuilder to StoryCAD.

#### Release 2.9.0.0

As of April 3rd 2023 we've rolled out Release 2.9.0.0.

Mostly a fix release, the changes include:

###### Preferences refactoring (##524)

Code cleanup to standardize use of Preferences data
throughout the app.

###### Enhanced Drag and Drop functionality (##526)

Using your pointing device, you can now move a story node and drop it as a child of any other story node. Fine positioning within the list of children can be done with the move buttons.

###### Update Move button methods (##529)

Correct MoveRightIsValid test when CurrentNode is first child of root

Clear StatusMessage at start of all MoveTreeViewItemX methods

###### Update project dependencies (##534)

Updated to latest version of WinAppSDK 1.2

###### Fix Not Connected message on release versions

The Not Connected message was being displayed on release versions of StoryBuilder. This was due to a change in the way the WebView2 runtime is installed. The WebView2 runtime is now installed as a part of the StoryBuilder installation. 

###### Reinforce list loads

Fixed crashes caused by bad copies of lists.ini

#### Release 2.8.0.0

As of March 1st 2023 we are now rolling out 2.8.0.0

###### Create issues from elmah.io GitHub app integration ##132
Elmah.io now uploads new erros to GitHub so that everyone can now
see error logs.

###### Launch StoryCAD from .stbx file (##94)

.STBX files are now associated with StoryCAD meaning that, now users can double
click .STBX files and it will automatically open in StoryCAD.

#### Release 2.7.0.0

As of February 9, 2023, we've rolling out Release 2.7.0.0.

###### New Features

######## ARM64 Support (##109, ##481)

StoryCAD now builds for three platforms: Intel x64 and x86, and ARM64.
This supports Windows 11 running on ARM-based systems such as Surface Pro X.

######## Improvements to Conflict Builder (##484)

We've added  additional subcategories of criminal conflucts and
expanded existing category/subcategories with more examples.

######## Problem Category (##491)
We've added a new field to the Problem class, ProblemCategory. 
This is a non-editable drop-down list (SfComboBox) which describes
the purpose of the problem in terms of story structure.

######## Unit testing / additional unit tests (##17, ##492)

We've got the StoryCADTest executing test scripts from Test Explorer.
It's not yet running as a part of PR review prior to merge. We'll get 
that added in the next (2.8.0.0) release.

As time permits, tests need to be generated to fill in code coverage, 
especially for user interactions.

###### Bug fixes

######## Re-add Listview for Scene Purpose (##478)

######## Fix label on Setting Page for Setting Summary (##488)

######## Fix error when opening files (##489)

###### Ongoing and Deferred Issues

######## User manual updates (##487, ##491)

Add write-up for Problem Category with screen shot.

Fix invalid markdown tag on bullet list items.

Add Next/Previous navigation.

######## Produce first newsletter (##)

We are up on MailChimp, and have content drafted for the first newsletter, 
which is the 2.6.0.0 changelog and the 2.7 roadmap. We also have additional
posts drafted, and a template with logo for the newsletter is being 
put together. 

Rather than one monthly newsletter, we'll be producing several smaller 
newsletters throughout the month: a newsletter for each new release,
and several newsletters with articles and tips.

Blog posts on StoryCAD.org and the newsletters will contain similar
content.

#### Release 2.6.0.0

As of January 24, 2023, we've rolled out Release 2.6.0.0.
This release was late due to a number of issues as well
as holiday and school schedules.

###### New Features

######## Expand market area to all stores

We've expanded our distribution to include all
English-based countries in our  Microsoft Store
market.

(This was done in the 2.5.1.0 point release, which
fixed issues with AutoSave and the WebView2 runtime, 
used in our WebPage Story Element.)

######## Remove SfComboBox (##464) 

This change results in the replacement of SyncFusion's
SfComboBox with the Microsoft ComboBox. This will make it 
easier for developers to work with the codebase because they
won't need their own SyncFusion licenses in order to do so.

######## Back out remove SfComboBox (##477)

We rolled back the SfCombobox replacement due to problems
with the Microsoft ComboBox control; we were getting bind
failures. This is still under investigation but we note
that there appear to be quite a few open problems relating
to ComboBox on the WinUI GitHub.

######## Purpose of Scene rewrite (##457)

Purpose of Scene was also an SfComboBox, because it allows
the selection of multiple values (a scene should do more than
one thing.) It's been converted to a custome control based on
a Listview with radio buttons. Since this change doesn't
depend on the Microsoft ComboBox, we're retained it.

######## Revise Character Relationships 'Create a new relationship' (##458)

Previously, the list of character Relations (relationship types,
such as father -> son or boss -> employee) was fixed. This
doesn't work, since the number of possible relastionships
is large and dynamic. The list was made editable so that users
can add their own relationship types.

######## Implement a Print Manager for printed reports (##157)

We added a Print Manager as a part of the Print Report menu.
This allows a user to select a printer and specify its 
StoryCAD print report options. 

As of 11/15/2022 (with WinAppSdk 1.2) the Print Manager is available and works
with Windows 11 but doesn't work with Windows 10. 
We disable the Print Manager with a status message if
the if the user is not on Windows 11. Windows 10 users
can continue to set their default printer before generating
print reports.

We'll continue to track the status of a fix for Windows 10
users.
 
######## Codebase Cleanup (##439) (ongoing)

We're continuing to work on refactorings to
improve our codebase and will continue to do so
indefinitely.

###### Bug fixes

The following bugs were addressed in this release:

######## Update Deletion Service to catch potential errors (##470, ##472)
######## Track Version in .STBX file (##469)
######## Improve filename checking (##471)
######## Add system info to log (##473)
######## Remove cached deferred write (##474)
######## Autosave fixes (##476)
######## Fix to SaveAs (##475)

#### Release 2.5.1.0

Fixed some issues with Autosave
Fixed Issues when installing WebView Runtime.

#### Release 2.5.0.0

As pf December 1, 2022, we rolled out Release 2.5.0.0 for general 
distribution via the Microsoft Store.

######## Revised right-click menu

The flyout menu changes distributed with Release 2.4.0.0 had an
issue which was cleaned up in a point release (2.4.1.0) and 
subsequently in this release. 

######## Codebase cleanup

We're continuing to clean up our codebase. This release includes significant
cleanup and simplification of our viewmodels, which are among our most 
complex classes. We're also making checks for clean code a part of our
Pull Request process, in ordere to improve ongoing quality. Code cleanup
will continue in the next release as well.

######## Remove Id property  

This particular code cleanup has been on our issue list for a long time,
but was repeatedly bypassed because (a) it did no harm and (b) it was
complicated. But hey, cleanup. It's done now.

######## Update to .NET 7

Updated to .NET 7 is the successor to Microsoft .NET cross-platform 
.NET framework, .NET 6. .NET 7 is primarily a performance release and
includes improvements in ARM64 support and desktop app support.

.NET 7 supports a feature, IL Trimming, which will significantly reduce
the size of applications (and thus speed up both download time and load
time at execution.) The benefits of IL Trimming. These benefits are not
yet present for packaged MSIX WinUI 3 applications like ours, but should 
be forthcoming.


###### Add an 'Overview and Main Story Problem' template for new story creation

This template focuses on the Story Problem and ties the Overview node's Premise
to one Problem node by making it the Story Problem. It also adds Protagonist
and Antagonist Character nodes to the problem.

######## Master Plots should be copied as a Problem rather than Scene

This issue was listed in the 2.5 Roadmap as 'self explanitory.' It 
isn't, exactly. The MasterPlot tool will now copy a masterplot as a
Problem story node with its description and notes contained in the problem
if it has one 'scene' in its termplate, and as a Problem with series of Scene
nodes if it's one of the 'story structure' masterplots such as Three Act Play
or Hero's Journey. Previously MasterPlots wer added as series of scenes
in all cases.

######## AutoSave improvements

Fixed a possible slowdown with AutoSave
Increased the maximum time between autosaves from 30 to 60 seconds and minimum from 5 to 15 seconds.

###### Remarks

Several things planned for this release didn't make it and will be rolled forward
to future releases. These include


#### Release 2.4.0.0

As of Novermber 13, 2022, we've rolled out Release 2.4.0.0 for general distribution 
via the Microsoft Store. 

2.4 contains the following new/changed features:

The WebPage story element research tool introduced in Release 2.3 now has a Preferences
tab which allows you pick the search engine to use when locating the web resource you're 
looking for. 

###### UI Improvements


######## Revised right-click menu

The flyout menu is a shortcut used to add new Story Elements and several other functions.
Jake's rewriten the flyout menu to make it much more user-friendly. 

######## Highlight in-process node

A StoryCAD outline is a tree of Story Elements which are displayed in the Navigation Pane. 
Clicking (or touching) a Story Element node on the Navigation Pane displays that 
Story Element’s content in the Content Pane. A node can also be right-clicked to
display a commandbar flyout context menu. The current (clicked) node is highighted
in the Navigation Pane, but the highlight is not very visible. Right-clicking a node
momentarily highlights it but with the light theme it's almost impossible to see.

The Navigation Pane now displays the current node and the right-mouse clicked node
(used for a target for the flyout menu) using your Display Settings accent colors to 
better highlight where you are.

######## Cast List revamp

Scene Story Elements contain a Cast List control which, as its name implies,
is used to list the characters in a scene. The Cast List control is based
on a ListView and requires a convoluted set of interactions to add and
delete cast members using a second control. We've simplified this by allowing you
to switch between a list of all characters, with a checkbox by each name to
add or remove the character to this scene's cast, or a list of just the scene's members.

######## Progress indicator

Several tasks, notably printing reports and loading installation data, may take 
a bit of time. We've added a progress indicator as an indicator that the app
hasn't frozen. 

###### Default Preferences

Some Preferences data, such as the default outline and backup folder locations,
have have been updated with some defaults (including preserving previous versions' 
values) when installing StoryCAD.

######## Codebase cleanup

Actively maintained programs tend to accumulate cruft over time. This release we've
started a process of addressing this by working through the codebase and removing
duplicate and unused code, conforming to some newly-set naming conventions,
and making other improvements. This is a long-term process, which will continue in
future releases, but we're making progress.

######## Bug fixes:

StoryCAD is a new product, and our number-one priority remains bug fixes and improvements.
Some specific fixes in this release include:

* Fixed a problem where the node wrapping Preference setting was not persisting.

* Fixed the app freezing when generating large reports. A progress indicator is now shown.

* Fixed a number of crashes relating to tool use and transitioning from one user action to 
another.

###### Windows App SDK Api 1.2

We've updated to the latest version of the Windows App SDK (1.2).

#### Release 2.3.0.0

As of October 3, 2022, we've rolled out Release 2.3.0.0 for general distribution 
via the Microsoft Store. 

2.3 has one significant new feature, the addition of researach tools. These take
the form of two new Story Element types which can be added as nodes to your outlines:
WebPage and Note nodes.

WebPage nodes are used to store links to web pages. They're implemented using the
WebView2 control, which is built on the Edge browser. A WebPage node can use search
to find a page and will persist that page's URL in your outline so that when you 
navigate to it the page is loaded and displays in the node's content area.

Note nodes are used to store text notes. They're implemented using the RichEditBox.
Major Story Element nodes (Story Overview, Problem, Character, Setting and Scene) all
have a Notes tabes which can be used to store notes about that particular Story Element,
but the new Note nodes are intended to be used to store notes about any topic  you
wish.

StoryCAD is a new product, and our priority remains bug fixes and improvements.
2.3 is primarily a fix release, as will be future short-term releases. Some
specific fixes in this release include:

######## Implemented Single Instancing

StoryCAD now uses Single Instancing. If the app is already open and you launch 
it again, the existing instance will be brought to the foreground rather than having
a new instance launched. While the ability to edit more than one outline at one time
has its uses, it can also cause problems. For example, if you have two instances of
StoryCAD open and you edit the same Story Element in both instances, the changes
from one instance will overwrite the changes from the other instance. Single Instancing
prevents that from happening.

######## Codebase cleanup

Actively maintained programs tend to accumulate cruft over time. This release we've
started a process of addressing this by working through the codebase and removing
duplicate and unused code, conforming to some newly-set naming conventions,
and making other improvements. This is a long-term process, which will continue in
future releases, but we're making progress.

######## User Manual and sample updates

We've updated the User Manual and several sample outlines to improve the documentation
and to reflect changes in the app. As with the codebase cleanup, this will be an
ongoing process. The User Manual changes are still mostly structural, and we're
well aware that line and copy edits are needed. We'll be working on those in future
releases.

######## Bug fixes:

* Fixed a bug preventing cast information from being saved.
* Fixes several issues causing progdram crashes.
* Fixed some issues relating to topics. Besides in-line topic
  data (topic/subtopic/notes), it's possible for a topic to 
  launch Notepad to display a file. This fix has that working.
* Fixed some issues with tracking changes.

https://github.com/StoryCAD-org/StoryCAD-2/compare/2.1.2.0...2.2.0.0

#### Release 2.2.0.0

* As of August 31st, 2022, we've rolled out Release 2.2.0.0. We have now opened the app up to general distribution via the Microsoft Store. 
* This release has a few fixes and improvements whilst implementing new features such as the Narrative Editor.
* Optimized code
* Fixed accidental spell checking on the email box.
* Added a new menu called Narrative Tool to make editing
the narrator view easier.
* Fixed Icons on certain nodes not showing up.
* Added a prompt to open the quick start menu when opening
StoryCAD for the first time.
* Updated the Danger Calls sample story and the tutorial in the User Manual.
* Fixed an error where the story said it was saved when it really wasn't.
* Made some minor changes to the contents of the comboboxes.
* New story overview nodes are now called the name of the story instead
of just working title.

https://github.com/StoryCAD-org/StoryCAD-2/compare/2.1.2.0...2.2.0.0

#### Release 2.1.2.0

Fixed syncfusion licensing issue.

#### Release 2.1.1.0

Fixed scaling issues.

Updated dependencies.

#### Release 2.1.0.0

As of July 29, 2022, we've rolled out Release 2.1.0.0. This is the 
completion of a major milestone, distributing StoryCAD 
via Windows Store direct link. 

This release has a ton of fixes, adds our privacy policy, and contains documentation improvements.
A point release, 2.1.1.0, on August 1, 2022, fixed a scaling issue we missed in 2.1.0.0. 
We allow Windows Store client installations for any Windows user who has a link to the download, from a
link through the website (https://StoryCAD.org) and other channels. 

Added a roadmap

Added Autosave

Updated some combobox choices

StoryCAD will now show the changelog on an update.

Revamped Relationship layout

Updated manual

Added StoryCAD Server support.


#### Release 2.0.0.0

Fixed warning by @Rarisma in ##311

Updated preferences UI

Fixed issues with Reports menu by @Rarisma in ##317

Datalogging by @terrycox in ##321

Fixed issues with Moving buttons

Fixed issues with generated reports by @Rarisma in ##323

Updated dependencies by @Rarisma in ##322

Fixed some crashes and inconsistencies in report naming by @Rarisma in ##335

Make startup quicker by @Rarisma in ##336

Update packages by @Rarisma in ##340

Fixed CharacterName, ProblemName and SceneName control issues by @Rarisma in ##341

User is now prevented from opening the add relationships menu if there aren't any prospective partners
Get ready for the store by @Rarisma in ##344

Fix DotEnv Requirement by @Rarisma in ##346

Reverted AutoComplete controls to SyncFusionComboboxes by @Rarisma in ##352

Added some new keybinds and fixes report issues relating to outertraits by @Rarisma in ##355

https://github.com/StoryCAD-org/StoryCAD-2/compare/2.0.14.0...2.0.0.0

#### Release 2.0.14.0

Tweak UI

Fixed logging

Fixed bugs

#### Release 2.0.13.0

Fixed Numerous smaller bugs by @Rarisma in ##258

Fixed crash caused by teaching tip by @terrycox in ##259

Fix Dark Mode Coloring by @Rarisma in ##265

Fix some fields not saving by @Rarisma in ##282

Updated about pageby @Rarisma in ##283

Updated search expirence by @Rarisma in ##287

Fix Releationship display issue by @Rarisma in ##291

Correct Setting Combobox by @terrycox in ##292

Fix Logging Error by @Rarisma in ##294

Added examples by @terrycox in ##293

Updated Automated Release

#### Release 2.0.12.0

Added privacy policy (Read it here https://github.com/StoryCAD-org/StoryCAD-2/blob/master/PRIVACY_POLICY.TXT)

Fix some issues with Cast Members

Added some tooltips

Updated repository documentation

Fixed issue with content persisting when a new story is loaded

Fixed some issues regarding dark mode

Fixed some grammar regarding search results

Added tool tip to the edit icon

Updated Scene purpose to allow multiple values

When StoryCAD is opened, the file open menu is shown

Cleaned up code and removed unused values

Updated logging

Updated samples to fix typos and grammar

Fixed bug which would cause the report printer to make tons of reports

Renamed Literary device to Literary Technique

Fixed saveas being broken

Updated list of countrys to be much more complete

Implimented autobuilds


Made sizing better, especially for screens using scaling

Fixed issue releating to content in structure tab not saving

Fixed wording in problem page

Fixed issue with locale and season showing the wrong values

Fixed crash relating to moving certain elements

Fixed crash releating to adding cast to stories

Removed quotes and characterization aids

Fixed error caused when cancling dramatic situations

Updated dependencies

Updated File menu to show the last edited date, and the path on hover

Fixed bug which caused stock scenes to insert two nodes instead of one, and now shows default when opened.

Fixed bug which caused text to be at the right of the screen

Removed need for keys to be read through environment variables, this new system does not require the user to ddo anything

Improved logging clarity

Fixed bug which would cause crashes with teaching tips

Clicking on the save pen will now save your file





