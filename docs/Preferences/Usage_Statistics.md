---
title: Usage Statistics
layout: default
nav_enabled: true
nav_order: 76
parent: Home
has_toc: false
---
## Anonymous Usage Statistics

StoryCAD can send a small amount of anonymous usage information back to the
StoryCAD team. We use this data to understand which features people actually
use, how long typical editing sessions are, and where to spend our development
effort.

Sharing this information is **entirely optional**. StoryCAD works exactly the
same whether you opt in or not. If you don't want to share anything, simply
leave the checkbox unchecked — either during first-run setup or on the *Other*
tab of the Preferences dialog.

### What we collect

When you opt in, each time you close StoryCAD we send one record that
contains:

- **Session timing** — when StoryCAD was started and stopped, and the total
  clock time for the session.
- **Outlines you worked on** — for each outline opened in the session: the
  outline's internal ID (a random identifier; not the file name), when it was
  opened and closed, how many story elements it contained, how many you added
  or deleted, and the outline's genre and story form (e.g. "Mystery", "Short
  Story"). These are the same values you pick from the drop-downs on the Story
  Overview form.
- **Features used** — the names of StoryCAD features you used (for example
  *Conflict Builder*, *Key Questions*, *Print Reports*) and how many times you
  used each one during the session.
- **An anonymous ID** — a random identifier (a GUID) generated on your
  computer the first time you opt in. This lets us tell that two sessions came
  from the same installation without knowing anything about who you are.

### What we do *not* collect

We deliberately do **not** collect any of the following:

- The **content** of your outlines — no character names, scene descriptions,
  story text, notes, or anything else you type into StoryCAD.
- The **titles** or **file names** of your outlines.
- File paths on your computer.
- Your **real name** or **email address**. (If you've given these to StoryCAD
  for error reporting or the newsletter, they live in different preferences
  and are never attached to your usage data.)
- Your **IP address**. The usage data is delivered over a secure connection
  and the server does not log the address it came from.

Usage data is not sold, shared with advertisers, or given to anyone outside
the StoryCAD team.

### How your identity is protected

The anonymous ID is a randomly generated GUID — a string of letters and
numbers like `3a7b1c4e-9f22-4e7c-a1d3-8b6e2f5a9c10`. It is **not** derived
from your name, email, machine name, or anything else that could identify
you.

If you change your mind and turn usage statistics **off**, your anonymous ID
is erased from your preferences file. If you later turn it back on, StoryCAD
generates a brand-new anonymous ID, so the two stretches of data cannot be
linked together.

### Turning it on or off

You'll be asked about usage statistics the first time you run StoryCAD. You
can change your answer at any time:

1. Click **Preferences** on the menu bar.
2. Select the **Other** tab.
3. Check or uncheck **Send anonymous usage statistics**.
4. Click **Save**.

The change takes effect for the next session. Data already sent from earlier
sessions is not retrieved, but no new data will be sent while the option is
off.

### Why we ask

Knowing which tools are used heavily
and which are rarely touched helps us decide what to improve, what to
document better, and what new features to build. Sharing usage data is a
direct, low-effort way to help shape StoryCAD's future — thank you if you
choose to do so.
