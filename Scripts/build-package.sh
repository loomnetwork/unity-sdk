set -e

if [ -z "${UNITY_PATH}" ]; then
    UNITY_PATH=$1
fi

DEFAULT_UNITY_PATH=/Applications/Unity/Unity.app/Contents/MacOS/Unity
if [ -d "${DEFAULT_UNITY_PATH}" ]; then
    echo "Using default Unity path ${DEFAULT_UNITY_PATH}"
    UNITY_PATH=${DEFAULT_UNITY_PATH}
fi

if [ -z "${UNITY_PATH}" ]; then
    echo "Error: Unity path not defined. Please set the UNITY_PATH environment variable to the Unity installation directory, or pass the pass as an argument"
    exit 1
fi

if [ ! -d "${UNITY_PATH}" ]; then
    echo "Error: File ${UNITY_PATH} doesn't exist"
    exit /B 1
fi

echo "Using UNITY_PATH = ${UNITY_PATH}"
echo "Launching Unity to build Loom SDK .unitypackage"

$UNITY_PATH -projectPath "${PWD}/../UnityProject/" -batchmode -quit -executeMethod Loom.Client.Unity.Editor.Build.BuildPackages.BuildPackage