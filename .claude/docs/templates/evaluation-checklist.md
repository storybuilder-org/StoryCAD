# Evaluation Checklist Template

Use this template during the Evaluate phase of any issue or feature implementation.

## Build & Test Verification
- [ ] Solution builds successfully (zero errors)
- [ ] All automated tests passing
- [ ] Manual testing completed (if UI changes)
- [ ] No regressions introduced

## Requirements Verification
- [ ] **All acceptance criteria met** (check original issue)
- [ ] Edge cases handled appropriately
- [ ] Error handling provides clear user feedback

## Code Quality Review
- [ ] Code review completed (manual or via code-reviewer agent)
- [ ] Project architectural patterns followed (see project documentation)
- [ ] No circular dependencies introduced
- [ ] No anti-patterns or code smells

## Documentation Review
- [ ] Code comments added for complex logic
- [ ] XML documentation for public APIs (if applicable)
- [ ] User documentation updated (if user-facing changes)
- [ ] Technical documentation updated (if architectural changes)

## Knowledge Capture
- [ ] **Lessons learned documented**
- [ ] **Tool/agent effectiveness documented** (if agents used)
- [ ] Project memory files updated if new patterns discovered

## Delivery Verification
- [ ] Changes committed with clear, descriptive messages
- [ ] Pull request created with comprehensive description
- [ ] CI/CD pipeline passing (if configured)
- [ ] Ready for peer review

---

## Agent/Tool Effectiveness Report Template

_Use if specialized agents were used_

```
## Agent/Tool Effectiveness Report

### Agents/Tools Used
| Agent/Tool | Phase | Task | Value Delivered | Issues/Gaps | Time Saved |
|------------|-------|------|-----------------|-------------|------------|
| [name] | [phase] | [task] | [value] | [issues] | [estimate] |

### Overall Assessment
**Most Valuable**: [name and why]
**Least Valuable**: [name and why, or N/A]
**Recommendations for Next Time**: [improvements]
```

---

## Lessons Learned Report Template

```
## Lessons Learned

### What Worked Well
- [Specific practice or approach that was effective]

### What Didn't Work
- [Problem that slowed progress or caused issues]

### Better Approach for Next Time
- [Specific improvement to process or workflow]

### Knowledge for Future Sessions
- [Pattern, gotcha, or insight to remember]
```

---

## Project-Specific Items

**Add project-specific checklist items here**.

**For StoryCAD**:
- [ ] ISaveable registered if applicable (ViewModels)
- [ ] SerializationLock used for state-modifying methods
- [ ] MVVM pattern followed (CommunityToolkit)
- [ ] Stateless services maintained
- [ ] UNO Platform conditional compilation correct (if platform-specific code)

---

## Quick Evaluation for Trivial Changes

For simple changes (typos, one-line fixes):

- [ ] Build succeeds
- [ ] Existing tests still pass
- [ ] Change verified manually
- [ ] Commit with clear message

**Note**: If "trivial" change takes >15 minutes or touches >2 files, use full evaluation.
