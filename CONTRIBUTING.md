# Contributing to PrepareLanding

Thank you for helping make PrepareLanding better! We'd like this to be a community effort. However, we do have a few rules we need you to follow in order to keep things running smoothly, whether you're reporting a bug or submitting code.

## Bug Reports

Before issuing a bug report, please follow the following steps:

* Check if your issue was already reported in the [existing issues](https://github.com/neitsa/PrepareLanding/issues?utf8=%E2%9C%93&q=is%3Aissue).

* If you can find one that is related to your problem, please comment there instead of making a new one.

* Make sure the issue title is descriptive, for example "I do that in the " instead of "it doesn't work." 

* Post your issue using this template: 

```
O.S: <system type> <version> <bitness (32 / 64 bits)> 

RimWorld: <version>

Problem: 
    <describe precisely your problem>

Reproduction steps: 
    <how can we reproduce your problem?; describe exactly the steps involved.>

Logs:
    <link>Output_log.txt

Installed Mods:
    <list> or <link to list> 

Persistent save:
    <link to persistent save>

Screenshot:
    Note: optional / if meaningful only.
    <when your reproduce your problem, take a screenshot in RimWorld (F10 key)>
```
    
* In `Reproduction steps` list the exact steps or conditions to reproduce the bug.
    * Instructions of the form "1. Load stock RimWorld. 2. Go there 3. Do this" are best. The easier we can make the bug happen on our end, the easier it is to fix it.
    
* In `Logs` provide your RimWorld log file:
    * **Windows**:
        - Windows (Steam): C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin_Data\output_log.txt
        - Windows (non-Steam) has TWO possible locations:
            - RimWorld####Win\RimWorld####Win_Data\output_log.txt
            - %userprofile%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\output_log.txt
    * **Mac OS**: 
        - Mac (Steam): Users/<UserName>/Library/Logs/Unity/Player.log
        - Mac (non-Steam): /Users/[your user name]/Library/Logs/Unity/Player.log
    * **Linux**:
        - Linux (Steam): /home/<UserName>/.config/unity3d/Ludeon Studios/RimWorld/Player.log
            - Note: You must run RimWorld from the console or the log file will not be created.  If you don't run RimWorld from the console, you may find the log at /tmp/rimworld_log
        - Linux (non-Steam): /home/[your user name]/.config/unity3d/Ludeon Studios/RimWorld/Player.log   
        
* As PrepareLanding is using HugsLib, you can also publish a log using `CTRL+F12` to publish the log from within the game.
    
* In `Installed Mods`, list all installed mods (preferably WITH version number)
    * Put the list either in the issue body, on [gist](https://gist.github.com/) (easier to edit later), [pastebin](http://pastebin.com/), or any similar site ([Fedora pastebin](http://fpaste.org),  [pastie](http://pastie.org/)).
    
* In `Persistent Save` provide a link to your current persistent save file. It can be very helpful to include a savegame, especially when you have trouble reproducing the bug. Find it by clicking the 'open save games folder' button in the in-game Options menu.  If the .rws file is too big (it likely is), you can zip the with with a compression program such as 7-Zip or you can upload it to Google Drive / One Drive / Dropbox / etc and post the link in your bug report.

* In `Screenshot` (optional) If the bug is easy to see in an image, please attach a screen shot (use F10 key when in RimWorld).

### Trimming down the problem

Despite how complicated it may seem, it is actually quite simple to do (even for Steam users), as you can simply copy your install directory elsewhere as needed and remove non-problematic mods. It's especially useful since interaction bugs are some of the hardest to nail down; something that appears to be a bug in one mod might actually be caused by something else.

If the issue stems from only `PrepareLanding`, try to replicate this issue with only `PrepareLanding` in a clean RimWorld install. 

If the issue stems from the use of several different mods (e.g. `PrepareLanding` and other mods) then get a clean install of RimWorld and install only these mods (and their dependencies, if applicable).

This may seem like an extreme measure, but it ensures that only `PrepareLanding` and these mods (or maybe the stock game) are to blame.

## Pull Requests

When writing new code for PrepareLanding, please follow the following steps:

* Read the titles of open issues and pull requests, to see if somebody's already thought of your idea. If the issue has somebody assigned (see the rightmost column of the page), it's already being taken care of. Otherwise, assign yourself and/or add a comment saying you'll do it.

* Fork PrepareLanding and do any code updates. Like most Rimworld plugins, PrepareLanding's code is organized around a Visual Studio solution (`.sln`) file, which can be opened with any of several C# development tools ([Visual Studio](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx); [MonoDevelop (aka Xamarin Studio)](http://www.monodevelop.com/download/); [SharpDevelop](http://www.icsharpcode.net/OpenSource/SD/Download/)). Third-party developers can ignore the most of the python scripts in the repository; they are needed only to support automatic builds from the online repository.

* What happens next depends on what the new code does:
    - If your code fixes a bug in PrepareLanding, create a pull request to the `develop` branch (this should be the default).
    - If your code adds a new feature in PrepareLanding, create a pull request to the branch of the next release (e.g., `1.7.0`; if no such branch exists, please contact us).
    
* One of our members or the maintainer will merge the pull request; if you yourself are a member, please wait one week to give others a chance to give feedback.