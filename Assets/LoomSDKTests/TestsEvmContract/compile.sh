set -e

solc --bin --abi --optimize --overwrite -o Compiled Tests.sol

if [ ! -d  Compiled/Resources ]; then
    mkdir Compiled/Resources
fi

mv Compiled/Tests.abi Compiled/Resources/Tests.abi.json