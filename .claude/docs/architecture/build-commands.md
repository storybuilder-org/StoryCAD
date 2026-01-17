# StoryCAD Build & Test Commands

This guide provides commands for building and testing StoryCAD from various environments.

## Build Commands

### From WSL/Claude Code (Recommended for AI Development)

Build entire solution:
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64
```

Build with minimal output:
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64 -v:q
```

Build specific project:
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCADTests/StoryCADTests.csproj -t:Build -p:Configuration=Debug -p:Platform=x64
```

### Standard MSBuild (Windows)

Restore dependencies:
```bash
msbuild StoryCAD.sln /t:Restore
```

Build Debug configuration:
```bash
msbuild StoryCAD.sln /p:Configuration=Debug /p:Platform=x64
```

Build Release configuration:
```bash
msbuild StoryCAD.sln /p:Configuration=Release /p:Platform=x64
```

Build for other platforms:
```bash
msbuild StoryCAD.sln /p:Configuration=Release /p:Platform=x86
msbuild StoryCAD.sln /p:Configuration=Release /p:Platform=arm64
```

### Using dotnet CLI (from Windows Command Prompt/PowerShell)

Note: The dotnet CLI does not work from WSL for WinUI 3 projects. Use Windows Command Prompt or PowerShell instead.

Restore and build:
```cmd
dotnet restore
dotnet build --configuration Release
```

Build specific project:
```cmd
dotnet build StoryCAD/StoryCAD.csproj --configuration Release
```

## Test Commands

### From WSL/Claude Code

**IMPORTANT**: Always build the test project before running tests:
```bash
# Step 1: Build test project (required)
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCADTests/StoryCADTests.csproj -t:Build -p:Configuration=Debug -p:Platform=x64

# Step 2: Run all tests
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll"
```

Or combine into one command:
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCADTests/StoryCADTests.csproj -t:Build -p:Configuration=Debug -p:Platform=x64 && "/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll"
```

Run specific test class:
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll" /Tests:StoryModelTests
```

Run specific test method:
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll" /Tests:FileTests.TestAPIWrite
```

Run multiple test classes:
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll" /Tests:StoryModelTests,OutlineServiceTests
```

Run with verbose output:
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll" /logger:console;verbosity=detailed
```

### Using dotnet CLI (from Windows Command Prompt/PowerShell)

Note: The dotnet CLI does not work from WSL for WinUI 3 projects. Use Windows Command Prompt or PowerShell instead.

Run all tests:
```cmd
dotnet test StoryCADTests/StoryCADTests.csproj --configuration Debug
```

Run with settings file:
```cmd
dotnet test StoryCADTests/StoryCADTests.csproj --settings StoryCADTests/mstest.runsettings
```

Run specific test:
```cmd
dotnet test --filter "FullyQualifiedName~StoryModelTests"
```

## TDD Workflow Commands

The typical TDD cycle from WSL:

1. Write/modify test:
```bash
# Edit test file
vim StoryCADTests/MyNewTests.cs
```

2. Build tests (see compilation errors):
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCADTests/StoryCADTests.csproj -t:Build -p:Configuration=Debug -p:Platform=x64
```

3. Run test (see it fail):
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll" /Tests:MyNewTests
```

4. Implement feature

5. Run test again (see it pass):
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll" /Tests:MyNewTests
```

6. Run all tests to ensure no regression:
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll"
```

## Common Issues

### Path Not Found
If the test DLL path changes due to target framework updates, check the actual path:
```bash
ls -la StoryCADTests/bin/x64/Debug/
```

### Build Errors
For detailed build output:
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64 -v:n
```

### Test Discovery Issues
List all available tests:
```bash
"/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe" "StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll" /ListTests
```

## Environment Variables

For repeated use, consider setting:
```bash
export MSBUILD="/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/MSBuild/Current/Bin/MSBuild.exe"
export VSTEST="/mnt/c/Program Files/Microsoft Visual Studio/2022/Community/Common7/IDE/CommonExtensions/Microsoft/TestWindow/vstest.console.exe"
export TESTDLL="StoryCADTests/bin/x64/Debug/net10.0-windows10.0.22621/StoryCADTests.dll"
```

Then use:
```bash
"$MSBUILD" StoryCAD.sln -t:Build -p:Configuration=Debug -p:Platform=x64
"$VSTEST" "$TESTDLL"
```