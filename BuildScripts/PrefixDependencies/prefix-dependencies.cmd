mkdir Processed

set currentFile=Newtonsoft.Json.dll
goto :addPrefix
:Newtonsoft.Json.dll

set currentFile=BouncyCastle.Crypto.dll
goto :addPrefix
:BouncyCastle.Crypto.dll

set currentFile=Google.Protobuf.dll
goto :addPrefix
:Google.Protobuf.dll

set currentFile=websocket-sharp.dll
goto :addPrefix
:websocket-sharp.dll

set currentFile=Chaos.NaCl.dll
goto :addPrefix
:Chaos.NaCl.dll

set currentFile=Common.Logging.Core.dll
goto :addPrefix
:Common.Logging.Core.dll

set currentFile=Nethereum.Contracts.dll
goto :addPrefix
:Nethereum.Contracts.dll

set currentFile=Nethereum.RPC.dll
goto :addPrefix
:Nethereum.RPC.dll

set currentFile=Nethereum.Model.dll
goto :addPrefix
:Nethereum.Model.dll

set currentFile=Nethereum.JsonRpc.Client.dll
goto :addPrefix
:Nethereum.JsonRpc.Client.dll

set currentFile=Nethereum.ABI.dll
goto :addPrefix
:Nethereum.ABI.dll

set currentFile=Nethereum.RLP.dll
goto :addPrefix
:Nethereum.RLP.dll

set currentFile=Nethereum.Util.dll
goto :addPrefix
:Nethereum.Util.dll

set currentFile=Nethereum.Hex.dll
goto :addPrefix
:Nethereum.Hex.dll

goto :eof

:addPrefix
LostPolygon.AssemblyNamespaceChanger.exe --replace-references --replace-assembly-name -i Original\%currentFile% -o Processed\Loom.%currentFile% -r ^
^^Newtonsoft\.:Loom.Newtonsoft.:^
^^Common\.Logging:Loom.Common.Logging:^
^^BouncyCastle\.Crypto:Loom.BouncyCastle.Crypto:^
^^Org.BouncyCastle:Loom.Org.BouncyCastle:^
^^Google\.Protobuf:Loom.Google.Protobuf:^
^^WebSocketSharp:Loom.WebSocketSharp:^
^^websocket-sharp:Loom.websocket-sharp:^
^^Chaos\.NaCl:Loom.Chaos.NaCl:^
^^Nethereum\.:Loom.Nethereum.

goto :%currentFile%