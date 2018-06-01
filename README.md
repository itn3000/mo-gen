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

## Difference from MagicOnion.CodeGenerator

* build as dotnet global tools
    * do not need Mono in Mac
* use [Buidalyzer](https://github.com/daveaglick/Buildalyzer) instead of MSBuildWorkspace
* add `-h|--help` option

# Related links

* [MagicOnion Repository](https://github.com/neuecc/MagicOnion)
