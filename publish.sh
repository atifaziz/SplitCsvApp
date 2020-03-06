#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"
if [ $# -eq 0 ]; then
    cat >&2 <<EOF
Missing Linux RID argument.

Use one of the following:

- Portable:
  - linux-x64       Most desktop distributions like CentOS, Debian, Fedora,
                    Ubuntu, and derivatives.
  - linux-musl-x64  Lightweight distributions using musl like Alpine Linux.
  - linux-arm       Linux distributions running on ARM like Raspberry Pi.
- Red Hat Enterprise Linux:
  - rhel-x64        Superseded by linux-x64 for RHEL above version 6.
  - rhel.6-x64
- Tizen:
  - tizen
  - tizen.4.0.0
  - tizen.5.0.0

For more information, see:
https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#linux-rids"
EOF
    exit 1
fi
if [[ -d dist ]]; then
    rm -rf dist
fi
mkdir dist
PUBLISH="dotnet publish -c Release"
$PUBLISH -o dist/fdd
$PUBLISH -o dist/scd          -r $1
$PUBLISH -o dist/one          -r $1 /p:PublishSingleFile=true
$PUBLISH -o dist/one+trim     -r $1 /p:PublishSingleFile=true /p:PublishTrimmed=true
$PUBLISH -o dist/one+trim+rtr -r $1 /p:PublishSingleFile=true /p:PublishTrimmed=true /p:PublishReadyToRun=true /p:PublishReadyToRunShowWarnings=true
