# StoryCAD Roadmap

#### Last Updated: 2023-03-01

Our next Release will be 2.9.0.0. 

We expect that this release will include the following features:

## New Features

### Refactor Preferences (#320)
The preferences system is in dire need of refactoring for the following reasons
1) Preferences processing is scattered
2) Prefences methods aren't consistent
3) change tracking are not manged consistently
4) one instance of preferences is used widely but not consistently

### Fix Drag n Drop is broken (#312)
Drag and drop functionailty appears to broken in its implementation and needs to be fixed 
as it does not properly update the tree when nodes are dragged and dropped.