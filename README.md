# Loom Network SDK for Unity3d

This repo contains the SDK code and a **Unity 2017.3** project that provides examples.

The SDK currently supports the following Unity targets:
- Desktop Win/MacOS/Linux
- Android

## Requirements

- Unity 2017.3 or later.
- `Build Settings` -> `Player Settings` -> `Configuration` set as follows:
  - `Scripting Runtime Version`: `Experimental (.NET 4.6 Equivalent)`
  - `API Compatibility Level`: `.NET 4.6`

## Overview

`AuthClient` should be used to obtain a `Identity`, once you have a `Identity` you
can use the associated private key to sign and then commit transactions using `LoomChainClient`.

## Authorization Flows

To obtain a `Identity` you must first obtain an access token from Auth0, the exact process
depends on the platform. Currently the process has only be implemented on desktop platforms,
mobile platforms are TBD.

### Desktop Windows / Mac / Linux

`AuthClient` will wait for an HTTP request on `http://127.0.0.1:9999/auth/auth0/`, then it will
open a new browser window and load the Auth0 sign-in page (using the default system web browser).
At this point the user should be directed to switch to the browser to sign-in, when they do so
successfully Auth0 will redirect to the aforementioned URL, and `AuthClient` will fetch or
create a `Identity`.

### Android

`AuthClient` will open the default system browser to the Auth0 hosted login page, once the user
signs in the browser will redirect the user back to the Unity app. `AuthClient` will then fetch
or create a `Identity`.

You must add an Auth0 redirect activity to the Android manifest, and set the `host`, `pathPrefix`,
and `scheme` to match the Auth0 redirect URL specified when creating a new instance of `AuthClient`.
For example:

```xml
<activity android:name="com.auth0.android.provider.RedirectActivity" tools:node="replace">
  <intent-filter>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data
        android:host="loomx.auth0.com"
        android:pathPrefix="/android/io.loomx.unity_sample/callback"
        android:scheme="io.loomx.unity3d" />
  </intent-filter>
</activity>
```

If you don't have a custom manifest you can use `Assets/Plugins/Android/AndroidManifest.xml` in
this repo as a starting point, at the very least you will need to update the `package`, and the
`data` parameters.

## Adding new transaction types

Create a new `.proto` file in the `Assets\Protobuf` directory, refer to [Google Protocol Buffers](https://developers.google.com/protocol-buffers/docs/csharptutorial) for syntax details etc.
You'll need to download the [`protoc` compiler](https://github.com/google/protobuf/releases) to
generate C# classes from the `.proto` files.

The relevant command will look something like this:
```cmd
protoc -I<install path>/protoc-3.5.1/include -I<project path>/Assets/Protobuf --csharp_out=<project path>/Assets/Protobuf <project path>/Assets/Protobuf/MyTransactions.proto
```

## Samples

The sample `authSample` scene expects a local Loom DAppChain node to be running on `localhost`, if
you decide to change the default host/ports the node runs on you'll need to update the host/ports in
`authSample.cs` to match.

When you run the sample scene you will see three buttons that are hooked up to call the
corresponding methods in `Assets/authSample.cs`, these must be pressed in the correct order:
1. Press the `Sign In` button, this should open a new browser window, once you've signed up/in
   you should see the text above the button change to `Signed in as ...`.
2. Once the textbox indicates you're signed in you can press the `Send Tx` button to generate, sign,
   and commit a new dummy transaction to the Loom DAppChain. If the transaction is accepted by the
   DAppChain the textbox should change to `Committed Tx to Block ...` - indicating the block the
   transaction was committed to a block in the DAppChain. You can press the `Send Tx` button again
   to create another transaction.
3. You can press the `Query` button after signing in to send a simple query to the sample contract
   running on the Loom DAppChain. 

## Dependencies

For ease of use all necessary prebuilt dependencies are located in the `Assets/Plugins` directory in
this repo, the assemblies are built to target `Any CPU`. The rest of this section contains some
useful notes in case you need to rebuild the dependencies.

### Auth0.Authentication and Auth0.Core

The Auth0 assemblies only target .NET Standard 1.1/2, and at the time of writing Unity 2017.3 only
targets .NET Framework 4.6, so attempting to install Auth0 via NuGet in the SDK project fails.
Building the assemblies from source and copying them into the SDK project also fails
(with `error CS0012: The type 'System.Object' is defined in an assembly that is not referenced`).
To get around these issues the Auth0 assemblies must be rebuilt from the `net4.6-target`
branch in this repo https://github.com/loomnetwork/auth0.net - then copy the built assemblies
from `src/Auth0.AuthenticationApi/bin/Release/net46` to the Unity project.

### Chaos.NaCl

Build from source https://github.com/CodesInChaos/Chaos.NaCl - then copy the build assemblies from
`Chaos.NaCl/Chaos.NaCl/bin/Release` to `Assets/Plugins`.

### Google Protocol Buffers

Install prebuilt [Google.Protobuf 3.5](https://www.nuget.org/packages/Google.Protobuf) from NuGet
and copy them to `Assets/Plugins`.
