@echo off
setlocal

echo Deleting all bin and obj folders under "%~dp0" ...

for /d /r "%~dp0" %%d in (bin obj) do (
    if exist "%%d" (
        echo Removing %%d
        rd /s /q "%%d"
    )
)

echo Done.
endlocal
pause
