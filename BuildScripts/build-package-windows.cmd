@echo off
setlocal

if "%UNITY_PATH%" == "" (
    set UNITY_PATH=%1
)

set DEFAULT_UNITY_PATH=C:\Program Files\Unity\Editor\Unity.exe
if exist "%DEFAULT_UNITY_PATH%" (
    echo Using default Unity path %DEFAULT_UNITY_PATH%
    set UNITY_PATH=%DEFAULT_UNITY_PATH%
)

if "%UNITY_PATH%" == "" (
    echo Error: Unity path not defined. Please set the UNITY_PATH environment variable to the Unity.exe file, or pass the path as argument
    exit /B 1
)

if not exist "%UNITY_PATH%" (
    echo Error: File %UNITY_PATH% doesn't exist
    exit /B 1
)

echo Using UNITY_PATH = %UNITY_PATH%
echo Launching Unity to build Loom SDK .unitypackage

%UNITY_PATH% -projectPath "%cd%/../UnityProject/" -batchmode -quit -executeMethod Loom.Client.Unity.Editor.Build.BuildPackages.BuildPackage