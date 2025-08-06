# Feature-Specific Test Plan Framework

## Concept
Generate tailored manual test plans for each feature request by:
1. Analyzing what the feature changes
2. Extracting relevant tests from master suite
3. Adding new tests for new functionality
4. Creating a focused 15-30 minute test plan

## How It Works

### Input: Feature Request
```markdown
Feature: Add word count to Story Overview
Changes: 
- Modified: StoryOverviewViewModel.cs
- Modified: StoryOverview.xaml
- Added: WordCountService.cs
```

### Output: Tailored Test Plan
AI analyzes the feature and generates:
- Relevant existing tests (filtered from master)
- New tests for the feature
- Regression tests for affected areas

---

## Template for Feature-Specific Test Plans

```markdown
# Test Plan for Feature: [Feature Name]
**PR/Issue**: #[Number]
**Estimated Time**: [15-30 minutes]
**Generated**: [Date]

## Changes Summary
- [List of files/components changed]

## Relevant Existing Tests
[Filtered from master test suite]

## New Feature Tests
[Specific to this feature]

## Regression Tests
[Areas that might be affected]

## Out of Scope
[What we're NOT testing from the full suite]
```

---

## Example: Feature Request #1234 - Add Character Word Count

### Generated Test Plan

```markdown
# Test Plan for Feature: Add Character Word Count
**PR/Issue**: #1234
**Estimated Time**: 20 minutes
**Generated**: 2025-01-08

## Changes Summary
- Modified: CharacterViewModel.cs (added WordCount property)
- Modified: Character.xaml (added word count display)
- Modified: StatusBar.xaml (added total word count)
- New: Services/WordCountService.cs

## Relevant Existing Tests (10 minutes)

### From Core Story Elements:
✅ CE-001: Create Character (verify word count shows)
✅ CE-006: Delete and Restore (verify count updates)

### From Core Navigation:
✅ CN-001: Tree Navigation (verify count updates on selection)

### From File Operations:
✅ CF-002: Save Story (verify word count persists)
✅ CF-003: Open Story (verify word count loads)

## New Feature Tests (8 minutes)

### FT-001: Character Word Count Display
**Steps:**
1. Create new character
   **Expected:** Word count shows "0 words"
2. Add text to Backstory field
   **Expected:** Count updates in real-time
3. Add text to Physical description
   **Expected:** Count includes all text fields

### FT-002: Status Bar Total Count
**Steps:**
1. Select Story Overview
   **Expected:** Status bar shows total word count
2. Add text to multiple characters
   **Expected:** Total updates correctly
3. Delete a character
   **Expected:** Total decreases

### FT-003: Word Count Accuracy
**Steps:**
1. Enter "Hello world" in description
   **Expected:** Shows "2 words"
2. Enter text with punctuation and numbers
   **Expected:** Counts correctly
3. Enter Unicode/emoji
   **Expected:** Handles gracefully

## Regression Tests (2 minutes)

### RT-001: Performance Check
**Steps:**
1. Open large outline (100+ characters)
   **Expected:** No lag when selecting characters
2. Type rapidly in text field
   **Expected:** Count updates without freezing

## Out of Scope
- Tools menu tests (not affected)
- Drag and drop tests (not affected)
- Reports (unless word count added to reports)
- Keyboard shortcuts (unless new shortcuts added)
```

---

## Automation Approach

### 1. Manual Generation (Current)
Developer/Tester reads PR and creates test plan using template

### 2. Semi-Automated (Near-term)
```python
# test_plan_generator.py
def generate_test_plan(pr_number):
    changes = get_pr_changes(pr_number)
    
    # Map changed files to test areas
    test_areas = map_changes_to_tests(changes)
    
    # Filter master test suite
    relevant_tests = filter_tests(master_suite, test_areas)
    
    # Generate new tests based on feature
    new_tests = generate_new_tests(pr_description)
    
    # Create markdown test plan
    return create_test_plan(relevant_tests, new_tests)
```

### 3. AI-Powered (Future)
```markdown
Prompt: Generate a test plan for this PR:
- Changed files: [list]
- Feature description: [text]
- Master test suite: [attached]
```

---

## Test Mapping Rules

### File Changes → Required Tests

| Changed File Pattern | Include Tests | Add New Tests |
|---------------------|--------------|---------------|
| *ViewModel.cs | Navigation, Save/Load | Property binding |
| *.xaml | UI interaction, Display | Visual verification |
| *Service.cs | Integration, Performance | Service-specific |
| Shell.xaml | Smoke, Navigation | Menu/toolbar items |
| FileOperations/* | All File Operations | File format |
| DragDrop/* | All Drag & Drop | Drop zones |

### Component Impact Matrix

| Component | Affects | Must Test |
|-----------|---------|-----------|
| Navigation Tree | All navigation | Tree operations |
| Status Bar | Display only | Visual check |
| Content Pane | Element editing | Save/Load |
| Tools Menu | Tool features | Specific tool |
| File Service | All file ops | Full file suite |

---

## Benefits

### Efficiency
- **Full Test**: 3-4 hours → **Feature Test**: 15-30 minutes
- **Targeted**: Only test what could break
- **Faster Feedback**: Test immediately after implementation

### Quality
- **Comprehensive**: Nothing missed for the feature
- **Documented**: Clear record of what was tested
- **Reproducible**: Same tests for similar features

### Scalability
- **Reusable**: Test fragments combine for different features
- **Learnable**: AI can learn patterns over time
- **Adaptable**: Easy to modify for similar features

---

## Implementation Steps

### Phase 1: Manual Templates (Now)
1. Create templates for common feature types
2. Train team to use templates
3. Store feature test plans with PRs

### Phase 2: Test Fragment Library
1. Break master suite into reusable fragments
2. Tag fragments with component/feature areas
3. Create mapping rules

### Phase 3: Automation
1. Script to analyze PR changes
2. Auto-generate draft test plan
3. Human review and approval

---

## Example Feature Types and Test Patterns

### 1. New UI Element
- Navigation to element
- Display verification
- Interaction testing
- Save/Load persistence
- Keyboard accessibility

### 2. New Tool/Dialog
- Tool menu access
- Dialog display
- Function verification
- Cancel/OK behavior
- Settings persistence

### 3. Data Model Change
- Migration from old format
- Save/Load integrity
- Backward compatibility
- Performance impact
- UI binding updates

### 4. Performance Optimization
- Baseline measurement
- Improvement verification
- Regression check
- Edge cases (large files)

### 5. Bug Fix
- Reproduction steps
- Fix verification
- Regression testing
- Related functionality

---

## Test Plan Storage

Store feature test plans alongside code:
```
/Tests
  /ManualTests
    /Features
      /2024
        FR-1234-WordCount.md
        FR-1235-DarkMode.md
      /2025
        FR-1240-MacSupport.md
```

Or in PR comments:
```markdown
## Test Plan
[Generated test plan here]

## Test Results
- [ ] All tests passed
- [ ] Issues found: [list]
```

---

## Metrics and Improvement

Track:
- Time to generate plan vs manual creation
- Test execution time
- Bugs found vs missed
- Test reuse percentage

Improve:
- Refine mapping rules based on misses
- Add new test fragments as needed
- Update patterns based on common features