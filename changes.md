***

# Version 0.7.0
**Released December 22, 2017**
**`RimWorld Version: Beta 18`**

# General

What's New?
===========

This version has more changes than I thought I would implement...

## TL;DR

* New filters:
    - Caves
    - Named locations
    - Coastal rotations

* Reworked Road / Rivers filters to follow Boolean logic.

* Stones can be filtered in order or not.

* Add a new "coordinates" window.

## Major Changes

* Add a filter for tiles with caves [issue #20]
    - It's in the "Terrain 2 & Temp". tab.

* Add a filter for filtering tiles that are in a specific named location (aka World Features) [issue #21]
    - It's in the "Terrain 2 & Temp". tab.

* Add a filter for coastal rotation [issue #25]
    - It is now possible to filter coastal tiles that have their coast facing a certain direction
    - e.g. Filter all tiles that have a northern coast, etc.
    - It's in the Terrain Tab, below the already existing coastal filters.

* Completely reworked the filtering for 3 states items (ON / OFF / Partial)
    - Roads and Rivers: complete Boolean filtering (AND & OR)
        - new button on the GUI to choose between the two
    - Stones
        - Finally, added a "no order" filtering, hooray!
    - These new filters can now be saved and loaded in presets.

* Add a new "Coordinates Window" (still rough implementation)
    - Allows to go to a tile by its ID or coordinates.
    - Add buttons with the main point of interests (North Pole, South Pole, etc.)

## Minor Changes

* Each filter header is now colored (default to magenta)

* Remove all the code that was implemented to bypass a bug in Rimworld vanilla (bug was fixed in B18) [issue #22]
    - [bug details](https://ludeon.com/forums/index.php?topic=34054.msg347150#msg347150)

* Info Tab
    - Add the "Average Disease Frequency" information for each biome in the Info Tab [issue #19]
    - Rework the world information display on the Info Tab
        - new "World records" with the lowest / highest characteristics (temperature, rainfall, elevation)

* Add a way to list and delete presets [issue #23]
    - The delete button is in the "Load" tab, visible when you load a preset.
    - Pick a preset in the left list and press delete twice to remove it.

* Fix a bug where "Animal Can Graze" filter could not be saved or loaded properly [issue #24]

* (Privacy) Redact the name of the current user from the log file [issue #26]
    - The preset folder location is written in the log as a debug purpose.
    - This folder is, most of the time,  in a sub-dir of the user directory: thus user name would be written to the log...
    - e.g. `C:\Users\neitsa\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\PrepareLanding`
    - Is now logged as: `C:\Users\<redacted>\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\PrepareLanding`

* Remove a warning from RimWorld: "mesh already finalized." [issue #27]
    - RimWorld thinks that the highlighted tiles mesh (used to display highlighted tile on the world map) is already finalized.
    - This makes RimWorld display a warning in the log.
    - This warning was not present until B18: fixed by telling RimWorld that the mesh was not yet finalized when created.

* Rename "Temperature" Tab to "Terrain II & Temp."

* Clear the filter logger when a new world is generated
    - Add a new message, "New World Generated"

* Updated filtered tile description (in "Filtered Tiles" tab) to be in synch. with B18.

## Internal Changes

* Upgrade automatic builder to VS 2017 \o/
* Add a new internal container for three state items
    - Easier to manage from a code POV.
* Reworked completely the Harmony Patch for displaying the main PrepareLanding button.
    - 2 times less code!
    - It now allows other mods to patch the same function.
* Major Code cleanup

***


# Version 0.6.0
**Released November 18, 2017**
**`RimWorld Version: Beta 18`**

# General

What's New?
===========

* Fixed code for Beta 18 support.
* Added a new filter:
    - "Coastal Tile (Lake)" in terrain tab (see. "Coastal Tiles")
        - A Lake is at most 18 tiles surrounded by land. More than that is considered a sea / ocean.
    - The previous behavior is now "Coastal Tile (sea)".

Detailed Changelog
==================

Fixed Issues
------------

* None

Future Plans
============

TODO for next release (v0.6.1 or v0.7)

* Better Beta 18 support:
    - Add filter: average disease frequency
    - Add filter for tiles with special features (e.g. Caves)
    - Add possibility to filter only tiles in a named location on the world map


***


# Version 0.5.1
**Released September 16, 2017**
**`RimWorld Version: Alpha 17b`**

**HotFix Release for v0.5**

# General

What's New?
===========

* Fixed a nasty bug when loading a save and clicking on the "world" button.
* Fixed a rare bug condition related to loading a new world.

Detailed Changelog
==================

Fixed Issues
------------

* bug when loading a save and going directly to the world
    - The problem was related to events that didn't tell the mod that the world was generated.
    - Fixed by changing the internal event that handle properly the world generation.

* Rare bug condition when loading a new world:
    1. Generate a new world and filter some tiles
    2. Go back to the main menu and load a save
    3. From the save, go to the world and filter some tiles
    4. Exit from the save to the main menu
    5. Generate a new world
    6. PrepareLanding didn't see that the world has changed and kept references to tiles from the previous world
    * --> Reworked completely the way rimworld events (e.g. World loaded or World Generated) are handled in the mod.


***


# Version 0.5.0
**Released September 02, 2017**
**`RimWorld Version: Alpha 17b`**

# General

What's New?
===========

The [online manual](https://neitsa.github.io/games/rimworld/preparelanding/) has been rewritten to account for the latest changes.

* [**Important**]  Most / Least Feature filter ([see manual](https://neitsa.github.io/games/rimworld/preparelanding/temperature_tab.html#most-least-feature))
    - Allows to filter on the following highest / lowest world characteristics:
        - Elevation
        - Rainfall
        - Temperature
* [**Important**] God Mode ([see manual](https://neitsa.github.io/games/rimworld/preparelanding/god_mode_tab.html))
    - Allows player to change tiles' characteristics on the world map
        - Biome
        - Average Temperature
        - Terrain (Hilliness)
        - Elevation
        - Rainfall
        - Stone Types
* [**Important**] Temperature Forecast ([see manual](https://neitsa.github.io/games/rimworld/preparelanding/temperature_tab.html#temperature-forecast))
* Less clicking for River and Road filters, they now have 3 buttons (Reset, All, None)
* Fixed a bug when there were more rivers than in Vanilla
    - Also fixed for Roads Filter
* Internal
    - Huge code refactoring
    - TileHighlighter now uses RimWorld's WorldLayer class
        - It takes a little bit more time to draw the highlighted tiles but,
        - It is less laggy and less demanding on smaller computers
    - Code and data for drawing map overlays is implemented (but not activated yet)

Detailed Changelog
==================

Fixed Issues
------------

* Add filtering on most / least (max / min) tile features [issue #10: requested by Barky, Zapleek]
* Rework the tile Highlighter [issue #11]
* Add must/must not have road/river" regardless of type. [issue #15: request by WorkingClassHero]
* Long list of rivers breaks the GUI layout [issue #17: request by elwooha6]

Future Plans
============

* Check if possible to actually set the tile properties according to user choices [issue #8]
    - 6 feature planned on 8 have been implemented (see God Mode)
* Add world map overlays for various features (temperature, rainfall, etc.) [issue #12]
    - Code & data are there but it is too slow at the moment to be released.


***


# General

# Version 0.4.3
**Released August 03, 2017**
**`RimWorld Version: Alpha 17b`**

What's New?
===========

* Still building upon [v0.4 release](https://github.com/neitsa/PrepareLanding/releases/tag/v0.4).
* Fix a GUI layout problem when more than 5 stone types are present (issue #6).
* Fix a problem when loading a preset where some states where not correctly set (issue #7).
* The filters are not reset to their default state when going to a new world (they keep their previous state). (issue #9)

Detailed Changelog
==================

Fixed Issues
------------

* Having more stone types than the vanilla game breaks the GUI layout. [issue #6: request by Oblitus]
    - tested & fixed using Cupro's Stones mod.
* Animals can graze now filter problem [issue #7: request by QuakeIV, Ozymandias]
    - the code used the default state of the MultiCheckState enum, which is ON by default...
* Do not reset filters when returning from map to map generation settings [issue #9: request by Oblitus]

Future Plans
============

* Check if possible to actually set the tile properties according to user choices [issue #8]
* Add filtering on most / least (max / min) tile features [issue #10]
* Rework the tile highlighter [issue #11]
* Add world map overlays for various features (temperature, rainfall, etc.) [issue #12]


***


# General

# Version 0.4.2
**Released August 01, 2017**
**`RimWorld Version: Alpha 17b`**

What's New?
===========

* Still building upon [v0.4 release](https://github.com/neitsa/PrepareLanding/releases/tag/v0.4).
* Fix a NRE while in play state and the main window is shown.
* Use a xpath patch rather than a new Def to show the main window when clicking the "World" button while playing.
* Catch a possible (rare) error condition while copying template presets to user folder.

Detailed Changelog
==================

Fixed Issues
------------

* Fix a NRE while in play state and the main window is shown: the number of buttons to show was miscalculated.
* Catch a possible (rare) error condition while copying template presets to user folder: If the mod was removed, some preset files were changed and finally the mod re-installed, this error could arise, rendering the mod useless as this error was triggered


***


# General

# Version 0.4.0
**Released July 30, 2017**
**`RimWorld Version: Alpha 17b`**

What's New?
===========

* **Main feature** of this version: Ability to load & save filters and options.
* Added a new filter: filter on two or three type of stones (whatever they are).
* Added an option to disable tile highlighting altogether.
* Add a sound when clicking on the "Coastal Tile" filter.
* Fix the filter logging text box which doesn't grow.
* Fix misleading error messages.
* Fix the "reset filters" which didn't reset properly all filters.

Detailed Changelog
==================

Fixed Issues
------------

* Add "only two stone types" filter [issue #1:  request by Oblitus]
* World info tab: filter logging text box doesn't grow [issue #2: request by Sixdd]
* Filtering error message is misguiding [issue #3: request by Sebastian Cigar]
* Add filter presets (load / save) [issue #4: request by aza9999, neitsa]


***


# General

# Version 0.3.1
**Released July 22, 2017**
**`RimWorld Version: Alpha 17b`**

What's New?
===========

* Fixed a problem with the "Allow Selection of Impassable Tiles" option in the options tab.

Detailed Changelog
==================

Fixed Issues
------------

* Fixed a problem with the "Allow Selection of Impassable Tiles" option in the options tab.

Internal
---------

* Made the options class inherits from INotifyPropertyChanged.


***


# General

# Version 0.3
**Released July 21, 2017**
**`RimWorld Version: Alpha 17b`**

What's New?
===========

* First release will all base ideas implemented!
* Still no manual (in the making)

Detailed Changelog
==================

Fixed Issues
------------

* First public release, so no fixed issues (but they were a lot of them before :p)


***


# General

# Version 0.2
**[Private build] Released July 2017**
**`RimWorld Version: Alpha 17b`**

What's New?
===========

* First release will all base ideas implemented!
* GUI implemented with all filters

Detailed Changelog
==================

Fixed Issues
------------

* Still in the making, so a lot of bugs were fixed & the code was rapidly moving...


***


# General

# Version 0.1
**[Private build] May 2017**
**`RimWorld Version: Alpha 17`**

What's New?
===========

* Most of the time was dedicated to reading the core game code.
* Base ideas with some filters; no gui
* Filtered tiles are output on the log file...

Detailed Changelog
==================

Fixed Issues
------------

* Still in the making, the code was rapidly moving...