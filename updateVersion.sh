#! /bin/bash

if [[ ! -z "$1" ]] ; then
    export BUILDVERSION=${1}
fi
export BUILDVERSION=${BUILDVERSION:-./BuildVersion/BuildVersion.exe}

$BUILDVERSION \
        --verbose \
        --namespace org.herbal3d.Loden \
        --version $(cat VERSION) \
        --versionFile Loden/VersionInfo.cs \
        --assemblyInfoFile Loden/Properties/AssemblyInfo.cs
