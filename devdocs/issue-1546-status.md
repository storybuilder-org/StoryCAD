# StoryCAD issue 1546: status of the work

Written 2026-11-28, after a power loss on 2026-11-26. A new session can start from this file. Keep it current: change it when a step below is done.

## Where the work stands

Issue 1546 is at step 2 of the process, the design. The design is written and reviewed one time. It waits for the maintainer.

| Step | State |
|------|-------|
| 1. Issue | Done. Rewritten and posted on 2026-11-26. Reviewed. Terry ticked "The issue is accepted." on GitHub. |
| 2. Design | Revision 2 is written. A reviewer read revision 1. No reviewer has read revision 2. "The design is approved." is not ticked. |
| 3. Build | Not started. No StoryCAD code is written. |
| 4. Pull request | Not started. |

## The next action

Terry reads the summary at the top of the design and the review report, then does one of these:

1. Ticks "The design is approved." on the issue in the browser. Then the build of pull request 1 starts, at task 1 of part 23 of the design.
2. Asks for a change to the design. Five choices and three differences from the issue are listed in the design's summary for this purpose.
3. Asks for a second review, of revision 2, before he decides.

Two questions are also waiting for Terry:

- Which branch of `storybuilder-miscellaneous` the five `issue-1546` files go on. They are not committed.
- Whether the summary of the design, about 60 lines, is too long. The process asks for one screen.

## Where each thing is

In the StoryCAD repo, worktree `D:\dev\src\StoryCAD-1546`, branch `issue-1546-beat-tree`, pushed:

| File in `devdocs` | What it is |
|-------------------|------------|
| `issue-1546-status.md` | This file. |
| `issue-1546-discussion-notes.md` | Every decision Terry made on 2026-11-25 and 2026-11-26, what the code does today, and what is open. |
| `issue-1546-mockup.html` | The mock-up of the beat tree window and the Structure tab. It shows The Old Man and the Sea. Open it in a browser. |
| `issue-1546-rewrite-draft.md` | The text of the issue as posted. The issue on GitHub is the true copy; it has one more line that names the design. |
| `issue-1546-original-body.md` | The issue body that the rewrite replaced. |

In the repo `storybuilder-miscellaneous`, folder `D:\dev\src\storybuilder-miscellaneous\devdocs`, **not committed**:

| File | What it is |
|------|------------|
| `issue-1546-design.md` | The design, revision 2. A plain English summary, then parts 1 to 23 in ASD-STE100, then Appendix A with the text for the manual page. |
| `issue-1546-design-review.md` | The reviewer's whole report on revision 1, with what the reviewer was given. |
| `issue-1546-issue-review.md` | The reviewer's whole report on the issue. |
| `issue-1546-design-glossary.yaml` | The word list that the ASD-STE100 checker uses for the design. |
| `issue-1546-design-check.json` | The checker's result: compliant, 0 findings. |
| `issue-1546-glossary-build.py` | The script that builds the word list and runs the checker. Run it again after any change to parts 1 to 23 of the design. |

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
- Wording in the design moves to ASD-STE100. The issue says "element" for a Scene or a Problem. The notes say "part".

## Things a new session must check before it acts

- That "The issue is accepted." is still ticked, and whether "The design is approved." is ticked: `gh issue view 1546 --repo storybuilder-org/StoryCAD`.
- That the five files in `storybuilder-miscellaneous\devdocs` are still there. They exist only on this computer.
- That the StoryCAD `dev` branch has not moved in the files the design names. The design's facts are checked against commit `70890daf`.
