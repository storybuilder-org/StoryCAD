# Tutorial Creating a Story Test Plan
**Time**: ~10 minutes (script run: about 6 minutes)
**Purpose**: Follow the user manual tutorial "Tutorial Creating a Story" (`docs/Tutorial Creating a Story/`) and retake its images.

## Setup
Launch StoryCAD. The tutorial builds one outline, "Danger Calls", from the Folders template. Each step lists the manual page it covers and the script step in `StoryCADTests/AutomationScripts/Tutorial-Creating-a-Story.scs`.

Not scripted: the tutorial has no Collaborator steps. Draft status: the manual pages are not yet compared with the app (see the status log).

---

| Step | Manual page | Actions | Expected |
|------|-------------|---------|----------|
| TC-001 | pt 1 | File > Open/Create file > Create new outline; template Folders; name "Danger Calls"; Create outline | Outline opens with Problems, Characters, Settings, Scenes folders; green pencil |
| TC-002 | pt 1 | Select the root; type a Story Idea; click the pencil | Pencil is red after the edit, green after the click |
| TC-003 | pt 1 | Structure tab: Genre Suspense, Type Short Story | Values show |
| TC-004 | pt 2 | Save, close the outline, reopen it from Recently opened | Outline reopens |
| TC-005 | pt 2 | Concept tab: type the concept question | Text stays |
| TC-006 | pt 2 | Tools > Key Questions, Story Overview, Next twice | First Concept question shows |
| TC-007 | pt 2 | Characters folder > Add Elements > Add Character three times: Leonard Kraskin (Detective, Protagonist), Charlie Lacas (Drug Dealer, Antagonist), Tony Irwin | Three characters under Characters |
| TC-008 to TC-010 | pt 3, pt 4 | Add three Problems with type, conflict type, subject, story question | Fields keep values |
| TC-011 | pt 4 | Leonard, Outer Traits, Trait Builder, Behaviors, Impulsive, Copy trait | "(Behavior): Impulsive" in the list |
| TC-012 | pt 4 | Inner Traits: Leonard Adventurousness Impulsive; Tony Shrewdness Thoughtful | Values show |
| TC-013 | pt 4 | Rename the third problem, set Person vs. Self, add research notes to the second | Tree shows the new name |
| TC-014 | pt 4, pt 5 | Resolution tab Premise on the first and third problems | Text stays |
| TC-015 | pt 5 | Overview Premise tab: Story Problem = first problem | Possible defect: the Premise tab stays empty and a save erases the Problem's own Premise |
| TC-016 | pt 5 | Scenes folder > Add Scene "He's like a ghost" with a sketch | Scene under Scenes |
| TC-017, TC-018 | pt 5, pt 6 | First problem, Structure tab: beat sheet Three Act Play; assign the scene to "Narrative hook" | Beat shows the scene (Three Act Play is a beat sheet in 4.x, not a Master Plot) |
| TC-019 | pt 6 | Drag problems under the first problem; drag the scene under the second | Tree shows the nesting |
| TC-020 | pt 6 | Right-click the third problem, Tools > Plotting Aids > Master Plots > Escape > Copy; Convert to Scene; name it; Cast tab, All Characters, check three | Scene has three cast members |
| TC-021 | pt 7 | Charlie Lacas: Role, Psychological, Social, Backstory tabs | Values stay |
| TC-022 | pt 8 | Opening scene: Development and Conflict tabs | Values stay |
| TC-023 | pt 8 | Stock Scenes > Chase Scenes > "The chase leads into an ambush" > Add Scene; rename to Shootout; type Climax scene | Scene under the third problem |
| TC-024 | pt 8 | Add the two denouement scenes | Scenes in the tree |
| TC-025 | pt 9 | Add Joe Edwards; add two scenes under the second problem | Scenes in the tree |
| TC-026 | pt 9 | Stock Scenes > Twists and Turns > first item; rename; add a stake-out scene; Move up | Order: stake-out before tip-off |
| TC-027 | pt 9 | Key Questions, Story Overview, fourth question | Structure question about markets |

Script line numbers: each `step "TC-nnn"` line in the script. TC-008 to TC-010 are three steps in the script.

**Pass/Fail:** ______
