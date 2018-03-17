# Loom DAppChain SDK - Unity3d Android Plugin

## Overview

This directory contains an Android Studio project that's used to build the Android Library
`io.loomx.unity3d.aar`, this library is a Unity3d plugin that can be used on Android.


## Building

In Android Studio:
1. Open the Gradle panel (`View`->`Tool Windows`->`Gradle`).
2. Expand the `unity3d`->`Tasks`->`unity3dplugin` node, double-click on either `deployDebugPlugin`
   or `deployReleasePlugin`, this will build and copy the plugin to `Assets/Plugins/Android`.

> NOTE: The `deploy` tasks will invoke either the `assembleDebug` and `assembleRelease` tasks, they
  can be found by expanding the `unity3d`->`Tasks`->`build` node. The `assemble` tasks will place
  the compiled `.aar` lib in the `Android/unity3d/build/outputs/aar` directory.
