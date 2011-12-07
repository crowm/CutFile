@echo off

REM Set the working folder to the folder this batch file is in just to be sure
%~d0
cd %~dp0

time /T

if not exist "SetEnvVars.bat" (
	copy "SetEnvVars.bat.template" "SetEnvVars.bat"
	
	call :exec ".\bin\fregex.exe" "r|VS10PATH=.*$|VS10PATH=$r|HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\10.0\InstallDir|" -i "SetEnvVars.bat" -o "SetEnvVars.bat"
	if not %ERRORLEVEL%==0 exit /B %ERRORLEVEL%

	call :exec ".\bin\fregex.exe" "r|TSVNPATH=.*$|TSVNPATH=$r|HKEY_LOCAL_MACHINE\SOFTWARE\TortoiseSVN\Directory|" -i "SetEnvVars.bat" -o "SetEnvVars.bat"
	if not %ERRORLEVEL%==0 exit /B %ERRORLEVEL%
)

call SetEnvVars.bat

if not exist "%TSVNPATH%" (
	echo Error: TSVNPATH does not exist: "%TSVNPATH%"
	echo Please edit your SetEnvVars.bat file to enter the correct location for the TortoiseSVN installation folder
	goto ConfigIsWrong
)

if not exist "%VS10PATH%" (
	echo Error: VS10PATH does not exist: "%VS10PATH%"
	echo Please edit your SetEnvVars.bat file to enter the correct location for your Visual Studio 2008 installation
	goto ConfigIsWrong
)

if exist devenv.log del devenv.log

if not exist .\Temp mkdir .\Temp

:Update-VersionNumbers
echo ######################## Update-VersionNumbers ########################

	REM Use the rev of the repo for the version
	call :exec "%TSVNPATH%\bin\subwcrev.exe" ".." "SetVersion.bat.template" "SetVersion.bat"
	if not %ERRORLEVEL%==0 exit /B %ERRORLEVEL%
	call SetVersion.bat
	echo Version is %VERSION%
	del SetVersion.bat

    REM back up files that hold versions so they can be restored after the build
    REM  (this is just so that these files don't keep showing up in the svn commit dialog)
    call :exec copy /Y "..\Properties\AssemblyInfo.cs" ".\Temp\CutfileAssemblyInfo.cs"
	if not %ERRORLEVEL%==0 exit /B %ERRORLEVEL%

    REM Replace the version numbers
	call :exec ".\bin\fregex.exe" "s/AssemblyVersion.*$/AssemblyVersion(\"%VERSION%\")]/" "s/AssemblyFileVersion.*$/AssemblyFileVersion(\"%VERSION%\")]/" -i "..\Properties\AssemblyInfo.cs" -o "..\Properties\AssemblyInfo.cs"
	if not %ERRORLEVEL%==0 exit /B %ERRORLEVEL%

:End-VersionNumbers

:Build-CutFile
echo ################## Build-CutFile ##################

	call :exec "%VS10PATH%\devenv.exe" "..\CutFile.sln" /rebuild "Release" /out devenv.log
	if not %ERRORLEVEL%==0 exit /B %ERRORLEVEL%

    REM restore AssemblyInfo.cs to original (unaltered version number)
    call :exec copy /Y ".\Temp\CutFileAssemblyInfo.cs" "..\Properties\AssemblyInfo.cs"
	if not %ERRORLEVEL%==0 exit /B %ERRORLEVEL%
    

:CreateBuildOutput

    if not exist "..\builds" mkdir "..\builds"
	if not %ERRORLEVEL%==0 exit /B %ERRORLEVEL%
    
    if not exist "..\builds\CutFile-%VERSION%" mkdir "..\builds\CutFile-%VERSION%"
	if not %ERRORLEVEL%==0 exit /B %ERRORLEVEL%
    
    copy /Y "..\bin\CutFile.exe" "..\builds\CutFile-%VERSION%"
    if exist "..\builds\CutFile-%VERSION%\CutFile.vshost.exe" (
        del "..\builds\CutFile-%VERSION%\CutFile.vshost.exe"
    )
    
    
:end
echo Build Successful
time /T
exit /B 0	

:ConfigIsWrong
	echo -------------------
	echo The settings file will now be opened in notepad.
	echo Please confirm that each of the path variables is pointing to the correct location.
	echo -------------------
	pause
	start notepad "SetEnvVars.bat"
	exit /B 1

:exec
pushd %CD%
if "%~x1" == ".bat" (
	echo call %*
	call %*
) else (
	echo %*
	%*
)
popd

set RETURNCODE=%ERRORLEVEL%
if not %RETURNCODE%==0 echo Exited with code %RETURNCODE%
echo _ %RETURNCODE%

exit /B %RETURNCODE%
