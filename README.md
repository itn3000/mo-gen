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

# Related links

* [MagicOnion Repository](https://github.com/neuecc/MagicOnion)
