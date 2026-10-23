# Issue #1421 Milestone 5: A video from one script

**Status:** draft for critique by Shipping Sentinel, then approval by Terry  
**Date:** 2026-10-07  
**Issue:** [StoryCAD #1421](https://github.com/storybuilder-org/StoryCAD/issues/1421)  
**Branch:** `issue-1421-video-m5`  
**Code home:** `StoryCADAutomation`  
**Builds on:** `devdocs/issue_1421_dsl_design.md` (the script language and the presentation profile, Milestone 4)  
**No ADR.** This milestone extends the presentation profile that ADR-009 already covers.

---

## 1. Document kind

| Question | Answer |
|----------|--------|
| What is this file? | The design for #1421 Milestone 5. |
| What language? | ASD-STE100 for this design. |
| What does this milestone change? | One command makes a finished YouTube video from one script. |
| What does this milestone not change? | The test profile. Scripts without video verbs. |

---

## 2. Problem

Milestone 4 records a demo, but the result is not a finished video.  
The run stops for each narrate line, so the screen does not move while the voice speaks.  
Terry rejected that result as stilted.  
The recording has no voice, no title, no text cards, and no closing card.  
A person must join the parts by hand in an editor.

---

## 3. Goal

One `.scs` file holds all parts of a video.  
One command makes the video from that file.  
The output is an `.mp4` file that Terry can put on the YouTube channel without more work.  
The voice speaks while the app moves.  
A change to the video starts as a change to the brief, then the command makes the full video again.

---

## 4. Terms

| Term | Meaning |
|------|---------|
| **Brief** | A spreadsheet with three columns: Step, Action on Screen, Narration. One row is one step. Terry or an LLM session writes it. This is the plan stage. |
| **Video script** | A `.scs` file that the `video` command can run to the end. An LLM session writes it from the brief. |
| **Step** | The statements from one `step` line to the next `step` line. One step agrees with one row of the brief. |
| **Clip** | One audio file that speaks one `narrate` line in the narrator voice. |
| **Narrator voice** | A cloned voice in ElevenLabs. The voice of a person who gives consent. |
| **Card** | A full frame that shows only an image or only text. The app does not show behind a card. |
| **Assets folder** | A folder outside the repository. It holds the logo, the intro music, and the images for `show image`. |
| **Assembly** | The ffmpeg process that joins the recording, the cards, and the clips into the `.mp4` file. |

---

## 5. Process

1. Write the brief. Write the Narration column first. Then write the Action on Screen column.
2. Terry reads the brief and approves it or changes it.
3. An LLM session writes the video script from the brief.
4. Run `check`, then run the script in the test profile. The script must pass.
5. Run `voice` to make the clips.
6. Run `video` to make the `.mp4` file.
7. Terry watches the video one time.
8. If Terry wants a change, go back to step 1. The command makes the full video again. This milestone does not patch a video.

---

## 6. Script changes

### 6.1 New statements

```
title "A Five-Minute Introduction to StoryCAD"   # the title card; first statement after script
step "The main window" hold 2                    # hold: extra seconds at the end of the step
show image "apple-pie.png"                       # an image card from the assets folder
show text "Free and open source"                 # a text card
closing "Like and subscribe"                     # the closing card; last statement
narrate "The extension dot S-T-B-X." caption "The extension .stbx."   # caption: the text for the captions
```

### 6.2 Rules

1. `title` is optional. If it is present, it is the first statement after `script`.
2. `closing` is optional. If it is present, it is the last statement.
3. In a step, all `narrate` lines come immediately after the `step` line.
4. A step with `show` has only `narrate` lines and one `show`. It has no other verb.
5. `show image` names a `.png` or `.jpg` file in the assets folder. The name has no folder part.
6. `hold` takes a number of seconds from 0 to 30.
7. `check` reports an error for each broken rule.
8. In `show text`, `\n` starts a new line on the card. Use it to put one list item on each line (#1626, Terry, 2026-10-20).
9. `caption` is optional on a `narrate` line. It gives the text for the captions when the spoken text is not correct as written text. Without `caption`, the captions use the `narrate` text (#1626, Terry, 2026-10-23).
10. Two `narrate` lines with the same text must have the same caption text. One text has one clip.

### 6.3 The test profile

The test profile writes each new statement to the log.  
The test profile does not wait for `hold`, cards, or narration.  
A video script is also a regression test. If a StoryCAD screen changes, the test run fails first.

---

## 7. Step timing in the presentation profile

1. At the start of a step, the narration and the actions start together.
2. The narration of a step is its clips, one after the other.
3. The step ends when the narration and the actions are both complete.
4. Then the run waits for the `hold` seconds.
5. Then the next step starts.

If the narration is longer, the screen stays on the last action until the voice stops.  
If the actions are longer, the voice is silent until the actions stop.

The runner does not play the clips. The runner reads the length of each clip and writes the start time of each clip.  
If a clip is missing, the runner uses the reading time from Milestone 4: 0.4 s for each word, minimum 2 s.  
Thus a script can run before the narrator voice is available.

A `show` step does no action. It lasts as long as its narration, plus `hold`.

---

## 8. Commands

### 8.1 voice

```
StoryCADAutomation voice <script.scs> --out <dir>
```

1. For each `narrate` line, the command makes one clip in `<dir>/voice/`.
2. The clip name comes from the text and the voice. If a clip with that name exists, the command does not make it again.
3. The command reads the API key from `ELEVENLABS_API_KEY` and the voice from `STORYCAD_VOICE_ID`.
4. If a variable is missing, the command stops with exit code 3.

### 8.2 video

```
StoryCADAutomation video <script.scs> --out <dir> --assets <dir>
```

1. The command sets the StoryCAD window so that StoryCAD has a layout of 1920x1080 at the display scale. At 100% scale, the visible area is 1920x1080 pixels. At a larger scale, the area is larger by the same factor. For example, the area is 2880x1620 pixels at 150% scale. The window is at the top left of the primary display.
2. The command starts ffmpeg. ffmpeg records only that area of the screen.
3. The command runs the script in the presentation profile.
4. The command stops ffmpeg.
5. If a step fails, the command deletes the recording and stops with exit code 1. If the launch fails, the exit code is 3.
6. The recording starts at `launch`. Put `narrate` and `show` statements after `launch`. If they are before `launch`, `video` stops with exit code 2.
7. If all steps pass, assembly makes `<dir>/<script name>.mp4`.
8. Then the command writes the captions to `<dir>/<script name>.srt`. Section 9.1 gives the rules.

The command replaces the OBS recording of Milestone 4.
YouTube can make captions from the audio, but these captions spell names and terms incorrectly.
The clips speak the `narrate` text, so the command makes the captions from that text (#1626, Terry, 2026-10-23).
The upload command sends the `.srt` file when `meta.json` names it in `captions`.

---

## 9. Assembly

The `.mp4` file has these parts, in this sequence:

1. **Title card.** The title text above the logo. The intro music plays under the card. The card lasts as long as the music. Without a music file, the card lasts 5 s.
2. **Recording.** Assembly puts each clip at its start time. For each `show` step, the card covers the recording for the full step.
3. **Closing card.** The `closing` text. The card lasts 10 s.

All cards use one style:

| Property | Value |
|----------|-------|
| Background | Solid dark color, full frame |
| Text | White, Segoe UI, 64 px, centered on both axes |
| Image | Centered, scaled to fit the frame |

Assembly scales the recording to 1920x1080.

The assets folder holds these files:

| File | Use |
|------|-----|
| `logo.png` | The logo on the title card |
| `intro-music` with an audio file extension, for example `intro-music.mp3` | The intro music |
| The files that `show image` names | The image cards |
| Each `.stbx` file | An outline that the script opens. Before StoryCAD starts, the `video` command copies it to `{scratch}/outlines` (#1626, Terry, 2026-10-20). |

The output is 1920x1080, H.264 video, AAC audio.

If the logo or the music file is not in the assets folder, assembly makes the title card without it. The video command does not fail.

### 9.1 Captions

The `.srt` file is a SubRip file in UTF-8. The YouTube API does not set the time of a caption without a time, so each caption has a time.

1. Each clip gets captions. The text is the `caption` text of its `narrate` line, or the `narrate` text. A cue without a clip gets no caption.
2. The captions of a clip start at the start of the clip in the `.mp4` file. This time is the start time of the clip in the recording, plus the start of the recording in the `.mp4` file. If there is a title card, the recording starts at the title card length minus the 0.5 s crossfade.
3. A caption has a maximum of 2 lines. A line has a maximum of 32 characters. These limits come from the DCMP Captioning Key.
4. A caption breaks only between words. When the next word does not fit, the caption ends after its last sentence. If no sentence ends in the caption, the caption ends after its last clause. A break after a sentence or a clause must keep a minimum of 20 characters before it.
5. A name of words that start with a capital letter stays on one line, if the name fits on one line.
6. Each caption of a clip gets a part of the clip time. The part agrees with the number of characters in the caption.
7. A caption shows for a minimum of 1.33 s and a maximum of 6 s. A longer caption stops at 6 s. A shorter caption shows for 1.33 s, but it stops before the next caption starts.
8. If the title card has music, a `[music]` caption shows for the length of the title card.
9. A caption after the end of the recording is not in the file.

---

## 10. Dependencies and security

1. ffmpeg must be on the presentation machine. The environment variable `STORYCAD_FFMPEG` gives the full path of the ffmpeg program. If the variable is missing, the `video` command stops with exit code 3. The command does not use ffprobe. CI does not use ffmpeg.
2. ElevenLabs is a paid vendor. Terry has the account.
3. The API key stays in an environment variable. Do not put it in a file in a repository.
4. The assets folder stays outside the repository. Do not commit music or images with unknown rights.
5. The `video` command needs a primary display that can hold the window of section 8.2, step 1, out of the taskbar. If the display is smaller, the command stops with exit code 3. The display scale can have any value. (Terry, 2026-10-13: try this method. If the text in the video is not sharp, use 100% scale.)

---

## 11. Work items

Terry's time is an estimate.

| # | Item | Terry's time |
|---|------|--------------|
| 1 | Script changes and `check` rules (section 6), with unit tests | none |
| 2 | Step timing (section 7), with unit tests for the timing rules | none |
| 3 | `voice` command (section 8.1) | none |
| 4 | `video` command and assembly (sections 8.2 and 9) | none |
| 5 | Remove the `.srt` output of Milestone 4. Change the OBS text in `issue_1421_dsl_design.md`. (#1626 adds a new `.srt` output from the clip times, section 9.1.) | none |
| 6 | Brief for the intro video, from the narration and actions in `Intro-Video.scs` | 10 min to read and approve |
| 7 | New `Intro-Video.scs` from the brief, by an LLM session | none |
| 8 | Done: three consecutive `video` runs pass on Brigid, and Terry approves one `.mp4` | about 4 min to watch, plus notes |

Before the narrator voice is available, items 1 to 7 use a stock ElevenLabs voice or the reading-time fallback.

---

## 12. Open items

| Item | Owner |
|------|-------|
| Narrator samples and consent in ElevenLabs | The narrator, with Terry |
| Whether the narrator gives a name in the first line | The narrator |
| Logo file and intro music file in the assets folder | Terry or Shipping Sentinel |
| When a video uses the intro music, put this credit line in its YouTube description: "Music: 'Dance of the Clouds' by Alexsey Zakharenko (Origen), used with permission." | The person who uploads the video |

---

## 13. Out of scope

1. Patch of a finished video (replace, insert, or remove a step).
2. YouTube Shorts.
3. X.
4. Upload, thumbnail, description, tags, and promotion.
5. Rights to the intro music.
