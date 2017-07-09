rem Pass the full path to the output directory as 1st arg to this script. In visual it's the $(TargetDir) macro

@echo on
IF [%1]==[] GOTO noarg

set RIMWORLD_ASSEMBLY_MOD_PATH=I:\rimworld_debug\RimWorld\Mods\PrepareLanding\Assemblies
set MOD_ASSEMBLY_PATH="%1"
del %RIMWORLD_ASSEMBLY_MOD_PATH%\PrepareLanding.dll
del %RIMWORLD_ASSEMBLY_MOD_PATH%\PrepareLanding.dll.mdb
del %RIMWORLD_ASSEMBLY_MOD_PATH%\PrepareLanding.pdb

echo Copying files
xcopy /f %MOD_ASSEMBLY_PATH%\PrepareLanding.dll %RIMWORLD_ASSEMBLY_MOD_PATH%
xcopy /f %MOD_ASSEMBLY_PATH%\PrepareLanding.dll.mdb %RIMWORLD_ASSEMBLY_MOD_PATH%
xcopy /f %MOD_ASSEMBLY_PATH%\PrepareLanding.pdb %RIMWORLD_ASSEMBLY_MOD_PATH%
GOTO done

:noarg
echo "No argument provided. It should be the full path of the output directory."
goto end
:done
echo Done generating mdb file
:end

