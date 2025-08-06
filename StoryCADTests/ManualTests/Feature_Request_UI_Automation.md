# Feature Request: Automated UI Testing for StoryCAD

## Summary
Implement automated UI testing for StoryCAD's smoke test suite to reduce manual testing burden and catch regressions earlier.

## Problem Statement
- Manual smoke testing takes 5+ minutes per build
- Full regression testing requires 3-4 hours
- Risk of human error in repetitive tests
- Testing burden increases with each new feature
- Critical bugs could slip through when testers are unavailable

## Proposed Solution
Automate the 5-minute smoke test using Appium with Windows Driver, focusing on critical user paths that verify the application is stable enough for further testing.

## User Story
**As a** StoryCAD developer  
**I want** automated smoke tests that run on each build  
**So that** I can quickly verify core functionality without manual intervention

## Acceptance Criteria
- [ ] Automated smoke test completes in < 2 minutes
- [ ] Tests cover: app launch, file operations, element creation, save/load
- [ ] Tests run via command line or CI pipeline
- [ ] Failure notifications sent to development team
- [ ] Documentation provided for test maintenance

## Technical Approach

### Phase 1: Minimal Viable Automation
1. Add AutomationIds to critical UI elements (Shell.xaml, forms)
2. Implement 5 smoke tests using Appium + MSTest
3. Use Page Object Model for maintainability
4. Integrate with existing build process

### Technology Choice
- **Appium** (not WinAppDriver) - actively maintained, works with WinUI 3
- **MSTest** - already in use for unit tests
- **Page Object Model** - reduces brittleness

### Tests to Automate
1. Application launches without error
2. Create and save new story
3. Add character and scene elements
4. Open existing story file
5. Clean exit with unsaved changes prompt

## Benefits
- **Time Savings**: 25+ hours/year on smoke testing alone
- **Faster Feedback**: Catch breaks within minutes, not hours
- **Consistency**: Same tests run exactly the same way every time
- **Developer Confidence**: Know immediately if changes break core functionality
- **Tester Focus**: Free testers for exploratory and complex scenario testing

## Risks and Mitigation
| Risk | Impact | Mitigation |
|------|--------|------------|
| Test brittleness | High maintenance | Use AutomationIds, not visual properties |
| Technology obsolescence | Rework needed | Use Appium (active) vs WinAppDriver (dead) |
| High initial cost | ROI delay | Start with just 5 tests, expand if successful |

## Implementation Timeline
- **Week 1-2**: Add AutomationIds to UI elements
- **Week 3**: Implement POC with 2 tests
- **Week 4**: Complete all 5 smoke tests
- **Week 5**: CI integration and documentation

## Success Metrics
- Smoke test execution time < 2 minutes
- 90%+ test reliability (not flaky)
- Zero manual smoke testing required
- Catch 1+ regression per month that manual testing might miss

## Cost/Benefit Analysis
- **Investment**: ~200 hours development time
- **Annual Savings**: 25+ hours testing time + earlier bug detection
- **Break-even**: Year 2 assuming < 25 hours annual maintenance

## Alternative Approaches Considered
1. **Continue manual only** - Rejected: Growing burden
2. **Full test automation** - Rejected: Too expensive initially  
3. **AI-powered testing** - Deferred: Evaluate after Phase 1

## Dependencies
- Appium.WebDriver NuGet package
- Appium server on build machines
- Developer time for AutomationId additions

## Open Questions
1. Should we automate on local builds or only CI builds?
2. Which CI system are we using for builds?
3. Should failed tests block releases?

## Next Steps
If approved:
1. Create GitHub issue for tracking
2. Add AutomationIds incrementally during regular development
3. Implement POC with launch and save tests
4. Review POC results before full implementation

## References
- [UI Automation Design Document](./UI_Automation_Design_Document.md)
- [Smoke Test Plan](./Smoke_Test.md)
- [Appium Windows Driver](https://github.com/appium/appium-windows-driver)

---

**Priority**: Medium  
**Effort**: Large (5 weeks)  
**Value**: High (long-term)  
**Category**: Testing Infrastructure