# Shellinator

Playing with shell extensions to provide some custom context menus. This can only add,
removal or editing is a much more murky subject.

Consists of a simple command line client that is called from registry commands.
It executes the requested operation and copies any standard output to the clipboard.

Configure with `shellinator.ini` in the same directory as the executable. A default is created
the first time the application is run. Refer to that file for specification of the custom entries.
When edited, run `shellinator reg` to register them. If the config needs to be changed, first
run `shellinator unreg`, make the edits, then run `shellinator reg` again.

If this gets a bit messed up, the registry entries can be removed manually.
Delete these entries:
```
HKEY_CURRENT_USER\Software\Classes\Directory\shell\<your_commands>
HKEY_CURRENT_USER\Software\Classes\Directory\Background\shell\<your_commands>
HKEY_CURRENT_USER\Software\Classes\DesktopBackground\shell\<your_commands>
HKEY_CURRENT_USER\Software\Classes\*\shell\run]
```
