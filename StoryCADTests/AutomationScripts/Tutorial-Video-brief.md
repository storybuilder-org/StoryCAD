# Tutorial video brief

The brief for the Danger Calls tutorial video (#1626, Release 4.4). Approved by Terry, 2026-11-11. Change the brief first, then the script.

## Document kind

| | |
|---|---|
| What language? | ASD-STE100 for the "Action on screen" column. The narration is spoken to viewers and follows no-ai-slop. |

## Terms

| Term | Meaning |
|---|---|
| click, right-click, drag, type | Technical verbs for mouse and keyboard actions in StoryCAD |
| template | The StoryCAD file that a new outline starts from |
| File Open page | The StoryCAD page that opens and makes outlines |
| Danger Calls | The outline that the tutorial makes |
| Leonard Kraskin, Charlie Lacas, Tony Irwin, Joe Edwards | Characters in Danger Calls |
| TC-NNN | A step in `Tutorial-Creating-a-Story.scs`. The video script uses the actions of that step. |
| Story Overview, problem, character, scene, story problem, premise, concept, beat sheet, beat | StoryCAD story elements and structure items, as in the user manual |
| Key Questions, Trait Builder, Master Plots, Stock Scenes | StoryCAD tools on the Tools menu or in a form |

## Sources

- Actions: `Tutorial-Creating-a-Story.scs` (#1422, PR #1673), steps TC-001 to TC-027. The video uses the same clicks and text. It does not take screenshots.
- Narration: the manual pages `docs/Tutorial Creating a Story/Creating_a_Story_pt_1.md` to `pt_9.md`, cut to what the screen shows.
- No earlier tutorial video exists on either channel, so no old script applies.

## One video in three sections

Terry, 2026-11-11: one video. A text card between sections reminds the viewer that an outline does not need to be done in one session. The sections follow the manual pages:

| Section | Manual pages | Script steps | Subject |
|---|---|---|---|
| 1 | pt 1, pt 2 | TC-001 to TC-007 | From idea to concept and characters |
| 2 | pt 3, pt 4, pt 5 | TC-008 to TC-017 | Problems, the story problem, and a beat sheet |
| 3 | pt 6 to pt 9 | TC-018 to TC-027 | Plotting with scenes and tools |

The narration is 1,364 words (section 1: 440, section 2: 475, section 3: 449). At the runner's estimate of 0.4 s for each word, that is 546 s of voice. The actions alone took 358 s on Brigid and about 250 s in the VM for the full test run (#1422 status log). A step lasts as long as the longer of its voice and its actions. With a 25 s title card and the 10 s closing card, the video runs about 9.7 minutes, plus the steps where the actions run longer than the voice. A timed run of `Tutorial-Creating-a-Story.scs` gives the action time of each step.

## Section 1: From idea to concept and characters

| # | Action on screen | Narration |
|---|---|---|
| 1.1 | Title card: logo, "Creating a Story with StoryCAD", music | (none) |
| 1.2 | Text card: "Danger Calls: an outline from one idea" | This video makes a complete story outline in StoryCAD, one step at a time, from a single idea. The story is Danger Calls, a short suspense story about two detectives and a drug dealer. We don't claim it's a great outline. The point is that any idea, worked through a process, can become a workable story. Every step is also written out in the user manual, under Tutorial Creating a Story. |
| 1.3 | Make a new outline with the "Folders" template and the name "Danger Calls" (TC-001). | Every story starts with an idea. Ours came from a television news piece: boys as young as fourteen selling drugs on the street, using disposable cell phones. Record an idea when it comes to you, before you lose it. It's normal to have many more ideas than stories you develop. To capture this one, open the File menu's Open/Create page and create a new outline. We pick the Folders template and name the project Danger Calls. The project name becomes the file name. |
| 1.4 | Type a sentence on the "Story Idea" tab. Then click the "Changed" indicator on the status bar (TC-002). | Select the story overview at the top of the tree. Its first tab, Story Idea, is where the idea goes. The author and dates are already filled in, from your preferences and your computer's clock. When you type, the pencil on the status bar turns red: there are unsaved changes. Click it to save, and it turns green. |
| 1.5 | Select "Suspense" in "Genre" and "Short Story" in "Type" on the "Structure" tab (TC-003). | The Structure tab sets limits on the story's form. Danger Calls is about crime, so the genre is Suspense, and the type is Short Story, to keep the example brief. Most drop-down lists in StoryCAD are suggestions. You can type a value that isn't on the list. |
| 1.6 | Close the outline. Then open it again from the list on the File Open page (TC-004). | After a break, we come back to the outline. StoryCAD opens on the File Open page, which lists the outlines you worked on last. Select Danger Calls and open it. |
| 1.7 | Type the concept on the "Concept" tab (TC-005). | The next step turns the idea into a concept. Larry Brooks, in his book Story Engineering, defines it this way: "A concept is an idea that asks a question that implies conflict." 'What if?' is a good way in. The answer to the question is your story. |
| 1.8 | Open "Key Questions" for the Story Overview and go to the first concept question (TC-006). | Questions can also test a concept. The Key Questions tool, on the Tools menu, has questions for each kind of story element. With Story Overview selected, Next Question steps through them to the first question about the concept. |
| 1.9 | Add three characters: Leonard Kraskin, Charlie Lacas and Tony Irwin (TC-007). | A cops and robbers story has two obvious characters, a cop and a criminal. Right-click the Characters folder to add a character. Leonard Kraskin is a detective and the protagonist. Charlie Lacas is a drug dealer and the antagonist. Drug Dealer isn't on the role list, so we type it in. Tony Irwin, a second detective, is Leonard's partner, which gives Leonard someone to talk to. |

## Section 2: Problems, the story problem, and a beat sheet

| # | Action on screen | Narration |
|---|---|---|
| 2.1 | Text card: "Section 2: Problems. You do not need to make an outline in one session." | You don't have to build an outline in one sitting. Save your work, close StoryCAD, and come back when you're ready. So far, Danger Calls has an idea, a concept and three characters. Next, the characters get problems, which are the heart of plotted fiction. |
| 2.2 | Add the problem "Catching the drug dealer Lacas". Then fill in its type, conflict, subject and story question (TC-008). | A concept points toward a premise: the answer to the concept's question, which is also the story told in a sentence or two. In StoryCAD, a Problem story element holds most of the premise. Leonard's basic problem is to catch Charlie. That makes the problem type Conflict, the conflict type Person versus Person, and the subject Crime. The story question is what the reader wants an answer to. It's why the reader will turn pages. |
| 2.3 | Add the problem "How will Leonard and Tony get past Charlie's tech?" (TC-009). | One question leads to another: how do the detectives get past Charlie's technology? That's a complication, so it becomes a second problem, with a how question instead of a yes or no question. Its conflict type is Person versus Situation. |
| 2.4 | Add the problem "Lacas gets the drop on Kraskin" (TC-010). | The concept mentions a rescue. A rescue from what? Perhaps Lacas gets the drop on one of the detectives, so that the other can rescue him. That's a third problem. |
| 2.5 | Use "Trait Builder" to put "(Behavior): Impulsive" on the "Outer Traits" tab of Leonard (TC-011). | The heart of a story is a character with a problem. To balance an outer, physical problem with an inner one, we give Leonard a flaw. On his Outer Traits tab, Trait Builder lists example behaviors, and we copy Impulsive. |
| 2.6 | Select "Impulsive" in "Adventurousness" for Leonard and "Thoughtful" in "Shrewdness" for Tony (TC-012). | Impulsive is also a value of Adventurousness on the Inner Traits tab. For conflict between the partners, Tony gets a trait that clashes with Leonard's: Thoughtful, under Shrewdness. |
| 2.7 | Give the problem "Lacas gets the drop on Kraskin" a new name. Then type notes on the "Notes" tab of the problem "How will Leonard and Tony get past Charlie's tech?" (TC-013). | Now the third problem can be more precise: Leonard's impulsiveness lets Lacas capture him. It's a Person versus Self problem. Research into how dealers communicate goes on the Notes tab of the second problem. Most story elements have a Notes tab. |
| 2.8 | Type the premise on the "Resolution" tab of two problems (TC-014). | A problem's Resolution tab holds its premise. Leonard rushes into a trap and Lacas captures him. Tony frees him by paging Lacas. The first problem's premise adds the last piece: Tony gets the drop on the dealers. |
| 2.9 | Select "Catching the drug dealer Lacas" in "Story Problem" on the "Premise" tab of the Story Overview (TC-015). | Only one problem is the story problem: when it's resolved, the story is over. You pick it on the overview's Premise tab, and its premise shows there. A change in either place changes both. |
| 2.10 | Add the scene "He's like a ghost" with a sketch (TC-016). | With the characters and problems known, plotting can start. We open with Leonard and Tony talking about how Lacas always seems one step ahead of them. Right-click the Scenes folder to add the scene. |
| 2.11 | Select the "Three Act Play" beat sheet on the "Structure" tab of the story problem. Then select the "Narrative hook" beat (TC-017). | A story structure gives the plot a skeleton. In StoryCAD, a structure is a beat sheet: a list of points where certain things should happen. Beats are not scenes. They remind you what your scenes need to do, and in roughly what order. A scene can do more than one thing, and it can encompass more than one beat, or even play a part in more than one problem story element. |

## Section 3: Plotting with scenes and tools

| # | Action on screen | Narration |
|---|---|---|
| 3.1 | Text card: "Section 3: Plotting. Work on your outline in as many sessions as you need." | Take as many sessions as you need. Each element waits in the tree until you come back to it. Danger Calls now has three problems, a story problem with a premise, and a skeleton for the plot. Next, scenes fill it in. |
| 3.2 | Click "Assign" to attach "He's like a ghost" to the "Narrative hook" beat (TC-018). | He's like a ghost is our narrative hook. With the beat selected, the Structure tab lists the scenes and problems you can assign to it. Select the scene and click Assign. |
| 3.3 | Drag the two other problems below the story problem. Then drag the scene below the problem "How will Leonard and Tony get past Charlie's tech?" (TC-019). | The two smaller problems belong under the story problem, and the scene belongs under the problem it's part of. To move a node, drag it onto its new parent. |
| 3.4 | Use "Master Plots" to add "Escape". Then click "Convert to Scene" and give the scene a name, a sketch and a cast (TC-020). | We already had a climax in mind. Master Plots, on the Tools menu, offers patterns of action that recur in fiction. Escape arrives as a problem under the selected node. We convert it to a scene, give it our premise, and add Leonard, Tony and Charlie to its cast. |
| 3.5 | Fill in the "Role", "Psychological", "Social" and "Backstory" tabs of Charlie Lacas (TC-021). | Now the characters get some flesh, starting with Charlie Lacas. He grew up in New York City and deals drugs in a smaller city upstate, supplied through his New York contacts. He's bright, which makes him a worthy opponent. A few details on each tab make him someone you can picture. |
| 3.6 | Fill in the "Development" and "Conflict" tabs of the scene "He's like a ghost" (TC-022). | Scenes are miniature stories too. The opening scene's value exchange is Challenge and Acceptance: the detectives choose to look for an answer instead of giving up. Lacas isn't in the scene, so its conflict is between the two who are, Tony and Leonard. |
| 3.7 | Use "Stock Scenes" to add "The chase leads into an ambush". Then give it the name "Shootout". Then select "Climax scene" in "Scene Type" (TC-023). | Stock Scenes, also on the Tools menu, lists scenes readers recognize at once. The chase leads into an ambush becomes our climax scene, named Shootout. |
| 3.8 | Add two scenes after "Shootout". Then select the "Denouement" beat (TC-024). | The Three Act Play ends with the denouement. Two scenes after Shootout fill it: Tony updates Leonard on the aftermath, and Tony confronts Jack Chun. |
| 3.9 | Add the character "Joe Edwards". Then add two scenes below the problem "How will Leonard and Tony get past Charlie's tech?" (TC-025). | The middle of the story needs a way to find the next drug deal. Joe Edwards owns an electronics shop, and he's a Peeping Tom. Leonard threatens to arrest and expose him unless he sets up a listening post. |
| 3.10 | Use "Stock Scenes" to add a scene from "Twists and Turns". Then add the scene "Leonard, on stake out, learns of a drug deal" and move it up one position (TC-026). | Stock Scenes has a category for twists and turns. In one, an ally turns out to be on the wrong side: Edwards tips off Lacas, who traps Kraskin. A new scene just before it has Leonard, on stakeout, learn of a drug deal. Move puts it in place. |
| 3.11 | Open "Key Questions" for the Story Overview and go to the 4th question (TC-027). | Before you draft, review. Changes are cheap while the story is still an outline. Key Questions tests the whole story. This one asks about the market, which we haven't picked yet. |
| 3.12 | Exit StoryCAD. | Danger Calls is now most of an outline: problems, characters and a plot. Settings and fuller characters are still to come. StoryCAD is free from the Microsoft Store and the Mac App Store. Thanks for watching. |
| 3.13 | Closing card: "Like and subscribe" | (none) |

Words in quotation marks in the "Action on screen" column are StoryCAD labels, list values, or element names.

## Open points for Terry

1. The video title.
