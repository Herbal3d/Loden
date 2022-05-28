#! /bin/bash

BUILDVERSION=${1:-./BuildVersion/BuildVersion.exe}

$BUILDVERSION \
        --verbose \
        --namespace org.herbal3d.Loden \
        --version $(cat VERSION) \
        --versionFile Loden/VersionInfo.cs \
        --assemblyInfoFile Loden/Properties/AssemblyInfo.cs
