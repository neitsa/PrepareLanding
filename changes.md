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