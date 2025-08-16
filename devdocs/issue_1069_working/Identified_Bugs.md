# Identified Bugs During Test Coverage Work
## Issue #1069 - 2025-01-15

## OutlineService Bugs

### 1. AddRelationship - Duplicate Relationships Not Prevented
**Location**: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Services/Outline/OutlineService.cs:416`

**Issue**: The method does not check if a relationship already exists before adding it, allowing duplicate relationships to be created.

**Expected Behavior**: Should check if a relationship between the two characters already exists and not add a duplicate.

**Test**: `AddRelationship_DuplicateRelationship_ShouldNotAddDuplicate`

**Current Code**:
```csharp
RelationshipModel relationship = new(recipient, desc);
((CharacterModel)sourceElement).RelationshipList.Add(relationship);
```

**Suggested Fix**:
```csharp
var sourceCharacter = (CharacterModel)sourceElement;
// Check if relationship already exists
if (!sourceCharacter.RelationshipList.Any(r => r.PartnerUuid == recipient && r.RelationType == desc))
{
    RelationshipModel relationship = new(recipient, desc);
    sourceCharacter.RelationshipList.Add(relationship);
}
```

### 2. AddCastMember - Null Reference Before Null Check
**Location**: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Services/Outline/OutlineService.cs:462`

**Issue**: The method accesses `source.Uuid` in the log statement before checking if `source` is null, causing a NullReferenceException instead of the intended ArgumentNullException.

**Expected Behavior**: Should check for null source before accessing any of its properties.

**Test**: `AddCastMember_WithNullSource_ThrowsException`

**Current Code**:
```csharp
_log.Log(LogLevel.Info, $"AddCastMember called for cast member {castMember} on source {source.Uuid}.");
if (source == null)
{
    throw new ArgumentNullException(nameof(source));
}
```

**Suggested Fix**:
```csharp
if (source == null)
{
    throw new ArgumentNullException(nameof(source));
}
_log.Log(LogLevel.Info, $"AddCastMember called for cast member {castMember} on source {source.Uuid}.");
```

### 3. ConvertProblemToScene - Child Node Names Not Preserved
**Location**: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Services/Outline/OutlineService.cs:495`

**Issue**: When converting a Problem to a Scene, child nodes are moved but their Name property is not preserved/updated.

**Expected Behavior**: Child nodes should maintain their Name property after being moved to the new Scene node.

**Test**: `ConvertProblemToScene_WithChildren_MovesChildren`

**Impact**: Child elements appear with incorrect or missing names in the tree after conversion.

### 4. ConvertSceneToProblem - Child Node Names Not Preserved
**Location**: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Services/Outline/OutlineService.cs:559`

**Issue**: When converting a Scene to a Problem, child nodes are moved but their Name property is not preserved/updated.

**Expected Behavior**: Child nodes should maintain their Name property after being moved to the new Problem node.

**Test**: `ConvertSceneToProblem_WithChildren_MovesChildren`

**Impact**: Child elements appear with incorrect or missing names in the tree after conversion.

### 5. FindElementReferences - Null Reference When Element Not Found
**Location**: `/mnt/d/dev/src/StoryCAD/StoryCADLib/Services/Outline/OutlineService.cs:317-322`

**Issue**: The method calls `StoryElement.GetByGuid(elementGuid)` which can return null, but then immediately accesses `elementToDelete.Node` without null checking, causing NullReferenceException.

**Expected Behavior**: Should check if elementToDelete is null and handle appropriately (throw ArgumentException or return empty list).

**Tests**: All FindElementReferences tests fail due to this bug

**Current Code**:
```csharp
StoryElement elementToDelete = StoryElement.GetByGuid(elementGuid);
if (StoryNodeItem.RootNodeType(elementToDelete.Node) == StoryItemType.TrashCan)  // NullRef here
```

**Suggested Fix**:
```csharp
StoryElement elementToDelete = StoryElement.GetByGuid(elementGuid);
if (elementToDelete == null)
{
    throw new ArgumentException($"Element with GUID {elementGuid} not found");
}
if (StoryNodeItem.RootNodeType(elementToDelete.Node) == StoryItemType.TrashCan)
```

## Summary
- 5 bugs identified in OutlineService
- All bugs have failing tests that document the correct expected behavior
- Tests are written to expect the correct behavior, not the buggy behavior