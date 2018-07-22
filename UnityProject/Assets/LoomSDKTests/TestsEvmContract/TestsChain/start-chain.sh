set -e

if [ ! -d ./build ]; then
    mkdir build
fi

cd build

if [ ! -f ./loom ]; then
    ../download-loom.sh 276
fi

rm -rf ./app.db
rm -rf ./chaindata

cp ../genesis.example.json genesis.json
set +e
./loom init
set -e
./loom run