---
title: Troubleshooting Cloud Storage Providers
layout: default
nav_enabled: true
nav_order: 110
parent: Miscellaneous
has_toc: false
---
## Troubleshooting Cloud Storage Providers
![](FileOfflineError.png)

Many people today use cloud storage providers like Google Drive, OneDrive, DropBox, etc. Most of the time these work flawlessly however, there can be times when cloud storage providers can cause problems with StoryCAD. 
If you are having issues with StoryCAD and you store your outlines within a cloud storage provider or StoryCAD is showing the message above, then this page will walk you through troubleshooting these problems.

First, it’s important to note that your outline is not lost/damaged or corrupted; it’s just not available locally to StoryCAD. 

With that out of the way, most problems with Cloud Storage Providers can be solved by connecting to the Internet or if you are already connected to the internet, then disconnecting and reconnecting should fix this. Once you have done this, try reopening the file within StoryCAD.

If the problem persists, then open File Explorer and navigate to the folder containing the Outline, if you do not know where the file is located then StoryCAD will display the location of the outline if you hover over the name of the outline within the StoryCAD File Open Menu (pictured below).

![](FileOpenMenu.png)

Once you have found the file in your system, right click the file and find the option to make it available offline/Always keep on device. (The wording and location of the option will vary depending on the cloud storage provider)

![](FileOfflineFix.png)

![](FileOfflineFixSuccess.png)

If the problem persists, then you should download the file from the cloud provider and store it somewhere that will not be synced by the cloud storage provider.

## Cross-Platform File Sharing

If you share StoryCAD outlines between Windows and macOS via cloud storage, be aware of text formatting differences between platforms. See [Platform Differences](../Front%20Matter/Platform_Differences.html) for details and recommendations.

If you have followed all the troubleshooting steps above and the issue persists, please contact us. The Getting Help Page in the front matter section contains information on getting help.
