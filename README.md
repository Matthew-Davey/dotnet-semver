dotnet-semver
=============

`dotnet-semver` is a dotnet re-implementation of the [ruby semver2 gem](https://github.com/haf/semver) cli.

Quickstart
----------
#### Install

```shell
dotnet tool install -g dotnet-semver
```

#### Use

```shell
# Initialize the .semver file.
dotnet semver init

# Find the .semver file and print a formatted string from this.
dotnet semver                    # => v0.1.0

# Print the tag for the current .semver file.
dotnet semver tag                # => v0.1.0

dotnet semver inc minor          # => v0.2.0
dotnet semver pre "alpha.45"     # => v0.2.0-alpha.45
dotnet semver meta "md5.abc123"  # => v0.2.0-alpha.45+md5.abc123
dotnet semver format "%M.%m.x"   # => 0.2.x
dotnet semver meta               # => v0.2.0-alpha.45
git tag -a `dotneet semver tag`
say 'that was easy'
```

```shell
cat .semver
---
:major: 0
:minor: 2
:patch: 0
:special: 'alpha.45'
:metadata: ''
```

Wrap dotnet CLI Commands
--------------------
`dotnet-semver` can wrap the dotnet cli commands `build`, `pack` and `publish`, automatically adding the necessary version switch to the command line.

For example the command:

`dotnet semver build --configuration Release`

is executed as:

`dotnet build /p:Version=0.2.0-alpha.45 --configuration Release`

Git Integration
---------------

```shell
git config --global alias.semtag '!git tag -a $(dotnet semver tag) -m "tagging $(dotnet semver tag)"'
```

Usage
-----

```shell
USAGE: dotnet semver [--help] [init [--force] | inc <version> | pre <value> | meta <value> | tag | next <version> | format <format>]

OPTIONS:
    --help - Display this list of options.

SUBCOMMANDS:
    init[ialize] [--force] - Initializes a new .semver file with an initial version v0.1.0.
    inc[rement] <version>  - Increments the specified version number according to semver2 rules. <version> must be one of [major|minor|patch].
    pre[release] <value>   - Sets the pre-release version suffix.
    meta[data] <value>     - Sets the metadata value.
    next <version>         - Format incremented specific version without saving it. <version> must be one of [major|minor|patch].
    tag                    - Print the tag for the current .semver file.
    format <format>        - Find the .semver file and print a formatted string from this.
    
DOTNET CLI WRAPPERS:
    build [args]           - Executes dotnet build, passing the current semver as a switch.
    pack [args]            - Executes dotnet pack, passing the current semver as a switch.
    publish [args]         - Executes dotnet publish, passing the current semver as a switch.
```

Credits
-------
* [Matthew Davey](mailto:matt.davey@fsfe.org)
