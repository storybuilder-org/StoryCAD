# AI Issue‑Centric Workflow Guide
_For Claude Code, Copilot, Gemini, or any agent helping on StoryCAD GitHub issues._

## 1 · When the agent should act
The agent acts **only when a human explicitly asks**, for example:

> “Please read and process this issue.”

No automatic triggers, webhooks, or YAML workflows are assumed.

## 2 · Read the entire Issue
1. Ingest everything in the Issue body—problem statement, proposed solution, screenshots, etc.  
2. Locate the **Lifecycle** headings, which must appear in this order:  
   - **Design tasks / sign‑off**  
   - **Code tasks / sign‑off**  
   - **Test tasks / sign‑off**  
   - **Final sign‑off**  
3. Work only on the **first** subsection whose task list is still missing or incomplete.

## 3 · Mandatory checklist frame for every Lifecycle subsection
Each subsection must always start and end with these unchecked items
(the agent must never tick them—humans do that):

- [ ] Plan this section  
- [ ] Human approves plan  
  <!-- AI inserts ordered tasks here → `- [ ] Task …` → `- [ ] Task …` etc. -->  
- [ ] Human final approval

The correct order is:
1. Plan this section (agent creates plan)
2. Human approves plan (human reviews and approves)
3. [Generated tasks go here] (agent executes after approval)
4. Human final approval (human tests and signs off)  

## 4 · Agent behavior in a subsection

### 4.1 · Planning phase  
**Condition:** "Plan this section" is **unchecked**

1. Generate an ordered, unchecked task list that fully describes what has to be done in this phase.  
2. Create a markdown document named `[issue_title] #[issue_number].md` (e.g., 'Remove and Replace DataSource #1067.md').  
3. Ask the user where to save the document (which drive and folder).  
4. Include the generated task list and all detailed planning in the markdown document.  
5. Update the issue body by inserting the generated task list between "Plan this section" and "Human approves plan" checkboxes.
6. Add an Issue comment:  
   > Planning for **<section name>** complete. Document saved as `[filename]`. Please review and tick **Human approves plan**.

### 4.2 · Execution phase  
**Condition:** "Plan this section" is **checked** **and** "Human final approval" is **unchecked**

1. Perform the listed tasks in order (e.g., draft design, open PR, add tests).  
2. **Check off each individual task** in the issue body as you complete it to show progress. Continue working through all tasks in the section without stopping—only pause at human approval checkpoints.
3. Document all work, artifacts, and detailed implementations in the markdown document created during planning.  
4. When every task is done, update the markdown document with all completed work and comment:  
   > Work for **<section name>** finished. All details documented in `[filename]`. Please validate and tick **Human final approval**.

## 5 · Progressing to the next phase
After a human checks **Human final approval** for the current section, wait for them to instruct the agent again (“read and process this issue”) and then repeat the process for the next Lifecycle subsection.

## 6 · Pull‑request merge rule
Only merge the pull request linked to the Issue after completing every task in Final sign-off and after the Human final approval checkbox is ticked.

_Task checkboxes remain in the Issue body for progress tracking. All detailed work, supporting artifacts (design summaries, code snippets, test results), and documentation should be included in the markdown document rather than as Issue comments._

## 7 · Examples

### Example: Planning Design Tasks
When the agent sees:
```markdown
### Design tasks / sign-off
- [ ] Plan this section
- [ ] Human approves plan
- [ ] Human final approval
```

The agent should:
1. Generate tasks like:
   - [ ] Analyze current architecture
   - [ ] Design new component structure
   - [ ] Create class diagrams
   - [ ] Define API contracts

2. Update the issue body to:
```markdown
### Design tasks / sign-off
- [ ] Plan this section
- [ ] Human approves plan
- [ ] Analyze current architecture
- [ ] Design new component structure
- [ ] Create class diagrams
- [ ] Define API contracts
- [ ] Human final approval
```

3. Create design document and comment: "Planning for Design tasks complete. Document saved as `Remove DataSource #1067.md`."

### Example: Checking Off Completed Tasks
As work progresses:
```markdown
### Code tasks / sign-off
- [x] Plan this section
- [x] Human approves plan
- [x] Convert StoryModel to ObservableObject ← Agent checks this after completing
- [ ] Add CurrentView property
- [ ] Human final approval
```

### Example: Using gh CLI to Update Issues
```bash
# Get current issue content
gh issue view 1067 --repo storybuilder-org/StoryCAD --json body -q .body > current-issue.md

# Edit the file to add tasks or check off completed ones
# Then update the issue
gh issue edit 1067 --repo storybuilder-org/StoryCAD --body-file current-issue.md
```
