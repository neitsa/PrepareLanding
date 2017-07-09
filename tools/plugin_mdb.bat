rem This script calls pdb2mdb.exe. The 1st arg to this script must be the full path of the DLL for which to generate the mdb file.
rem Side note: pdb2mdb doesn't handle relative paths...
@echo on
rem echo "%1"
IF [%1]==[] GOTO noarg

echo Generating mdb file
%~dp0\pdb2mdb.exe "%1"
GOTO done

:noarg
echo "No argument provided. It should be the directory containing the mod DLL"
goto end
:done
echo Done generating mdb file
:end