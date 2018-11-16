# Changelog

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