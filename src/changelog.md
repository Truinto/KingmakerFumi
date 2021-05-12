# Changelog

## [1.0.12]

### Added
- added option to remove Craft Magic Item caster level prerequisite
- added ability to toggle kinetic whip
- added overpowered debug item (cannot be looted, only accessible with Bag of Tricks)
- added style feats to Warpriest and Ninja selection

### Fixed
- mobile gathering (short) must be used for moving now
- added missing style flag to style feats (this doesn't really do anything as far as I know)
- Ascetic Strike will now use your unarmed strike damage or your character level -4 (instead of your monk level -4)

### Dev
- Kineticist abstraction

## [1.0.11]

### Added
- added Pearl of Powers
- added Runestone of Powers
- added Ascetic Style

### Changed
- Snake Sidewind works with Feral Combat Training
- Boar Style works with Feral Combat Training
- Medusa's Wrath works with Feral Combat Training
- improved some style feat descriptions

### Fixed
- Medusa's Wrath sometimes triggering too many AoO

## [1.0.10]

### Fixed
- fix bug crashing Kineticist.init

## [1.0.9]

### Added
- added option to have Summoner's Life Link trigger at 1 HP, instead below 0

### Removed
- removed Kineticist flight talents, since CotW has them too
- removed Kineticist mastery selection, since CotW also implemented that

### Changed
- reduced the Mind Shield bonus to 1
- CotW's Hex Strike available to Hexcrafter now
- Hex Strike should work with Feral Combat Training now

### Fixed
- fixed Mobile Blast error log
- fixed Impale showing up twice
- fixed Impale triggering way too many 'Attacks of Opportunity'
- fixed accidentally making some hexes touch attacks
- fixed Accursed Strike not being melee touch attack
- fixed Hexcrafter some hexes not scaling
- fixed Hex Strike ignoring cooldown

### Dev
- changed Hex Strike generation (remove any Hex Strike variants from hotbar)

## [1.0.8-alpha]

### Added
- Option to not load patches (gives great control over the mod, but disabling certain patches can cause issues or crashes)

### Fixed
- fixed catastrophic bug that corrupts saves and prevents leveling up
- fixed some GUIDs appearing twice (although it would work just fine)

## [1.0.7-alpha]

### Dev
- NOTE: this version hasn't been tested thoroughly
- new assemblies for game version 2.1.1
- more internal changes

## [1.0.6]

### Added
- Accursed Glare touch variant

### Fixed
- "Accursed Strike: Beast of Ill Omen" is now a standard action instead of a free action
- fixed missing hexcrafter prerequisite to Flight hex
- added missing "Accursed Glare" and "Major Bestow Curse" to the list of hexcrafter spells
- fixed missing Medusa's Wrath to Scaled Fist bonus feat selection
- fixed bug that caused inconsistencies with hexcrafter hexes (like DC or casterlevel)
- fixed Boar Ferocity not working with Hurtful feat

### Dev
- Medusa's Warth was reported to trigger 6 instead of 2 times; reduce amount to 1 (may trigger 3 times then?) until bug is found
- started implementation so mod may be operated without CotW (unfinished)

## [1.0.5]

### Added
- Master of Many Styles (Monk archetype)
- Combat Style Master
- Snake Style chain
- Boar Style chain
- Wolf Style chain
- Ki-Leech Qinggong Power
- One-Touch Ki Power

### Fixed
- fixed Accursed Strike and Hex Strike not working

## [1.0.4]

### Added
- Hexcrafter (Magus archetype)
- Hex Strike Feat
- Wood Soldiers wild talent (earth element for now)

## [1.0.3]

### Added
- 3 Flight Talents (Flame Jet, Wings of Air, Ice Path)
- Shift Earth
- Spark of Life
- Flensing Strike
- fix for Wall Infusion (deals damage now every round)
- fix for Expanded Element (grants extra talents, boni at rank 3)
- fix for Spiked Pit (deals 1d6 every round)
- fix that Shambling Mound cannot act during grappling (optional)
- CotW: added sfx to Aura of Doom (optional)
- CotW: dazing spell no longer counts as mind affecting (optional)
- Cheat: combining some Parametrized Feat (Weapon Focus and the like)

## [1.0.2]

### Added
- added Mind Shield
- added Hurricane Queen

### Fixed
- fixed Mobile Gathering exploits to get more power or ignore movement debuff
- fixed some settings not being applied

## [1.0.1]

### Added
- added Precise Blast and Mobile Gathering
- added disable options

### Fixed
- fixed impale and spray burn cost

## [1.0.0] Initial commit

### Added
- kineticist: cold spray, impale, extra wild talent
- magus: extra arcana
- CotW: slumber hex no cap (optional)
- hexcrafter archetype (not implemented)

## Legende
### Changed for changes in existing functionality
### Deprecated for soon-to-be removed features
### Removed for now removed features
### Fixed for any bug fixes