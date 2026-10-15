@echo off
setlocal
set "PROJECT_DIR=%~dp0"
set "DOTNET_ROOT=%PROJECT_DIR%.tools\dotnet"
set "GODOT_EXE=%PROJECT_DIR%.tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe"
set "GODOT_CONSOLE=%PROJECT_DIR%.tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"

if not exist "%GODOT_EXE%" (
  echo RungProof Next cannot start because the pinned Godot executable is missing.
  echo Expected: %GODOT_EXE%
  echo See docs\DEVELOPMENT_SETUP.md for the portable toolchain layout.
  exit /b 1
)

if not exist "%DOTNET_ROOT%\dotnet.exe" (
  echo RungProof Next cannot start because the pinned .NET executable is missing.
  echo Expected: %DOTNET_ROOT%\dotnet.exe
  echo See docs\DEVELOPMENT_SETUP.md for the portable toolchain layout.
  exit /b 1
)

if not exist "%GODOT_CONSOLE%" (
  echo RungProof Next cannot prepare assets because the Godot console executable is missing.
  echo Expected: %GODOT_CONSOLE%
  exit /b 1
)

set "PATH=%DOTNET_ROOT%;%PATH%"
pushd "%PROJECT_DIR%"
rem Build includes package restore so a fresh checkout needs no cached obj files.
"%DOTNET_ROOT%\dotnet.exe" build RungProof.Next.csproj --nologo
if errorlevel 1 (
  echo RungProof Next build failed. The application was not started.
  popd
  exit /b 1
)
rem Import GLBs, fonts and textures before starting the player. Godot reuses
rem unchanged imports on later launches; Blender authoring files stay disabled.
"%GODOT_CONSOLE%" --headless --path "%PROJECT_DIR%." --import
if errorlevel 1 (
  echo RungProof Next asset import failed. The application was not started.
  popd
  exit /b 1
)
popd
start "RungProof Next" "%GODOT_EXE%" --path "%PROJECT_DIR%."
endlocal
