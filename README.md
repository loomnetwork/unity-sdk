# Loom Network SDK for Unity

This repo contains the SDK code and a **Unity 2017.4** project that provides examples.

The SDK currently supports the following Unity targets:
- Desktop Win/MacOS/Linux
- Android
- iOS
- WebGL

## Requirements

- Unity 2017.4 or later.
- `Build Settings` -> `Player Settings` -> `Configuration` set as follows:
  - `Scripting Runtime Version`: `Experimental (.NET 4.6 Equivalent)`
  - `API Compatibility Level`: `.NET 4.6`

## Overview

`LoomChainClient` provides the means to communicate with a Loom DAppChain from a Unity game, while
the `Contract` class provides a more convenient abstraction that streamlines invoking methods on
a specific smart contract running on a DAppChain.

If you haven't done so already now would be a good time to read through the [Unity SDK Quickstart][].

## Adding new transaction types

Create a new `.proto` file in the `Assets\Protobuf` directory, refer to [Google Protocol Buffers][]
for syntax details etc. You'll need to download the [`protoc` compiler][] to generate C# classes
from the `.proto` files.

The relevant command will look something like this:
```shell
protoc \
  -I<install path>/protoc-3.5.1/include \
  -I<project path>/Assets/Protobuf \
  --csharp_out=<project path>/Assets/Protobuf \
  <project path>/Assets/Protobuf/sample.proto
```

## Samples

The sample `authSample` scene expects a local Loom DAppChain node to be running on `localhost`, if
you decide to change the default host/ports the node runs on you'll need to update the host/ports in
`authSample.cs` to match.

When you run the sample scene you will see three buttons that are hooked up to call the
corresponding methods in `Assets/authSample.cs`, these must be pressed in the correct order:
1. Press the `Sign In` button to generate a new random identity, once you've signed up/in you should
   see the text above the button change to `Signed in as ...`.
2. Once the textbox indicates you're signed in you can press the `Call SetMsg` button to call the
   `SetMsg` method in the `BluePrint` smart contract, this method will store a key/value in the
   smart contract state. If the method executes without error the textbox should change to
   `Smart contract method finished executing.`. You can press the `Call SetMsg` button again to make
   another call. Each call to the smart contract actually requires a new transaction to be
   generated, signed, and then validated by the DAppChain.
3. Press the `Call SetMsgEcho` button to call the `SetMsgEcho` method in the `BluePrint` smart
   contract, similarly to `SetMsg` this method will store a key/value in the smart contract, and
   return the key/value it stored.
4. You can press the `Call GetMsg` button to send a simple query to the `BluePrint` contract.

## Building the SDK

`BuildScripts` folder contains scripts for building loom-unity-sdk.unitypackage for Windows and macOS. Scripts use the `UNITY_PATH` environment variable to determine the Unity executable path, if it is set; alternatively, the path can be passed as an argument. As a fallback, standard Unity installation directory might be used.
The package is built to `Assets\~NonVersioned\loom-unity-sdk.unitypackage`.

## Dependencies

For ease of use all necessary prebuilt dependencies are located in the `Assets/Plugins` directory in
this repo, the assemblies are built to target `Any CPU`. The rest of this section contains some
useful notes in case you need to rebuild the dependencies.

### Chaos.NaCl

Build from source https://github.com/CodesInChaos/Chaos.NaCl - then copy the build assemblies from
`Chaos.NaCl/Chaos.NaCl/bin/Release` to `Assets/Plugins`.

### Google Protocol Buffers

Install prebuilt [Google.Protobuf 3.5 from NuGet] and copy them to `Assets/Plugins`.

### Websocket-Sharp

Clone https://github.com/sta/websocket-sharp and build the `Release` config for `AnyCPU`.
The version currently was built from Git rev `000c0a76b4fb2045cabc4f0ae6a80bea03e2663e`.

### Loom.Nethereum.Minimal

Clone https://github.com/loomnetwork/Nethereum/tree/loom-minimal, build solution `Loom.Nethereum.Minimal` with `Release` configuration for `AnyCPU`.

[Unity SDK Quickstart]: https://loomx.io/developers/docs/en/unity-sdk.html
[Google Protocol Buffers]: https://developers.google.com/protocol-buffers/docs/csharptutorial
[`protoc` compiler]: https://github.com/google/protobuf/releases
[Google.Protobuf 3.5 from NuGet]: https://www.nuget.org/packages/Google.Protobuf