# The MagicOnion code generator

This is the fork of MagicOnion.CodeGenerator.
The goal of this project is merged into [MagicOnion](https://github.com/neuecc/MagicOnion/)'s code generator .

# How to use

## Prerequisits

* [dotnet core sdk 2.1.300 or later](https://www.microsoft.com/net/download/windows)

## Build

1. `cd ./src/mo-gen`
2. `dotnet pack -c Release`
3. `dotnet tool install -g dotnet-mo-gen --add-source ./bin/Release dotnet-mo-gen`

## Usage

run `dotnet mo-gen --help`

### Usage Warning

if you use multi targetted framework(TargetFrameworks), you must add `-p TargetFramework=[your targetframework]`.

## Changes from MagicOnion.CodeGenerator

* build as [dotnet global tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools)
    * do not need Mono in Mac
* use [Buildalyzer](https://github.com/daveaglick/Buildalyzer) instead of MSBuildWorkspace
* add `-h|--help` option
* add `p|property` option

# Related links

* [MagicOnion Repository](https://github.com/neuecc/MagicOnion)
