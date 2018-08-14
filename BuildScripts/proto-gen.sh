#!/bin/bash
set -e

ROOT=../UnityProject/Assets/LoomSDK/Source/Protobuf
pushd ${ROOT}/ > /dev/null

protoFiles=("address_mapper.proto" "evm.proto" "loom.proto" "plasma_cash.proto" "transfer_gateway.proto")
protoFilesWithPath=()
for Path in "${protoFiles[@]}"
do
    protoFilesWithPath+=("proto/${Path}")
done

protoFilesWithPathConcat=$(printf -- "%s " ${protoFilesWithPath[*]})
protoc -I. --csharp_out=internal_access:. ${protoFilesWithPathConcat}
sed -i 's/global::Google.Protobuf/global::Loom.Google.Protobuf/' *.cs

popd > /dev/null