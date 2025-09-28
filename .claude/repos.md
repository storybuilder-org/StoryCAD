# Repository Overview

This document maps the repositories in the development environment. Each repository should have its own README.md with detailed information.

## Primary Repository

### StoryCAD
- **Path**: `/mnt/d/dev/src/StoryCAD/`
- **README**: [README.md](/mnt/d/dev/src/StoryCAD/README.md)
- **Summary**: Free, open-source Windows outlining tool for fiction writers ("CAD for fiction writers")
- **Key Files**: `CLAUDE.md`, `devdocs/`, `StoryCAD.sln`

## Related Repositories

### StoryBuilderCollaborator
- **Path**: `/mnt/d/dev/src/StoryBuilderCollaborator/`
- **README**: [README.md](/mnt/d/dev/src/StoryBuilderCollaborator/README.md)
- **Summary**: AI collaboration service/library for StoryCAD

### ManualTest
- **Path**: `/mnt/d/dev/src/ManualTest/`
- **README**: [README.md](/mnt/d/dev/src/ManualTest/README.md)
- **Summary**: User documentation for StoryCAD (Just the Docs)

### StoryCADConverter
- **Path**: `/mnt/d/dev/src/StoryCADConverter/`
- **README**: [README.md](/mnt/d/dev/src/StoryCADConverter/README.md)
- **Summary**: Conversion tool for StoryCAD file formats

### StoryCAD-Legacy-STBX-Conversion-Tool
- **Path**: `/mnt/d/dev/src/StoryCAD-Legacy-STBX-Conversion-Tool/`
- **README**: [README.md](/mnt/d/dev/src/StoryCAD-Legacy-STBX-Conversion-Tool/README.md)
- **Summary**: Legacy .stbx file conversion utility

### StbxDeduper
- **Path**: `/mnt/d/dev/src/StbxDeduper/`
- **README**: [README.md](/mnt/d/dev/src/StbxDeduper/README.md)
- **Summary**: Deduplication tool for .stbx files

### ListStoryCADIssues
- **Path**: `/mnt/d/dev/src/ListStoryCADIssues/`
- **README**: [README.md](/mnt/d/dev/src/ListStoryCADIssues/README.md)
- **Summary**: GitHub issue management tool

## Supporting Repositories

### API-Samples
- **Path**: `/mnt/d/dev/src/API-Samples/`
- **README**: [README.md](/mnt/d/dev/src/API-Samples/README.md)

### WinUIChat
- **Path**: `/mnt/d/dev/src/WinUIChat/`
- **README**: [README.md](/mnt/d/dev/src/WinUIChat/README.md)

### UNO Samples
- **Path**: `/mnt/d/dev/src/UNO Samples/`
- **README**: [README.md](/mnt/d/dev/src/UNO Samples/README.md)

## Documentation Repositories

### just-the-docs
- **Path**: `/mnt/d/dev/src/just-the-docs/`
- **README**: [README.md](/mnt/d/dev/src/just-the-docs/README.md)

### jtd-template / jtd-template_save
- **Path**: `/mnt/d/dev/src/jtd-template/`
- **README**: [README.md](/mnt/d/dev/src/jtd-template/README.md)

## Other Repositories

### storybuilder-miscellaneous
- **Path**: `/mnt/d/dev/src/storybuilder-miscellaneous/`
- **README**: [README.md](/mnt/d/dev/src/storybuilder-miscellaneous/README.md)
- **Summary**: Private repo with sysadmin material for StoryBuilder (confidential)

### openai
- **Path**: `/mnt/d/dev/src/openai/`
- **README**: [README.md](/mnt/d/dev/src/openai/README.md)

### fabric
- **Path**: `/mnt/d/dev/src/fabric/`
- **README**: [README.md](/mnt/d/dev/src/fabric/README.md)

### fixes
- **Path**: `/mnt/d/dev/src/fixes/`
- **README**: [README.md](/mnt/d/dev/src/fixes/README.md)

### App1
- **Path**: `/mnt/d/dev/src/App1/`
- **README**: [README.md](/mnt/d/dev/src/App1/README.md)

## Repository Relationships

```
StoryCAD (Main App)
├── StoryBuilderCollaborator (AI Features)
├── ManualTest (User Documentation)
├── StoryCADConverter (File Migration)
├── StoryCAD-Legacy-STBX-Conversion-Tool (Legacy Support)
└── StbxDeduper (File Management)
```

## Working Directories

- **Primary Development**: `/mnt/d/dev/src/StoryCAD/`
- **Documentation**: `/mnt/d/dev/src/ManualTest/`
- **Logs**: `/mnt/c/Users/tcox/AppData/Local/Packages/34432StoryBuilder.StoryBuilder_mty98bvf7kaq2/RoamingState/StoryCAD/Logs/`
- **Test Data**: `/tmp/` and `/mnt/c/temp/`

## Notes

- Most repositories follow the StoryBuilder/StoryCAD ecosystem
- StoryCAD is the evolution of the older StoryBuilder application
- Multiple conversion and migration tools exist to support the transition
- Documentation uses Jekyll with Just the Docs theme
- AI features are being integrated through the CollaboratorService