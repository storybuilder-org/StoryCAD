# AI-Powered Test Plan Generation

## How to Generate Feature-Specific Test Plans

### For Developers/Testers
Simply provide the AI with:
1. The PR/Issue description
2. List of changed files
3. Reference to the master test suite

### Example Prompt Template

```markdown
Generate a manual test plan for this feature:

ISSUE: #1234 - Add dark mode support

CHANGED FILES:
- Shell.xaml (added theme toggle button)
- Preferences.xaml (added theme selection)
- Services/ThemeService.cs (new)
- ViewModels/ShellViewModel.cs (theme handling)

DESCRIPTION:
Users can now switch between light and dark themes via preferences 
or quick toggle button in toolbar.

MASTER TEST SUITE: [See StoryCAD_Manual_Test_Plan.md]

Generate a focused test plan (15-20 minutes) that includes:
1. Relevant existing tests to re-run
2. New tests for the feature
3. What to skip from the full suite
```

### AI Response Would Generate:

```markdown
# Test Plan for Issue #1234: Dark Mode Support
**Estimated Time**: 20 minutes

## Relevant Existing Tests (8 minutes)

From Core Navigation:
- CN-003: Tab Navigation (verify theme persists across tabs)

From Tools Menu:
- TC-030: Open Preferences (verify theme option present)

From File Operations:
- CF-002: Save Story (verify theme preference saves)

## New Feature Tests (10 minutes)

### DM-001: Theme Toggle Button
1. Click theme toggle in toolbar
   Expected: Theme switches immediately
2. Verify all UI elements update
   Expected: Consistent theme throughout

### DM-002: Preferences Theme Selection
1. Open Tools > Preferences > Theme tab
   Expected: Light/Dark/System options
2. Select each option
   Expected: Theme updates on OK

### DM-003: Theme Persistence
1. Set dark theme and exit
   Expected: Theme saved
2. Restart application
   Expected: Dark theme restored

## Skip These Tests
- All Tools tests except Preferences
- Drag and drop tests
- Reports tests
- Character relationship tests
```

---

## Automation Pipeline

### 1. GitHub Action on PR Creation

```yaml
name: Generate Test Plan
on:
  pull_request:
    types: [opened, edited]

jobs:
  generate-test-plan:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    
    - name: Get PR Changes
      id: changes
      run: |
        echo "files=$(git diff --name-only main...${{ github.head_ref }})" >> $GITHUB_OUTPUT
    
    - name: Generate Test Plan
      run: |
        python generate_test_plan.py \
          --pr "${{ github.event.pull_request.number }}" \
          --files "${{ steps.changes.outputs.files }}" \
          --description "${{ github.event.pull_request.body }}"
    
    - name: Comment Test Plan
      uses: actions/github-script@v6
      with:
        script: |
          github.rest.issues.createComment({
            issue_number: context.issue.number,
            owner: context.repo.owner,
            repo: context.repo.repo,
            body: 'Generated Test Plan:\n\n' + testPlan
          })
```

### 2. Claude Code Integration

```bash
# Command to generate test plan for current branch
claude-code generate-test-plan --branch feature/dark-mode

# Or for specific issue
claude-code generate-test-plan --issue 1234
```

### 3. VS Code Extension

```json
{
  "command": "storycad.generateTestPlan",
  "title": "Generate Test Plan for Current Changes"
}
```

---

## Benefits of AI Generation

### Speed
- Human: 30-60 minutes to create plan
- AI: 30 seconds

### Consistency
- Always includes critical paths
- Never forgets regression tests
- Consistent format

### Intelligence
- Learns from past test plans
- Identifies risky changes
- Suggests edge cases

### Coverage
- Maps code changes to test areas
- Identifies affected components
- Includes performance considerations

---

## Training the AI

### Provide Examples
Feed the AI successful test plans that found bugs:
```
"This test plan for feature X found 3 bugs. 
Learn from this pattern."
```

### Feedback Loop
```
"The generated plan missed testing Y. 
Update your rules to include Y when Z changes."
```

### Domain Knowledge
```
"In StoryCAD, when FileService changes, 
always test backward compatibility."
```

---

## Cost-Benefit Analysis

### Manual Test Plan Creation
- Time: 30-60 minutes per feature
- Quality: Varies by creator
- Coverage: Often incomplete

### AI-Generated Test Plans
- Time: 30 seconds + 5 minutes review
- Quality: Consistent baseline
- Coverage: Comprehensive mapping

### ROI
- 10 features/month × 45 minutes saved = 7.5 hours/month
- Bugs caught earlier = less rework
- Better test coverage = higher quality

---

## Implementation Roadmap

### Month 1: Manual Templates
- Create 5 feature type templates
- Train team on usage
- Track time savings

### Month 2: Semi-Automation
- Script to extract changed files
- Map files to test areas
- Generate draft plans

### Month 3: AI Integration
- Train AI on successful plans
- Integrate with PR workflow
- Measure effectiveness

### Month 4: Full Automation
- Auto-generate on PR creation
- Post as PR comment
- Track test execution