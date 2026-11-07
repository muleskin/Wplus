# Wplus — design sheet

The numbers, colours, timings and text rules Wplus is drawn and behaves by.
Values marked **estimated** were chosen by eye rather than fixed by design.

## Window and theme

| Value | Number |
|---|---|
| Default window size | 736 × 464 |
| Tile corner radius | 14 |
| Settings tab column width | 144 |
| Default theme | Light (Dark available) |
| Success colour | #13cd00 |
| Warning colour | #fed60a |
| Alert colour | #FD463A |
| Accent / progress fill | #1771fc on a #d8d8d8 track (light), #404040 track (dark) |
| Dark window | #1f1f1f, text #d2d2d2 |
| Light text | #4c4c4c |

Theme colours by name:

| Key | Light | Dark |
|---|---|---|
| WindowBackgroundColor | #f6f6f6 (**estimated**) | #1f1f1f |
| TileBackgroundColor | #ffffff | #2f2f2f |
| TileBackgroundHoverColor | #fafafa | #323232 |
| TileTitleMainColor / CommonTextForegroundColor | #4c4c4c | #c9c9c9 |
| ButtonLinkForegroundHoverColor | #1771fc | — |

## Tile layout (device area)

Positions are inside a 468 × 376 device area with 12 px gaps.

| Devices | Tiles (x, y, w, h) |
|---|---|
| 1 | (0,0,424,376) full |
| 2 | (0,0,228,376), (240,0,228,376) |
| 3 | (0,0,228,376), (240,0,228,182), (240,194,228,182) |
| 4 | (0,0,228,182), (240,0,228,182), (0,194,228,182), (240,194,228,182) |

Tall/full tiles: icon 118, icon margin top 84 / bottom 42, title 18 pt.
Small tiles: icon 64, margin 36 / 24, title 14 pt.

Drop-arrow spacing: icon height > 100 → top 28 / bottom 56; otherwise top 22 /
bottom 8.

Side column: 228 wide. The Local Folder tile is 376 tall on its own, or 182
tall with the Library tile below it at y = 194 (182 tall); switching animates
over 0.45 s. Settings ▸ View has "Show the Library tile" to turn this on or
off. (The column's left/top offsets, 12 and the 64 px header, are
**estimated**.)

Empty device area: "Connect your device via USB or wake from sleep for Wi-Fi",
18 pt, margin 80/0/80/20, above three pulsing rings of 23/39/55 px on a 2 s
loop (scale to 1.12 inner, 1.08 others; start delays 0/0.2/0.4 s).

## Timing

| Animation | Value |
|---|---|
| Progress bar glide | 900 ms, cubic ease-out |
| Tile resize/move | 450 ms, cubic ease-out |
| Drop overlay fade | 150 ms |
| First device fade-in | 250 ms delay, 750 ms |
| Ctrl/Alt highlight | 150 ms |
| Browse dialog title | "<tile> Browse files" |

Cubic ease-out: `f(t) = 1 - (1 - t)^3`.

## Text rules

- **File sizes**: divide by **1000** while the value rounded to *d* decimals is
  ≥ 1000; units bytes, KB, MB, GB, TB, PB, EB, ZB, YB; *d* = 1 by default,
  2 past GB; format `0.#` (trailing zeros dropped) + space + unit.
- **Device names** over 28 characters show the first 25 + "...".
- **Counts**: "1 file", "N files".
- **Device capacity label**: total capacity ÷ 1,073,741,824, rounded; the
  first bucket it fits: ≤8 "8 Gb", ≤16 "16 Gb", ≤32 "32 Gb", ≤64 "64 Gb",
  ≤128 "128 Gb", ≤256 "256 Gb", ≤512 "512 Gb", ≤1024 "1 Tb", ≤2048 "2 Tb",
  otherwise empty. Free space is tracked separately and drives the "Not enough
  free space" state.
- **Tile wording**: "Drop files here to convert & transfer into {0}",
  "Drop files here to add to Local Folder", "Drop to add to", "Preparing files...",
  "Preparing to convert...", "Transfer paused.", "Cancel transferring.",
  "Not enough free space", "Unlock device to continue.",
  "Connect your device via USB.", "{0} disconnected.", "Show in folder.",
  "Press here to find out where the transferred files are located on your device."

## Content types and formats

Content types: Music, Video, TV Show, Ringtones, Audiobooks, PDFs, EPUBs.

| Group | Extensions |
|---|---|
| Audio | mp3 aac m4a wav flac ape aif aiff wma ogg oga dff tta tak wv dsf cue |
| Video | mp4 m4v mov avi mkv wmv dv m2v webm mpeg mpg flv 3gp m2ts ts mpe mts rm rmvb vob |
| Subtitles | srt ass ssa sub |
| Audiobook | m4b |
| Ringtone | m4r |
| Books | epub, pdf |

## Tile states

Normal, Preparing, Transferring, Transfer paused, Success, Error, Out of
space ("Not enough free space"), Untrusted ("Unlock device to continue.").
Connection kinds: None, Can wake up, USB, Wi-Fi.

## What is real vs. simulated

- **Local Folder tile — real.** Dropped or browsed files are copied into the
  chosen folder with live progress. No conversion is done (files are copied
  as they are). Existing files are never overwritten; copies get " (2)" etc.
- **Library tile — real.** Media is filed into the user's Music / Videos /
  Documents folders by type.
- **Device tiles — simulated.** Real transfer needs a phone's USB device
  stack; Wplus runs demo devices with an **estimated** speed of 25 MB/s and
  an **estimated** 0.8 s "Preparing files..." step.
