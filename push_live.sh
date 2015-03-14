#!/bin/bash
echo "Initializing Repositiory"
git submodule init
git submodule update
nuget restore

echo "Building solutuion"

xbuild /p:Configuration=Release

echo "Creating 'live' directory"

# create "live" dir if not existing
mkdir -p live__

echo "Copying builds"
# Update Builds
cp -R runscripts/* ./live__
cp -R FuckThisFuckingCGIFuck/bin/Release/* ./live__
cp -R WebSocketServer/bin/Release/* ./live__

echo "Finished build, restarting"

cd live__
./kill_cgi.sh
./cgi.sh
./kill_wss.sh
./wss.sh

cd ..

echo "Finished."
