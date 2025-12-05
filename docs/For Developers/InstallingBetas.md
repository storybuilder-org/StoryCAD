---
title: Installing Beta Versions of StoryCADLib
layout: default
nav_enabled: true
nav_order: 118
parent: For Developers
has_toc: false
---
### Installing Beta Versions of StoryCAD.
-  Navigate to the actions tab
- Select the AutoBuilder or Build and Release Tab
- Find the build you want and click on it
- Download StoryCAD zip file you want
- Unzip and follow the included instructions.
## Installing Beta Versions of StoryCADLib via Local NuGet Repository

Beta versions of StoryCADLib can be installed using NuGet by setting up a local repository. Follow these steps:

### Setting Up a Local NuGet Repository

1. **Create a local folder** to store NuGet packages (e.g., `C:\Users\MyUsername\Documents\NugetRepository\`).
2. **Register the folder as a NuGet source** by running:

   ```bash
   dotnet nuget add source C:\Users\MyUsername\Documents\NugetRepository\
   ```

### Downloading and Installing Packages

* Download `.nupkg` and optionally `.snupkg` (symbol) files from the **GitHub Actions** or **Releases** tab.
* Place these files into the local repository folder you created.

The `.snupkg` files, while optional, are highly recommended for improved debugging and issue reporting.

Once the packages are in place, install StoryCADLib as usual through your NuGet package manager.

**Important:** For production use, only use officially released builds of StoryCADLib.
