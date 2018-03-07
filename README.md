# Loom Network SDK for Unity3d

This repo contains the SDK code and a **Unity 2017.3** project that provides examples.

The SDK currently supports the following Unity targets:
- Desktop Win/MacOS/Linux

## Overview

`LoomAuthClient` should be used to obtain a `LoomIdentity`, once you have a `LoomIdentity` you
can use the associated private key to sign and then commit transactions using `LoomChainClient`.


## Authorization Flows

To obtain a `LoomIdentity` you must first obtain an access token from Auth0, the exact process
depends on the platform. Currently the process has only be implemented on desktop platforms,
mobile platforms are TBD.

### Desktop Windows / Mac / Linux

`LoomAuthClient` will wait for an HTTP request on `http://127.0.0.1:9999/auth/auth0/`, then it will
open a new browser window and load the Auth0 sign-in page (using the default system web browser).
At this point the user should be directed to switch to the browser to sign-in, when they do so
successfully Auth0 will redirect to the aforementioned URL, and `LoomAuthClient` will fetch or
create a `LoomIdentity`.

## Adding new transaction types

Create a new `.proto` file in the `Assets\Protobuf` directory, refer to [Google Protocol Buffers](https://developers.google.com/protocol-buffers/docs/csharptutorial) for syntax details etc.
You'll need to download the [`protoc` compiler](https://github.com/google/protobuf/releases) to
generate C# classes from the `.proto` files.

The relevant command will look something like this:
```cmd
protoc -I=<install path>/protoc-3.5.1/include -I=<project path>\Assets\Protobuf --csharp_out=<project path>/Assets/Protobuf <project path>/Assets/Protobuf/MyTransactions.proto
```

## Samples

Currently there is only once appropriately named scene that renders a couple of buttons that
are hooked up to call the corresponding methods in `Assets/authSample.cs`, these
must be pressed in the correct order. First press the `Sign In` button, this should open a new
browser window, once you've signed up/in you should see the text above the button change to
`Signed in as ...`. Once the textbox indicates you're signed in you can press the `Send Tx`
button to generate, sign, and commit a new dummy transaction to the Loom DAppChain running at
`http://stage-rancher.loomapps.io:46657`, if the transaction is accepted by the DAppChain the
textbox should change to `Committed Tx to Block ...` - indicating the block the transaction was
committed to, then you can press the `Send Tx` button again to create another transaction.
