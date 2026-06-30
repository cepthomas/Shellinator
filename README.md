
# Shellinator

Playing with shell extensions to provide some custom context menus.

- Consists of a simple command line client that is called from registry commands.
  It executes the requested operation and copies any standard output to the clipboard.
- Currently this is a build-it-yourself tool using VS2022 and .NET8.
  To make your own, copy `Commands.cs` and hack to taste.
- It requires an env var called `TOOLS_PATH` where the binaries are copied and where the registry commands
  point to, plus a corresponding entry in `PATH`. Alternatively, the `App.cs` and `Shellinator.csproj` files
  can be modified to taste and rebuilt.

This is really clunky for a general purpose tool. The plan is to migrate to a version that is configured by
an `.ini` file. This is partially implemented by code with `Nuevo` in the names.
