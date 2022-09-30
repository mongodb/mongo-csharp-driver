#!/usr/bin/env bash
# Define varibles
SCRIPT_DIR=$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )
source $SCRIPT_DIR/build.config
TOOLS_DIR=$SCRIPT_DIR/tools
CAKE_EXE=$TOOLS_DIR/dotnet-cake
CAKE_PATH=$TOOLS_DIR/.store/cake.tool/$CAKE_VERSION

if [ "$CAKE_VERSION" = "" ] || [ "$DOTNET_VERSION" = "" ]; then
    echo "An error occured while parsing Cake / .NET Core SDK version."
    exit 1
fi

# Make sure the tools folder exist.
if [ ! -d "$TOOLS_DIR" ]; then
  mkdir "$TOOLS_DIR"
fi

###########################################################################
# INSTALL .NET CORE CLI
###########################################################################

export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
export DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=2

DOTNET_INSTALLED_VERSION=$(dotnet --version 2>&1)

if [ "$DOTNET_VERSION" != "$DOTNET_INSTALLED_VERSION" ]; then
    echo "Installing .NET CLI..."
    if [ ! -d "$SCRIPT_DIR/.dotnet" ]; then
      mkdir "$SCRIPT_DIR/.dotnet"
    fi
    curl -Lsfo "$SCRIPT_DIR/.dotnet/dotnet-install.sh" https://dot.net/v1/dotnet-install.sh
    # N.B. We explicitly install .NET Core 2.1 and 3.1 because .NET 6.0 SDK can build those TFMs
    #      but will silently upgrade to a more recent runtime to execute tests if the desired runtime
    #      isn't available. For example, `dotnet run --framework netcoreapp3.0` will silently run
    #      on .NET 6.0 if .NET Core 3.0 and 3.1 aren't installed.
    #      This solution is admittedly hacky as .NET Core 2.1 and 3.1 won't be installed if
    #      $DOTNET_VERSION matches $DOTNET_INSTALLED_VERSION, but it minimizes the changes required
    #      to install required dependencies on Evergreen.
    #      Since ARM64 support was first added in .NET 6.0, the following commands will install:
    #      | CPU   | 2.1 | 3.1 | Latest |
    #      | x64   | x64 | x64 |    x64 |
    #      | arm64 | x64 | x64 |  arm64 |
    bash "$SCRIPT_DIR/.dotnet/dotnet-install.sh" --channel 2.1 --architecture x64 --install-dir .dotnet --no-path
    bash "$SCRIPT_DIR/.dotnet/dotnet-install.sh" --channel 3.1 --architecture x64 --install-dir .dotnet --no-path
    bash "$SCRIPT_DIR/.dotnet/dotnet-install.sh" --channel 5.0 --architecture x64 --install-dir .dotnet --no-path
    bash "$SCRIPT_DIR/.dotnet/dotnet-install.sh" --version $DOTNET_VERSION --install-dir .dotnet --no-path
    export PATH="$SCRIPT_DIR/.dotnet":$PATH
    export DOTNET_ROOT="$SCRIPT_DIR/.dotnet"
fi

###########################################################################
# INSTALL CAKE
###########################################################################

CAKE_INSTALLED_VERSION=$(dotnet-cake --version 2>&1)

if [ "$CAKE_VERSION" != "$CAKE_INSTALLED_VERSION" ]; then
    if [ ! -f "$CAKE_EXE" ] || [ ! -d "$CAKE_PATH" ]; then
        if [ -f "$CAKE_EXE" ]; then
            dotnet tool uninstall --tool-path $TOOLS_DIR Cake.Tool
        fi

        echo "Installing Cake $CAKE_VERSION..."
        dotnet tool install --tool-path $TOOLS_DIR --version $CAKE_VERSION Cake.Tool
        if [ $? -ne 0 ]; then
            echo "An error occured while installing Cake."
            exit 1
        fi
    fi

    # Make sure that Cake has been installed.
    if [ ! -f "$CAKE_EXE" ]; then
        echo "Could not find Cake.exe at '$CAKE_EXE'."
        exit 1
    fi
else
    CAKE_EXE="dotnet-cake"
fi

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

# Start Cake
(exec "$CAKE_EXE" build.cake --bootstrap) && (exec "$CAKE_EXE" build.cake "$@")
