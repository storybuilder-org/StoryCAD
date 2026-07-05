# AutomationProperties naming convention

Approved by founder 2026-07-03 on issue #1420 (design comment plus the 2026-07-03 amendments comment). This document is the single source of truth for `AutomationProperties.AutomationId` and `AutomationProperties.Name` in StoryCAD XAML. Items marked *(plan-stage addition)* were added while drafting the implementation plan and need founder sign-off with that plan; everything else is approved as written.

## Scope

All XAML under `StoryCAD/Views` and `StoryCADLib` (`Controls`, `Services/Dialogs`, `Services/Dialogs/Tools`, `Collaborator/Views`): 39 files, ~586 declared interactive control tags as of 2026-07-03 on `dev`. XAML added after that date is caught by the convention scan test in `StoryCADTests`, not by re-surveying.

## AutomationId

Every interactive control gets `AutomationProperties.AutomationId`.

- PascalCase, named by function, suffixed by control role:

| Control | Suffix | Example |
|---|---|---|
| Button, AppBarButton, HyperlinkButton | `Button` | `FileMenuButton`, `MoveUpButton` |
| MenuFlyoutItem, MenuFlyoutSubItem | `MenuItem` | `SaveStoryMenuItem` |
| ComboBox | `Combo` | `CharacterRoleCombo` |
| TextBox | `TextBox` | `AuthorTextBox` |
| RichEditBoxExtended | `RichEdit` | `StoryIdeaRichEdit` |
| CheckBox | `Check` | `AutoSaveCheck` |
| NumberBox | `NumberBox` | `AutoSaveIntervalNumberBox` |
| AutoSuggestBox | `SearchBox` | `ShellSearchBox` |
| TabViewItem | `Tab` | `StoryIdeaTab` |
| TabView | `Tabs` | `OverviewTabs` |
| TreeView | `Tree` | `NavigationTree` |
| ListView | `List` | `ElementsList` |
| GridView | `GridView` | `ImagesGridView` *(plan-stage addition; needed for `ImageGalleryControl`)* |
| Flyout | `Flyout` | `EmptyTrashFlyout` |
| RadioButton | `Radio` | *(added Unit 1)* |
| RadioButtons | `Radios` | `StructureElementsRadios` *(added Unit 2)* |
| ToggleSwitch | `Toggle` | *(added Unit 1)* |
| BrowseTextBox | `TextBox` | *(added Unit 1)* |
| ItemsRepeater | `Tree` | `NavigationTree` *(added Unit 1; exists for the Shell navigation tree stand-in; revisit if a non-tree ItemsRepeater ever needs annotation)* |
| Expander | `Expander` | `GeographyExpander` *(added Unit 4)* |
| NavigationView | `Nav` | `FileOpenNav` *(added Unit 6)* |
| NavigationViewItem | `NavItem` | `RecentlyOpenedNavItem` *(added Unit 6)* |
| InfoBar | `InfoBar` | `PrintReportsSynopsisWarningInfoBar` *(added Unit 7)* |
| TreeViewItem | `TreeItem` | *(added Unit 8, founder ruling on PR #1457; navigation automation depends on manipulating tree rows. Templated TreeViewItems — stamped from a DataTemplate, which is every current instance: Shell x3, NarrativeTool x2 — carry NO AutomationId (TemplateSafety) and instead bind the accessible name: `AutomationProperties.Name="{x:Bind Name, Mode=OneWay}"`, the pattern Shell.xaml already uses)* |

- A control type not in this table gets a proposed suffix in the PR description; the table row is added when the PR merges.
- Values are literal strings: ASCII letters and digits, no spaces, no bindings, never a story element name, file path, date, or any other runtime value.
- Unique within the live UI tree (Shell plus one page plus at most one open dialog). In practice the scan test enforces global uniqueness across all annotated files *(plan-stage addition: strict enforcement of the approved "preferred style")*:
  - Prefix with the page name where a function repeats across pages: `OverviewNotesRichEdit`, `CharacterNotesRichEdit`.
  - Dialog-internal controls prefer the dialog-name prefix (`SaveAsCancelButton`) even though only one dialog opens at a time.
- Where an existing `x:Name` already fits this style, the AutomationId reuses the same string. Never rename an `x:Name` (code-behind references it), and never add a new `x:Name` just for automation; AutomationId does not need one.
- AutomationProperties attributes are set unconditionally: no `win:`/`skia:` prefixes, so Windows and macOS expose the same identifiers (ADR-001 constraint).

## Name vs LabeledBy

- Controls with `Header` (TextBox, ComboBox, NumberBox), `Content` (Button, CheckBox, RadioButton), or `Label` (AppBarButton) get their accessible name from the framework. Do not duplicate it with `AutomationProperties.Name`. AutomationId is still required.
- Set `AutomationProperties.Name` where no clean visible label exists:
  - icon-only buttons (the Shell status bar save, autosave, and backup buttons; the four Move flyout arrow buttons);
  - a Button whose `Content` is a panel rather than plain text or a `Header`: WinUI 3 does not aggregate inner TextBlock text into the UIA Name (verified live 2026-07-04, StoryWorldPage Add/Remove buttons announced blank), so the visible text is duplicated as an explicit Name;
  - every MenuFlyoutItem whose `Text` embeds a padded keyboard shortcut (`"Save Story                      Ctrl+S"`): Name carries the clean command text (`Save Story`), set once and unconditionally so the `win:`/`skia:` text variants share one accessible name;
  - AutoSuggestBox with only `PlaceholderText` (the Shell search box);
  - a field whose visible label is a sibling `Expander.Header` TextBlock rather than a `Header` property (StoryWorldPage's #782 Expander layout, where the header doubles as a bold-when-filled content indicator): mirror the header text as a literal Name. Founder ruling 2026-07-04 on PR #1451: literal Name, not `LabeledBy`, so no `x:Name` gets added to label TextBlocks for automation; a reworded header means updating the Name with it, since the static tests cannot catch drift. *(added Unit 4)*
  - custom controls whose `Header` does not reach the automation peer. PR 1 verifies `RichEditBoxExtended` and `BrowseTextBox` with Accessibility Insights. If Header is not exposed: `BrowseTextBox` gets the Name once inside its own XAML; `RichEditBoxExtended` has no XAML (partial classes only), so the Name is set once in its C# constructor via `AutomationProperties.SetName` *(plan-stage addition: the C# path; the approved design assumed control XAML)*.
  - a text input with no visible label source at all — no `Header`, no `PlaceholderText`, no sibling label TextBlock: name the function with a literal Name, the same principle as icon-only buttons (WorkflowPage's chat input carries `Name="Message"`). Founder ruling 2026-07-05 on PR #1458. *(added Unit 9)*
  - a ListView whose container announcement would otherwise be blank or unverified: `ListView.Header`'s UIA Name derivation is unprobed, so a Header-bearing list pins the header text as a literal Name (StockScenesDialog `Name="Scenes"`, founder ruling 2026-07-04 on PR #1457), and a Header-less list a user must locate pins a function name (`Name="Elements"` on both ElementPicker lists, founder ruling 2026-07-05 on PR #1458). Id-only remains correct where the container announcement carries no information (FileOpenMenu's lists, PrintReportsDialog's five report lists). Realized items announce independently via `DisplayMemberPath` or bound Names. *(rulings Units 8-9, bullet recorded at closeout)*
- A control whose only visible label is a sibling TextBlock gets a literal `AutomationProperties.Name` mirroring the label text, punctuation stripped (`"Target:"` → `Target`). `AutomationProperties.LabeledBy` is not used anywhere in this codebase: it would need an `x:Name` on the label TextBlock, and the convention forbids adding an `x:Name` solely for automation. A reworded label means updating the Name with it, since the static tests cannot catch drift. *(generalized Unit 8, founder ruling on PR #1457; extends the Unit 4 ruling from PR #1451)*
- **Temporary rule, in force only while the Uno generator bug (#1452) exists:** no literal `AutomationProperties.Name` on any element inside an `x:Load`-deferred subtree. Uno's desktop-head XAML generator mistakes the literal value for an element name in the x:Load dematerialization cleanup and emits a phantom `_{value}Subject` reference, failing `net10.0-desktop` compilation (WinAppSDK head unaffected; AutomationId unaffected; bound Names untested in deferred subtrees, so probe before relying on one). Verified 2026-07-04 by differential builds on FolderPage ("Notes" → `_NotesSubject`, "Zebra" → `_ZebraSubject`). Founder ruling 2026-07-04 on PR #1453: agreed, but only as long as the bug exists; #1452 tracks the upstream report, restoring FolderPage's Notes Name, and deleting this bullet when the fix ships. *(added Unit 5)*

## Repeated and templated controls

- Never put AutomationId inside a `DataTemplate`; every realized item would report the same id. The scan test enforces this across all files from day one.
- Annotate the items control itself (`NavigationTree`, `TrashTree`, `ElementsList`, `ImagesGridView`).
- Inside templates, bind `AutomationProperties.Name` to the item's display property (`{x:Bind Name}`) so each realized item is announced. Tests locate items by container AutomationId plus item Name.
- Bind Name inline on the template element itself, never via an `ItemContainerStyle`/Style `Setter`: the Windows Runtime does not evaluate bindings in `Setter.Value`, so the name silently never gets set (no error, no warning). *(added Unit 2)*
- Static TabViewItems are not templated; each gets a `Tab`-suffixed AutomationId.
- A composite user control (`Traits`, `RelationshipView`, `Conflict`, `Flaw`, `BrowseTextBox`, `ImageGalleryControl`) carries AutomationIds on its internal controls. If a composite is ever instantiated inside a repeater, treat it like a DataTemplate: internal ids stay, tests scope by the repeating parent's id plus a bound Name.

## Do not annotate

- Decorative icons inside an already-labeled parent (SymbolIcon, FontIcon inside a Button or MenuFlyoutItem).
- Separators and spacers: AppBarSeparator, Border rules, filler grid columns.
- Layout containers (Grid, StackPanel, ScrollViewer) unless one is a LabeledBy target or an items host.
- TextBlocks that are content rather than labels.
