# Samples

These small console projects are used to exercise the `dotnet-archive` tool.

## HelloWorld
A minimal framework-dependent console app. Use it to test the basic export/run flow:

```bash
dotnet archive export ../HelloWorld
dotnet archive run ../HelloWorld
dotnet archive run ../HelloWorld -- --name Alice
```

## HelloWorld.SelfContained
A minimal console app used to demonstrate `--self-contained`:

```bash
dotnet archive export ../HelloWorld.SelfContained --self-contained --runtime linux-x64
```

The resulting archive includes a portable .NET runtime and can be run on a Linux x64 machine without .NET installed.

## MultiProject
A solution with a class library (`Greeter`) and an executable (`MultiProject`) that references it. Verifies that publish correctly bundles referenced projects.
