# Loom Network SDK for Unity

This repo contains the SDK code and a **Unity 2017.4** project that provides examples.

The SDK currently supports the following Unity targets:
- Desktop Win/macOS/Linux
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

Here are some notes on the dependencies used and how to update them.

### Chaos.NaCl

Download the latest `Chaos.NaCl` package from [NuGet](https://www.nuget.org/packages/dlech.Chaos.NaCl/), open as a ZIP archive, use the `lib\net40\Chaos.NaCl.dll` assembly.

### Google Protocol Buffers

Download the latest `Google.Protobuf` package from [NuGet](https://www.nuget.org/packages/Google.Protobuf), open as a ZIP archive, use the `lib\net45\Google.Protobuf.dll` assembly.

### Websocket-Sharp

Clone https://github.com/sta/websocket-sharp, build the `Release` configuration, the output assembly is `bin\Release\websocket-sharp.dll`.

### Nethereum

Download the latest `net461dllsAOT.zip` release artifact from https://github.com/Nethereum/Nethereum/releases. Only those assemblies are used by the SDK:
```
Nethereum.ABI.dll
Nethereum.Contracts.dll
Nethereum.Hex.dll
Nethereum.JsonRpc.Client.dll
Nethereum.Model.dll
Nethereum.RLP.dll
Nethereum.RPC.dll
Nethereum.Util.dll
BouncyCastle.Crypto.dll
Common.Logging.Core.dll
Newtonsoft.Json.dll
```

### Dependecy Namespace Prefixing

The SDK contains quite a bit of dependencies, so there is a high chance that a Unity project will contain another version of a dependency for other purposes. Since Unity doesn't have any dependency management for third-party SDKs yet, all dependencies have `Loom.` prefix added to their namespaces. To do this, [AssemblyNamespaceChanger](https://github.com/LostPolygon/AssemblyNamespaceChanger) tool is used. The relevant version is already present n the repo.

1. Acquire/build all dependency assemblies.
2. Copy them into the `BuildScripts\PrefixDependencies\Original` folder.
3. Run the `BuildScripts\PrefixDependencies\prefix-dependencies.cmd` script. It will prefix the assemblies and put them into the `BuildScripts\PrefixDependencies\Processed` folder.
4. Copy the prefixed assemblies from `BuildScripts\PrefixDependencies\Processed` to `UnityProject\Assets\LoomSDK\Plugins`.

[Unity SDK Quickstart]: https://loomx.io/developers/docs/en/unity-sdk.html
[Google Protocol Buffers]: https://developers.google.com/protocol-buffers/docs/csharptutorial
[`protoc` compiler]: https://github.com/google/protobuf/releases