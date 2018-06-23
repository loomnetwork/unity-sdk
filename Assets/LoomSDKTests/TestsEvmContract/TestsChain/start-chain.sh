set -e

if [ ! -f ./loom ]; then
    wget https://storage.googleapis.com/private.delegatecall.com/loom/linux/build-196/loom
    chmod +x loom
fi

rm -rf ./app.db
rm -rf ./chaindata

set +e
./loom init
set -e
./loom run