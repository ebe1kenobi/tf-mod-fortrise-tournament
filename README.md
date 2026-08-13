# Tournament

Runs a **tournament** between several players: build the roster, draw the bracket,
chain the matches, show standings and the champion. Available formats: single
elimination, round-robin and king of the hill.

A mod for **FortRise 5** (>= 5.3.3). The FortRise 4 version (`tf-mod-fortrise-tournament`) is no longer maintained: fixes and new features only land in this repository.

## Installation

1. Install FortRise 5 and start the game through `FortRise.exe`.
2. No other mod is required. **Archer** is optional - see
   [Where the names come from](#where-the-names-come-from).
3. Copy `release/tf-mod-fortrise-tournament` into `<TowerFall>/FortRise/Mods/`.

Settings are under **Options > Mods > Tournament**.
Data and log files live in `<TowerFall>/FortRise/Saves/Tournament/` and `<TowerFall>/FortRise/Logs/`.

## Usage

A **TOURNAMENT** button shows up on the main menu, between VERSUS and CO-OP.

### 1. The roster

The selection screen lists the known players. The left column is named after where
those names come from.

| Input | Effect |
|-------|--------|
| Up / Down | move through the list |
| A | add the player to the tournament |
| B | remove the last one added, or leave when the list is empty |
| **Y** | **create a new player** (virtual keyboard, `JSON` source only) |
| **Left / Right** | **switch source** (only when Archer is installed) |
| Start | confirm and move on to the settings |

The virtual keyboard accepts physical typing - keyboard layout honoured, AZERTY
included - as well as controller navigation (directions, A to type, RB to delete,
Start to confirm, B or Escape to cancel). A new name is appended to
`tournament_players.json` right away.

The screen stays reachable even when the file is missing or too short: it is the
only way to fill it from inside the game.

#### Where the names come from

Two sources, and the choice is yours:

| Source | Names | Adding a name |
|--------|-------|---------------|
| `JSON` | `<TowerFall>/FortRise/Saves/Tournament/tournament_players.json` | Y, on the spot |
| `PROFILES` | the profiles of the **Archer** mod | in the Archer menu |

> The mod that holds the profiles used to be called **Profiles**. It is **Archer**
> now - the name, the folder and the API key alike. Anywhere this page says `PROFILES`
> in capitals it means the *source*, which kept its name because it is a list of
> profiles; the *mod* that publishes it is Archer.

**Archer is optional.** Without it the tournament behaves exactly as it always has, on
its own file; left/right do nothing and no source is offered, because there would be
only one.

The choice is remembered in `tournament_players.json`, next to the names. A roster
already filled in keeps working untouched: `JSON` stays the default.

Switching sources **keeps the players already picked** - a tournament is made of
names, wherever they come from, so a grid can draw from both.

On the `PROFILES` source, a player brings their whole profile into the match: colours,
sounds, portraits and key mapping. The profile is attached when the match starts, so
nothing has to be picked again on a screen the tournament never shows.

Their favourite archer is **offered** on the controller screen, not imposed - and it
matters more than a preference: a profile's colours are stored for one archer and one
costume, and do not apply to another character. Change archer there and you play that
archer, in its own colours.

A name that matches no profile detaches the slot, so the previous match's player does
not leave their colours behind.

The roster is asked for as an interface of its own, and that is what makes it safe: an
Archer that does not publish it simply answers nothing, without costing the in-game
player names, which have always worked.

**No minimum version is required** any more. There was one - 1.16, the version the
roster appeared in - and it turned into a trap the day every mod in the repository
restarted from a common number: 1.1.0 is *lower* than 1.16.0, so the `PROFILES` source
vanished from the selection screen while the interface was in fact right there. The
shape of the members is a sounder test than the number, and it is the one the interop
already makes.

When no roster is published, the screen says so by showing `JSON (NO PROFILES)`.

### 2. Tournament settings

Six options, navigated with up/down and adjusted with left/right:

| Option | Purpose |
|--------|---------|
| TOURNAMENT MODE | single elimination, round-robin or king of the hill |
| FFA FORMAT | players per match (2, 3 or 4) |
| ROUNDS TO WIN | rounds needed to win a match |
| MAP | manual pick, random, or a fixed tower |
| GAME MODE | same list as the versus mode button: Last Man Standing, Head Hunters, Team Deathmatch, then every mode added by a mod (Respawn, PlayTag, Speed Run...) |
| VARIANTS | shows how many are active; **A** opens the variants screen |

**Start** confirms and draws the bracket.

#### Variants screen

Opened with **A** on the VARIANTS line. Icons are laid out in a grid grouped by
header, exactly like the versus variants screen, and the name of the highlighted
one is shown underneath.

| Input | Effect |
|-------|--------|
| Up / Down | move between rows, skipping headers |
| Left / Right | move along a row, wrapping onto the next one |
| A | toggle the highlighted variant |
| B | back to the settings |

A coloured icon with a green frame means the variant is on, a greyed one means off.
The first line, `RESET ALL VARIANTS`, turns everything off at once.

Starting a new tournament pre-selects **NO AUTOBALANCE**, and nothing else.

It used to pre-select the game's whole *tournament rules* preset, which also brings
`SYMMETRICAL TREASURE` along - a variant nobody had asked for, that then applied to
every match of the tournament. Only the one with a reason to be imposed is left:
without it the game hands arrows and shields to whoever is losing, which has no
place in a competition. The rest is a choice, and a choice gets ticked.

Resuming a saved tournament keeps its own variants instead.

#### Rules are pinned to the tournament

The chosen game mode and variants are stored with the tournament and re-applied
before **every** match. Leaving the tournament to play a versus in another mode, or
with other variants, does not change the rules of the tournament in progress.

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

**Two players may take the same archer.** The default still spreads them out - being
put on a duplicate without asking would be a poor start - but nothing stops anyone
from cycling onto an archer a neighbour already holds. Recoloured profiles tell them
apart; two identical archers with no recolouring stay indistinguishable, and that is
the player's call.

**A slot holding a profile has its archer locked**, marked `PROFILE` under the
portrait. Left/right and the costume toggle refuse it. A profile's colours are stored
for one archer and one costume: changing them would not give a differently coloured
archer, it would give an archer with no colours at all. Their profile archer wins over
any archer remembered from an earlier match.

The game mode and variants chosen in the settings are re-applied here, so a versus
played in between does not carry over into the tournament.

## Data

`<TowerFall>/FortRise/Saves/Tournament/` holds the roster
(`tournament_players.json`) and the tournament in progress, picked up again on the
next launch.

`tournament_players.json` carries both the names and the chosen source:

```json
{
  "players": [ "ERIC", "LEO" ],
  "source": "JSON"
}
```

A file without `source` - every file written before 1.1.0 - reads as `JSON`.

## Build / deployment

| Script | Purpose |
|--------|---------|
| `script/release.bat` | build, then assemble into `release/` |
| `script/deploy.bat` | copy `release/` into the TowerFall `Mods` folder |
| `script/release_deploy.bat` | both, one after the other |

Paths (game folder, module name) are set in `script/config.bat`.
