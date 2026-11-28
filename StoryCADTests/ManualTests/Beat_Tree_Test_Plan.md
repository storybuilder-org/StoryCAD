# Test plan for the beat tree

The tests of the beat tree window and of the Structure tab for issue #1546 are in this file.

A person does these tests. Do each test on Windows and on macOS. Write the result of each test in the table at the end of this file.

## Names

| The name | The item |
|----------|----------|
| Beat column | The display of one Problem in the beat tree window: a header, the cards and the list `On no beat`. |
| Card | The display of one beat in a beat column. |
| Element list | The list at the right side of the beat tree window, and at the right side of the Structure tab. |
| First beat column | The beat column of the Problem `The old man hasn't cought a fish in 84 days`. |
| `Manolin` beat column | The beat column of the Problem `Manolin must abandon his jinxed mentor`. |

A button of the Structure tab and of the beat tree window has a symbol and no text. This table gives the name that this file uses for each button, and the text that shows when the pointer is on the button.

| The name in this file | The text on the button |
|-----------------------|------------------------|
| `Assign Beat` | `Assign element to beat` |
| `Unassign Beat` | `Take element off the beat` |
| `Add to problem` | `Add element to problem, on no beat` |
| `Remove from problem` | `Remove element from problem` |
| `Add Beat` | `Add a new beat` |
| `Delete Beat` | `Delete selected beat` |
| `Move Beat Down` | `Move beat down` |
| `Save Beat Sheet` | `Save beat sheet to file` |

## Before the tests

1. Start StoryCAD.
2. Select `File` > `Open/Create file` > `Sample outlines`.
3. Open `The Old Man and the Sea`.
4. Select `File` > `Save Story As`. Save the outline with the name `BeatTreeTest` in a test folder.
5. Do the tests in the sequence of this file. Do each test, because each test uses the result of the tests before.

## The beat tree window

### BT-001: The window opens

1. Select `Tools` > `Beat Tree`.

**Correct result:** A window with the name `Beat Tree` opens. The window shows five beat columns. The first beat column is for the Problem `The old man hasn't cought a fish in 84 days`.

### BT-002: The connectors

1. Find the four cards of the first beat column that have a Problem.
2. Follow the connector from each of these cards.
3. Move the beat columns in the horizontal direction.
4. Move the cards of the first beat column up and down.

**Correct result:** Each connector goes from the card of a Problem to the header of the beat column of that Problem. Each connector stays on its card after steps 3 and 4.

### BT-003: The contents of a beat column

1. Examine the first beat column.

**Correct result:** The header shows the name of the Problem and a `Beat Sheet` box with the text `Seven Point Story Structure`. The beat column shows eight cards. Each card shows the name of its beat and the name of its element, or the text `Unassigned`. The list `On no beat` is below the cards.

### BT-004: The element list

1. Select `Scene` above the element list.
2. Find the Scene `Santiago sleeps` and the Scene `He sails homeward` in the element list.

**Correct result:** The element list shows each Scene of the outline. `Santiago sleeps` has no text after its name. `He sails homeward` has the text `on a beat` after its name.

### BT-005: Action 1, an element on a beat, with a drag operation

1. Move `Santiago sleeps` from the element list to the card `Ice Monster (8)` of the first beat column, with a drag operation.

**Correct result:** The card `Ice Monster (8)` shows `Santiago sleeps`. In the element list, `Santiago sleeps` has the text `on a beat`.

### BT-006: Action 2, an element off a beat, with a drag operation

1. Move the card `Ice Monster (8)` to the list `On no beat` of the first beat column, with a drag operation.

**Correct result:** The card `Ice Monster (8)` shows `Unassigned`. The list `On no beat` of the first beat column shows `Santiago sleeps`. In the element list, `Santiago sleeps` has the text `on no beat`.

### BT-007: Action 3, an element in a Problem with no beat, with a drag operation

1. Move `Santiago prays` from the element list to the list `On no beat` of the `Manolin` beat column, with a drag operation.

**Correct result:** The list `On no beat` of the `Manolin` beat column shows `Santiago prays`. In the element list, `Santiago prays` has the text `on no beat`.

### BT-008: Action 4, an element out of a Problem, with a drag operation

1. Move `Santiago prays` from the list `On no beat` of the `Manolin` beat column to the element list, with a drag operation.

**Correct result:** The list `On no beat` of the `Manolin` beat column does not show `Santiago prays`. In the element list, `Santiago prays` has no text after its name.

### BT-009: Action 5, an element from one beat to a different beat, with a drag operation

1. Move `Santiago sleeps` from the list `On no beat` of the first beat column to the card `Ice Monster (8)`, with a drag operation.
2. Move the card `Ice Monster (8)` to the card `Midpoint (3)` of the same beat column, with a drag operation.
3. Select `Move here` in the dialog.

**Correct result:** After step 3, the card `Midpoint (3)` shows `Santiago sleeps`, and the card `Ice Monster (8)` shows `Unassigned`. The list `On no beat` of the first beat column shows `Man is not made for defeat`, the Scene that the card `Midpoint (3)` had before.

### BT-010: The dialog for a beat that has an element

1. Select the card `Midpoint (3)` of the first beat column.
2. Select the button `Delete Beat`.
3. Make sure that the dialog has the buttons `Keep in the Problem`, `Remove from the Problem` and `Cancel`.
4. In the dialog, select `Keep in the Problem`.

**Correct result:** The first beat column has seven cards. The list `On no beat` of the first beat column shows `Santiago sleeps`.

### BT-011: The dialog for a Scene on a different beat

1. Select the card `Ice Monster (8)` of the first beat column.
2. Select `He sails homeward` in the element list.
3. Select the button `Assign Beat`.
4. Make sure that a dialog with the name `Already on a beat` opens. Make sure that the dialog has the buttons `Move here`, `Put here as well` and `Cancel`.
5. In the dialog, select `Put here as well`.

**Correct result:** Two cards of the first beat column show `He sails homeward`: `Ice Monster (8)` and `Plot Turn II (7)`.

### BT-012: The dialog for a Problem with a different parent

1. Move the card `Pinch II (6)` of the first beat column to the card `Escalation` of the `Manolin` beat column, with a drag operation.
2. Make sure that a dialog with the name `Already assigned!` opens.
3. Select `Assign here`.

**Correct result:** The card `Pinch II (6)` shows `Unassigned`. The card `Escalation` of the `Manolin` beat column shows `Sharks are attracted to the fish`. The beat column of that Problem is immediately after the `Manolin` beat column. A connector goes from the card `Escalation` to that beat column.

### BT-013: The dialog for a Problem on a different beat of the same Problem

1. Move the card `Plot Turn I (4)` of the first beat column to the card `Pinch II (6)` of the same beat column, with a drag operation.
2. Make sure that a dialog with the name `Already assigned!` opens.
3. Select `Assign here`.

**Correct result:** The card `Plot Turn I (4)` shows `Unassigned`. The card `Pinch II (6)` shows `Santiago hooks a marlin bigger than his boat`. The connector to the beat column of that Problem starts at the card `Pinch II (6)`.

### BT-014: The two windows show the same data

1. Put the beat tree window and the main window of StoryCAD side by side.
2. In the main window, select the Problem `The old man hasn't cought a fish in 84 days` in the navigation tree. Select the tab `Structure`.
3. In the beat tree window, move `Santiago prays` from the element list to the card `Plot Turn I (4)` of the first beat column.
4. Make sure that the Structure tab shows `Santiago prays` on the beat `Plot Turn I (4)`.
5. On the Structure tab, select the beat `Plot Turn I (4)`. Select the button `Unassign Beat`.

**Correct result:** After step 5, the card `Plot Turn I (4)` in the beat tree window shows `Unassigned`. The list `On no beat` shows `Santiago prays` in the two windows.

### BT-016: Action 5 with a button

1. In the beat tree window, select the card `Plot Turn I (4)` of the first beat column.
2. Select `He sails homeward` in the element list.
3. Select the button `Assign Beat`.
4. Select `Move here` in the dialog.

**Correct result:** The card `Plot Turn I (4)` shows `He sails homeward`. One of the two cards that had `He sails homeward` before shows `Unassigned`.

### BT-017: A drag operation from one beat column to a different beat column

1. Find the card `Plot Turn I (4)` of the first beat column.
2. Move that card to the list `On no beat` of the `Manolin` beat column, with a drag operation.

**Correct result:** The list `On no beat` of the `Manolin` beat column shows `He sails homeward`. The card `Plot Turn I (4)` of the first beat column also shows `He sails homeward`.

### BT-018: A change of the beat sheet

1. In the header of the first beat column, select `Save The Cat (Mini)` in the box `Beat Sheet`.
2. In the dialog, select `Confirm`.

**Correct result:** The first beat column shows eight cards, and each card shows `Unassigned`. The list `On no beat` shows each element that was on a card before, in the sequence of the cards. Each Problem in the list `On no beat` has a beat column and a connector.

### BT-019: A beat sheet from a file

1. Move `Santiago prays` from the list `On no beat` of the first beat column to its first card.
2. Select the button `Save Beat Sheet`. Save the file with the name `BeatTreeTest` in the test folder.
3. In the header of the first beat column, select `Load Custom Beat Sheet from file...` in the box `Beat Sheet`.
4. Open the file `BeatTreeTest.stbeat`.

**Correct result:** The box `Beat Sheet` shows `Custom Beat Sheet`. Each card shows `Unassigned`. The list `On no beat` shows `Santiago prays`.

### BT-020: A loop is not possible

Do this test only with the app from pull request 3.

1. Find the Problem `Manolin must abandon his jinxed mentor` in the list `On no beat` of the first beat column.
2. Move that Problem to a card of the first beat column, with a drag operation.
3. Move that card to the list `On no beat` of the beat column of `Sharks are attracted to the fish`.
4. Select `Assign here` in the dialog.

**Correct result:** The Problem does not move. The main window shows the status message `Cannot put a Problem in its own descendant.`

### BT-015: The window closes with the outline

1. In the main window, select `File` > `Close Story`.
2. Select `No` in the dialog `Save changes?`.

**Correct result:** StoryCAD closes the beat tree window.

## The Structure tab

Before these tests, open `The Old Man and the Sea` from the sample outlines again. Select the Problem `The old man hasn't cought a fish in 84 days`. Select the tab `Structure`.

### BT-030: Action 1 with a button

1. Select the beat `Ice Monster (8)`.
2. Select `Scene` above the element list. Select `Santiago sleeps`.
3. Select the button `Assign Beat`.

**Correct result:** The beat `Ice Monster (8)` shows `Santiago sleeps`. In the element list, `Santiago sleeps` has the text `on a beat`.

### BT-031: Action 2 with a button

1. Select the beat `Ice Monster (8)`.
2. Select the button `Unassign Beat`.

**Correct result:** The beat `Ice Monster (8)` shows `Unassigned` and stays selected. The list `On no beat` shows `Santiago sleeps`.

### BT-032: Action 3 with a button

1. Select `Santiago prays` in the element list.
2. Select the button `Add to problem`.

**Correct result:** The list `On no beat` shows `Santiago prays`. In the element list, `Santiago prays` has the text `on no beat`.

### BT-033: Action 4 with a button

1. Select `Santiago prays` in the list `On no beat`.
2. Select the button `Remove from problem`.

**Correct result:** The list `On no beat` does not show `Santiago prays`. In the element list, `Santiago prays` has no text after its name.

### BT-034: Action 5 with a button

1. Select the beat `Ice Monster (8)`. Select `He sails homeward` in the element list.
2. Select the button `Assign Beat`.
3. Select `Move here` in the dialog.

**Correct result:** The beat `Ice Monster (8)` shows `He sails homeward`. The beat `Plot Turn II (7)` shows `Unassigned`.

### BT-035: Action 6, a change of the beat sheet

1. Select `Save The Cat (Mini)` in the box `Beat Sheet`.
2. In the dialog, select `Confirm`.

**Correct result:** Each beat shows `Unassigned`. The list `On no beat` shows each element that was on a beat before, in the sequence of the beats.

### BT-036: Action 7, the commands for a beat

1. Select the first beat. Select the button `Add Beat`.
2. Select the button `Move Beat Down`.
3. Select the button `Delete Beat`. Select `Delete` in the dialog.

**Correct result:** After step 1, a beat with the name `New Beat` is below the first beat. After step 2, `New Beat` is one position lower. After step 3, `New Beat` is not in the list.

### BT-037: The Structure tab at the dimensions of the manual

1. Make the dimensions of the main window 1036 by 744.
2. Examine the Structure tab.

**Correct result:** The Structure tab shows the box `Beat Sheet`, the beats, the list `On no beat` and the nine buttons. The Structure tab also shows the element list and the two fields below the lists. Each control shows fully.

## Results

| The test | Windows | macOS |
|----------|---------|-------|
| BT-001 | | |
| BT-002 | | |
| BT-003 | | |
| BT-004 | | |
| BT-005 | | |
| BT-006 | | |
| BT-007 | | |
| BT-008 | | |
| BT-009 | | |
| BT-010 | | |
| BT-011 | | |
| BT-012 | | |
| BT-013 | | |
| BT-014 | | |
| BT-015 | | |
| BT-016 | | |
| BT-017 | | |
| BT-018 | | |
| BT-019 | | |
| BT-020 | | |
| BT-030 | | |
| BT-031 | | |
| BT-032 | | |
| BT-033 | | |
| BT-034 | | |
| BT-035 | | |
| BT-036 | | |
| BT-037 | | |
