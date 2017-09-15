#!/bin/bash
set -euxo pipefail

pushd "$(dirname ${BASH_SOURCE[0]})"

PROTOC=~/.nuget/packages/grpc.tools/1.6.0/tools/macosx_x64/protoc
PLUGIN=~/.nuget/packages/grpc.tools/1.6.0/tools/macosx_x64/grpc_csharp_plugin

$PROTOC -I. \
  --csharp_out ../FdwSharp/gen \
  fdw.proto \
  --grpc_out ../FdwSharp/gen \
  --plugin=protoc-gen-grpc=$PLUGIN

# pip install grpcio-tools
python2.7 -m grpc_tools.protoc \
  -I. \
  --python_out=../multicorn/grpc_fdw \
  --grpc_python_out=../multicorn/grpc_fdw \
  fdw.proto 

popd