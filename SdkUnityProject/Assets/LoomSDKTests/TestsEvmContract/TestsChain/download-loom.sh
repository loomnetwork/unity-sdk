set -e

LOOM_BUILD=latest

if ! [ -z "$1" ] && ! [ "$1" == "latest" ]; then
    LOOM_BUILD=build-$1
fi

case "$(uname -s)" in
    Darwin)
        echo "Downloading Loom ${LOOM_BUILD} binary for macOS"
        LOOM_OS_NAME="osx"
        ;;
    *)
        echo "Downloading Loom ${LOOM_BUILD} binary for Linux"
        LOOM_OS_NAME="linux"
        ;;
esac

if [ -x "$(command -v curl)" ]; then
    curl -O https://storage.googleapis.com/private.delegatecall.com/loom/${LOOM_OS_NAME}/${LOOM_BUILD}/loom
elif [ -x "$(command -v wget)" ]; then
    wget https://storage.googleapis.com/private.delegatecall.com/loom/${LOOM_OS_NAME}/${LOOM_BUILD}/loom -O loom
else
    echo "Error: wget or cURL must be installed"
    exit 1
fi

chmod +x loom