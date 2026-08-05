# Tournament

Runs a **tournament** between several players: build the roster, draw the bracket,
chain the matches, show standings and the champion. Available formats: single
elimination, round-robin and king of the hill.

A mod for **FortRise 5** (>= 5.3.3). The FortRise 4 version (`tf-mod-fortrise-tournament`) is no longer maintained: fixes and new features only land in this repository.

## Installation

1. Install FortRise 5 and start the game through `FortRise.exe`.
2. Install the mods this one depends on first: **CustomName**.
3. Copy `release/tournament` (or the shipped folder) into `<TowerFall>/FortRise/Mods/`.

Settings are under **Options > Mods > Tournament**.
Data and log files live in `<TowerFall>/FortRise/Saves/Tournament/` and `<TowerFall>/FortRise/Logs/`.

## Usage

A **TOURNAMENT** button shows up on the main menu, between VERSUS and CO-OP.

### 1. The roster

The selection screen lists the known players, read from
`<TowerFall>/FortRise/Saves/Tournament/tournament_players.json`.

| Input | Effect |
|-------|--------|
| Up / Down | move through the list |
| A | add the player to the tournament |
| B | remove the last one added, or leave when the list is empty |
| **Y** | **create a new player** (virtual keyboard) |
| Start | confirm and move on to the settings |

The virtual keyboard accepts physical typing - keyboard layout honoured, AZERTY
included - as well as controller navigation (directions, A to type, RB to delete,
Start to confirm, B or Escape to cancel). A new name is appended to
`tournament_players.json` right away.

The screen stays reachable even when the file is missing or too short: it is the
only way to fill it from inside the game.

### 2. Tournament settings

Match format (FFA with 2, 3 or 4), tournament type, rounds needed to win a match,
and map choice (manual, random or fixed tower).

### 3. The bracket

The bracket shows the matches and their winners. Start launches the current match.

### 4. Before each match

An announcement screen introduces the players, then the **controller assignment
screen** comes up: each player takes the slot with their name, using the controller
already in their hands.

| Input | Effect |
|-------|--------|
| Left / Right | move to a free slot |
| A | take the highlighted slot |
| Left / Right (slot taken) | change archer |
| RB | archer alt skin |
| A | mark yourself ready |
| B | give the slot back, or return to the bracket when you have none |

This screen is what removes the controller passing between matches: the name follows
the controller, not the other way round. The controller icon and its number are
drawn under each slot. The chosen archer is remembered and offered again next match.

The game mode is reset to **last man standing** when entering the tournament and
before each match, so a previously played mode does not carry over.

## Data

`<TowerFall>/FortRise/Saves/Tournament/` holds the roster
(`tournament_players.json`) and the tournament in progress, picked up again on the
next launch.

## Build / deployment

| Script | Purpose |
|--------|---------|
| `script/release.bat` | build, then assemble into `release/` |
| `script/deploy.bat` | copy `release/` into the TowerFall `Mods` folder |
| `script/release_deploy.bat` | both, one after the other |

Paths (game folder, module name) are set in `script/config.bat`.
