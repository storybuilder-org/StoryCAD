# StoryCAD issue 1546: status of the work

Written 2026-11-28, after a power loss on 2026-11-26. Last changed on 2026-11-28, after the design was approved. A new session can start from this file. Keep it current: change it when a step below is done.

## Where the work stands

Issue 1546 is at step 3 of the process, the build. The design is approved, and the build has started.

| Step | State |
|------|-------|
| 1. Issue | Done. Rewritten and posted on 2026-11-26. Reviewed, with answers. Terry ticked "The issue is accepted." |
| 2. Design | Done. Terry ticked "The design is approved." on 2026-11-28, for revision 2. Revision 3, of the same day, brings the text to the ASD-STE100 rules of process pull request 10; the design itself is the same. A comment on the issue lists what changed. A reviewer read revision 1 only. |
| 3. Build | Started 2026-11-28. Pull request 1: tasks 1 to 3 of part 23 are done and committed (the data, the methods of the service, the open and the trash). The next action is task 4. |
| 4. Pull request | Not started. |

## The next action

Continue the build of pull request 1, at task 4 of part 23 of the design: `StructureChangedMessage`, `StructureElementItem` and `StructureElementListViewModel`. Then task 5, the changes to `BeatSheetsViewModel`.

Build notes: build with `-restore -nr:false`, because MSBuild gave internal errors on this computer without them. Two tests fail in this worktree for a reason that is not this work: `CheckDotEnvFile` and `CheckDoppler` need the file `StoryCADTests/.env`, which is not in the worktree. Before the first test, load the skill `test`. Before the first code, load the skill `code`. Write each test before its code. The commands to build and to run the tests are in `CLAUDE.md` of the StoryCAD repo.

The design leaves the look of the screens open. Part 19 lists those items; Terry settles each one on the branch, and the session records the value there.

The issue's Tasks section holds the list of the large steps. Tick each one when it is done.

## Where each thing is

In the StoryCAD repo, worktree `D:\dev\src\StoryCAD-1546`, branch `issue-1546-beat-tree`, pushed:

| File in `devdocs` | What it is |
|-------------------|------------|
| `issue-1546-status.md` | This file. |
| `issue-1546-discussion-notes.md` | Every decision Terry made on 2026-11-25 and 2026-11-26, what the code does today, and what is open. |
| `issue-1546-mockup.html` | The mock-up of the beat tree window and the Structure tab. It shows The Old Man and the Sea. Open it in a browser. |
| `issue-1546-rewrite-draft.md` | The text of the issue as posted. The issue on GitHub is the true copy; it has one more line that names the design. |
| `issue-1546-original-body.md` | The issue body that the rewrite replaced. |

In the repo `storybuilder-miscellaneous`, folder `D:\dev\src\storybuilder-miscellaneous\devdocs`, branch `november-2026-filing`, committed and pushed (commit `229e10c`):

| File | What it is |
|------|------------|
| `issue-1546-design.md` | The design, revision 3. A plain English summary, then parts 1 to 23 in ASD-STE100, then Appendix A with the text for the manual page. |
| `issue-1546-design-review.md` | The reviewer's whole report on revision 1, with what the reviewer was given, and the answers. |
| `issue-1546-issue-review.md` | The reviewer's whole report on the issue, and the answers. |
| `issue-1546-design-glossary.yaml` | The word list that the ASD-STE100 checker uses for the design. |
| `issue-1546-design-check.json` | The checker's result: compliant, 0 findings. |
| `issue-1546-glossary-build.py` | The script that builds the word list and runs the checker with the shared word list of the process. Run it again after any change to the design, then commit, so the result file matches the committed design. |

On GitHub: StoryCAD issue 1546. Milestone Release 4.4. The body names the mock-up, the notes, the issue review, the design and the design review.

## The shape of the work, for a session that has not seen the conversation

- **The aim** (Terry): develop a plot from several Problems that in the end break down into Scenes.
- **Two goals:** a beat tree screen in StoryCAD, and a Collaborator workflow that works through the beat tree. Issue 1546 is the StoryCAD part. The Collaborator part gets its own issue, which is not written. Terry wants it written after the StoryCAD screen is up.
- **The idea underneath:** each Problem keeps a list of the Scenes and Problems that belong to it. An element is on a beat or on no beat. Nothing is lost when a beat sheet changes.
- **Three pull requests,** all for Release 4.4, in Terry's order:
  1. The list, the shared view model, the beat tree window.
  2. The AI's access: API methods and MCP tools.
  3. The protection: refusing a loop, and making the ways of changing a beat sheet agree.
- **The Structure tab and a column of the beat tree use the same view model** (Terry: "Do one thing one way, so that there is less confusion.").
- **Look and feel is settled on the branch with Terry,** without a new approval. Part 19 of the design lists the items.

## How Terry wants the work done

- Plain English in conversation. Explain each term. No shorthand such as "rule 6".
- Keep it as simple as possible. The work grows if allowed to.
- Listen. Do not take charge of the conversation.
- A session never ticks an approval box, and never unticks one. When the issue body is edited, start from the body as it stands on GitHub, so the tick on "The issue is accepted." is kept.
- Wording in the design is ASD-STE100, under the rules in `rules/writing-standards.md` of the process repo. The issue says "element" for a Scene or a Problem. The notes say "part". The design says "structure element", "beat column", "structure parent" and "structure child", because the shared word list fixes the meaning of "element", "column" and "parent".
- A change to the design after its approval needs a comment on the issue that says what changed.

## Things a new session must check before it acts

- That "The issue is accepted." is still ticked, and whether "The design is approved." is ticked: `gh issue view 1546 --repo storybuilder-org/StoryCAD`.
- That the StoryCAD `dev` branch has not moved in the files the design names. The design's facts are checked against commit `70890daf`.
