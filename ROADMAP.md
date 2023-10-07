# StoryCAD Roadmap

#### Last Updated: 2023-10-07 (yyyy/mm/dd)
StoryCAD 2.13.0.0 Roadmap

In StoryCAD 2.13 we are focusing on a mid-November release date that includes the following features: 

#### Improve testing and integrate it into Auto/ReleaseBuild (#494)
StoryCAD has previously had some regressions in its code causing some bugs that were previously patched
to reappear. Tests should prevent any codebase regressions from going unnoticed and subsequently unpatched.
We are currently working to get tests in a working state as this requires changes to the StoryCAD codebase
to properly function within GitHub Actions and to function consistently.

#### Revise Install service (#656)
StoryCAD Copies some files to the users disk such as samples and some list/combobox content.
We believe that some if not all of this is unnessicary and could instead revise install service to simply
read the files from within StoryCAD instead of being forced to copy them out to the disk first. This
should benefit load times massively, espeically on updates or first runs as Install Service is forced
to replace all files.

#### Provide separate First Name, Last Name fields on signup (#525) 
StoryCAD currently asks for the users full name on sign up, however this should be two separate textboxes
one for first name and one for the last name.

#### Add a Preference to expressly specify the Application theme. (#624)
StoryCAD currently does not allow users to change the app theme at run time and instead requires the user
to change the global system theme and reload the app, this should be simplified to change in real time
and instead allow a user to select Light, Dark, Auto in preferences.

#### Correct notes placement on samples (#538)
Some StoryCAD Samples have notes in the wrong place, setting a bad example on how to use the program.
StoryCAD 2.13 should include an updated set of samples that fixes the placement of notes.

#### Update to .NET 8 and update dependencies
Around Mid-November, Microsoft should release .NET 8 this should improve efficency, performance ect.
As such StoryCAD should include .NET 8 (This is the main reason for the Mid November release date.)
StoryCAD Should also ship with an updated set of dependencies.

#### Fix all reported issues.
Undoubtedly some issues will be found within StoryCAD 2.12, as such StoryCAD 2.13 should ship with 
fixes to any issues found within StoryCAD 2.12.
