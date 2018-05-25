#!/bin/sh
#export UNITY_PATH="/Applications/Unity/Unity.app/Contents/MacOS/Unity"
$UNITY_PATH -projectPath "$PWD" -batchmode -quit -exportPackage "Assets"  unity3d-identity.unitypackage
