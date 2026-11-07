# Landmarks – Environmental Cognition Lab (`azras_branch`)

Unity VR project for spatial navigation experiments in Professor Derek Huffman's Environmental Cognition Lab at Colby College. It is built on the **Landmarks** VR experiment framework (Michael J. Starrett et al.), with lab-specific tasks added by Derek Huffman and Ainsley Bonin.

This branch (`azras_branch`) starts from **`ainsleys_branch`**, Ainsley Bonin's thesis project (2023–2024, "MapsGraphsThesis"). It will be adapted for Azra Karatan's independent EEG/VR study on whether frontal-midline theta tracks distance, speed or time during navigation.

> **Status:** No changes from `ainsleys_branch` yet. Everything below describes Ainsley's thesis setup as the starting point.

---

## Requirements

- **Unity 2020.3.12f1** (open the project with exactly this version through Unity Hub)
- **SteamVR** and a SteamVR-compatible headset (uses OpenVR and Unity XR Management)
- **Python 3** with `numpy` and `pandas` (only for generating participant files)
- Windows lab machine for VR sessions (the project was also edited on macOS)

---

## Getting started

1. Clone or download the repo and switch to `azras_branch`.
2. Open the project folder in Unity Hub with Unity 2020.3.12f1. The first open takes a while while Unity rebuilds the `Library/` folder.
3. **Fix the OpenVR package path** (see Known issues #1) if Unity reports a missing package.
4. Open the main scene:
   `Assets/Landmarks/Scenes/HuffmanLab/AB_Thesis/Ainsley_Thesis.unity`
5. Press **Play**. The first screen asks for a participant ID, which picks the participant files to load.

---

## Project layout

Only the folders you're likely to touch are listed. Everything else is third-party assets (SteamVR, Standard Assets, object models, etc.).

```
Assets/Landmarks/
├── Scenes/HuffmanLab/
│   ├── AB_Thesis/Ainsley_Thesis.unity   ← Ainsley's thesis experiment
│   ├── PPT_experiment/                  ← other lab experiments
│   └── eeg_faces_objects_inverted/
├── Scripts/
│   ├── Experiment.cs                    ← experiment manager; sets where data is saved
│   ├── AKB_Door.cs                      ← opening doors between rooms
│   ├── generate_paths.py                ← makes participant files (Python)
│   └── ExperimentTasks/                 ← one script per task type
├── Prefabs/                             ← TASK_* prefabs used in the scene
└── TextFiles/
    ├── *.txt                            ← on-screen instructions
    └── ParticipantFiles/                ← per-participant trial lists (s0–s49)
data/                                    ← output data (created at runtime)
```

---

## Ainsley's thesis task

The environment is a **5 × 5 grid of rooms** (numbered 0–24) connected by doors (lettered `a`–`nn`). The full room and door coding is in the docstring of `generate_paths.py`.

```
0   a   1   b   2   c   3   d   4
e       f       g       h       i
5   j   6   k   7   l   8   m   9
...
```

Each room holds an everyday object (apple, teapot, clock, …). Participants learn **7 fixed routes** through the grid, then complete distance and map tasks.

### Task sequence in the scene

| Order | Scene object | Main script(s) | What it does |
|---|---|---|---|
| 1 | `InputParticipantID` | `InputParticipantID.cs` | Experimenter types the participant number |
| 2 | `InitializeDoors` | `ReadDoorsFromTxt.cs` | Loads which doors open on each trial |
| 3 | `TASK_NavRDJ` | `DistanceAndNavTask.cs`, `RDJ.cs` | Navigate a route, then make a relative distance judgment between two objects |
| 4 | `TASK_NavigationTask` | `DistanceAndNavTask.cs` (`shortcut = true`) | Shortcut navigation between room pairs |
| 5 | `TASK_ADJ` | `Abs_Distance_Judgment.cs` | Absolute distance judgment between room pairs (slider) |
| 6 | `TASK_Mappit` | `MapTestTask.cs` | Place objects on a map; scored against criterion |

*Check the order against the scene Hierarchy in Unity before relying on it.*

### Participant files

Each participant needs five files in `Assets/Landmarks/TextFiles/ParticipantFiles/`, named by participant number (e.g. `s12_…`):

| File | Read by | Contents |
|---|---|---|
| `sN_paths.txt` | `ReadRoomsFromTxt.cs` | Room sequence for each navigation trial |
| `sN_doors.txt` | `ReadDoorsFromTxt.cs` | Doors to open for each trial |
| `sN_rdj.txt` | `ReadRDJFromTxt.cs` | Object pair for each relative distance judgment |
| `sN_adj.txt` | `ReadADJFromTxt.cs` | Room pairs for the absolute distance judgment |
| `sN_nav2.txt` | `ReadShortcutsFromTxt.cs` | Room pairs for shortcut navigation |

Format: one value per line, with a blank line between trials.

Files for `s0`–`s49` already exist (`s0` is for testing). To make new ones, edit the loop at the bottom of `generate_paths.py`, then run it **from inside `Assets/Landmarks/Scripts/`** (it uses relative paths):

```bash
cd Assets/Landmarks/Scripts
python generate_paths.py
```

Each participant gets 70 navigation trials (7 routes × 2 directions × 5 repetitions, shuffled within each repetition) and 28 ADJ and shortcut trials (4 conditions × 7).

---

## Data output

Set in `Experiment.cs`:

| Running from | Saved to | Log file name |
|---|---|---|
| Unity Editor (Play button) | `data/tmp/` in the project folder | `test.log` plus per-task CSVs like `TASK_Mappit.csv` |
| Built app (`.exe`) | `data/` next to where the app is launched | `MapsGraphsThesis_p_<date_time>.log` |

Editor runs reuse the same file names, so copy anything you want to keep before the next run.

---

## Known issues

Found while writing this README; none fixed yet.

1. **OpenVR package points to Ainsley's laptop.** `Packages/manifest.json` has
   `"com.valvesoftware.unity.openvr": "file:/Users/ainsleybonin/Documents/git_repos/Landmarks/Packages/com.valvesoftware.unity.openvr-1.1.4.tgz"`.
   The `.tgz` is already in `Packages/`, so change it to
   `"file:com.valvesoftware.unity.openvr-1.1.4.tgz"`.
2. **Participant files only load in the Editor.** The `Read*FromTxt.cs` scripts open `"Assets/Landmarks/TextFiles/ParticipantFiles/..."`, a path that doesn't exist in a built app. Run sessions from the Editor, or move the files to `StreamingAssets/` before building.
3. **RDJ left/right is never swapped.** In `generate_paths.py`, `coin = np.random.choice(1, 1)` always returns 0, so object order is never flipped. It should be `np.random.choice(2, 1)`. This affects the existing `sN_rdj.txt` files.
4. **`generate_adj_revised` writes outside the repo.** It saves condition CSVs to `../../../../../MapsGraphsThesis/...`. That folder must exist or the script crashes.
5. **Outdated docstrings** in `generate_paths.py` say "3 repetitions of 7 paths = 21 total"; the code actually makes 70.
6. **Stray files committed:** `.DS_Store` files, `mono_crash.*.json`, and an empty `VR` file in the root. These are safe to delete.

---

## Changes on this branch

*Azra: log your changes here as you go (date, what changed, why).*

| Date | Change |
|---|---|
|Added AK_SpeedPilot| 11/07/26 |

---

## Credits

- **Landmarks framework:** Michael J. Starrett, Jared D. Stokes and contributors
- **Lab tasks and supervision:** Derek J. Huffman, Colby College
- **Thesis environment and tasks (`AKB_*`, `RDJ.cs`, `DistanceAndNavTask.cs`, `generate_paths.py`):** Ainsley Bonin, 2023–2024
- **This branch:** Azra Karatan
