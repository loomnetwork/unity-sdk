# Loom DAppChain SDK - Unity3d Android Plugin

## Overview

This directory contains an Android Studio project that's used to build the Android Library
`io.loomx.unity3d.aar`, this library is a Unity3d plugin that can be used on Android.


## Building

In Android Studio:
1. Open the Gradle panel (`View`->`Tool Windows`->`Gradle`).
2. Expand the `unity3d`->`Tasks`->`build` node, double-click on either `assembleDebug` or
   `assembleRelease` to build the plugin. Provided everything compiled correctly you'll find the
   build output in the `Android/unity3d/build/outputs/aar` directory.
3. Expand the `unity3d`->`Tasks`->`unity3dplugin` node, double-click on either `deployDebugPlugin`
   or `deployReleasePlugin`, this will copy the plugin built in the previous step to
   `Assets/Plugins/Android`.
