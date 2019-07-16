# Changelog

## June 17, 2019

- Updated `Nethereum `to `3.3.0`.
- Updated `Protobuf` to `3.9.0`.
- Added full support for subscribing to specific event topics for Go contracts. Previously, this had to be done through the `IRpcClient` directly, which was error-prone and ignored the order of calls. Now, subscribing/unsubscribing are separate operations. See `DAppChainClient.SubscribeToEvents/UnsubscribeFromAllEvents/SubscribeToEvents/UnsubscribeFromEvents`.
- Added `RawChainEventContract`, which is similar to `RawChainEventContract`, but isn't expecting the event data to be in JSON-RPC format.
- Fixed fetching EVM logs with a filter applied not working in some cases.
- Fixed link.xml to work on AOT platform with bytecode stripping enabled.

## May 8, 2019

- Prioritize write client for getting nonce.
- Implemented speculative nonce.
- Fixed concurrency issues in `CryptoUtils`.
- Increment retry delay on invalid nonce exponentially instead.
- Fixed tests to compile with Solidity 0.5.0 compiler.
- Fixed building in Unity 2019.1.

## November 16, 2018

- Use `DAppChainClientConfiguration` class for `DAppChainClient` options instead of having multiple properties inside of `DAppChainClient`. `timeout` optional parameter is now gone from `Contract.Call*` methods, modify `CallTimeout` and `StaticCallTimeout` in `DAppChainClient.Configuration` now.
- Modified `Loom.Nethereum.Minimal.Packed.dll` to not use PublicKey when referencing `Loom.Newtonsoft.Json` (was causing assembly reference mismatch in Unity in some cases).
- Fixed an issue in `WebSocketRpcClient` that caused timeouts when multiple calls were going on at the same time.
- Added `Address.FromBytes` convenience method.
- Added `EvmContract.GetEvent` and `EvmEvent`, allows getting past event with filters (covers https://github.com/loomnetwork/unity-sdk/issues/50 and https://github.com/loomnetwork/unity-sdk/issues/12).
- Added `EvmContract.GetBlockHeight`.
- Added `IDAppChainClientCallExecutor`, a layer that controls the blockchain calls flow. All calls made from inside `DAppChainClient` must be wrapped in `IDAppChainClientCallExecutor` methods calls. Calling `IRpcClient.SendAsync` directly on an instance of `IRpcClient` should now be considered unsafe.
- Implemented `DefaultDAppChainClientCallExecutor`:
    1. Calls throw a `TimeoutException` if the calls receives no response for too long.
    2. Calls are queued, there can be only one active call at any given moment.
    3. If the blockchain reports an invalid nonce, the call will be retried a number of times.