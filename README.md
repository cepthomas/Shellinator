
# Shellinator
Playing with shell extensions to provide some custom context menus.

Consists of a simple command line client that is called from registry commands.
It executes the requested operation and copies any standard output to the clipboard.

It is also used to configure registry commands by executing in Visual Studio.

This is not currently a general purpose tool but rather a collection of specific customizations.
To make your own, copy `Commands.cs` and hack away using VS2022 and .NET8.
Also it requires an env var called `TOOLS_PATH` where the binaries are copied, and a corresponding entry in `PATH`.
Alternatively, the `App.cs` and `Shellinator.csproj` files can be modified to taste.
