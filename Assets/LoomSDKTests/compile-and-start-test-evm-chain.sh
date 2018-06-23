set -e

pushd ./TestsEvmContract/
./compile.sh
popd

pushd ./TestsEvmContract/TestsChain/
./start-chain.sh
popd