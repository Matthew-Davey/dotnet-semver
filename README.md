dotnet-semver
=============

[![NuGet Version](https://img.shields.io/nuget/vpre/dotnet-semver?style=for-the-badge&logo=nuget&label=latest)](https://www.nuget.org/packages/dotnet-semver/latest)
[![NuGet Downloads](https://img.shields.io/nuget/dt/dotnet-semver?style=for-the-badge&logo=nuget)](https://www.nuget.org/stats/packages/dotnet-semver?groupby=Version)
[![GitHub License](https://img.shields.io/github/license/Matthew-Davey/dotnet-semver?style=for-the-badge)](https://github.com/Matthew-Davey/dotnet-semver/blob/master/LICENSE)

---

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

dotnet semver inc major          # => v1.0.0
dotnet semver inc minor          # => v1.1.0
dotnet semver pre "alpha.45"     # => v1.1.0-alpha.45
dotnet semver meta "md5.abc123"  # => v1.1.0-alpha.45+md5.abc123
dotnet semver format "%M.%m.x"   # => 1.1.x
dotnet semver meta               # => v1.1.0-alpha.45
dotnet semver inc minor          # => v1.2.0-alpha.45
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
`dotnet-semver` can wrap the dotnet cli commands `build`, `pack`, `publish` and `run`, automatically adding the necessary version switch to the command line.

For example the command:

`dotnet semver build --configuration Release`

is executed as:

`dotnet build /p:Version=1.2.0-alpha.45 --configuration Release`

Git Integration
---------------

```shell
git config alias.semtag '!git tag -a $(dotnet semver tag) -m "tagging $(dotnet semver tag)"'

```

Usage
-----

```shell
USAGE: dotnet semver [--help] [init [--force] | inc <version> | pre <value> | meta <value> | tag | next <version> | format [-n] <format>]

OPTIONS:
    --help  - Display this list of options.
    --force - Force creation of a new .semver file, even if one already exists.
    -n      - Do not output a trailing newline.

SUBCOMMANDS:
    init[ialize] [--force] - Initializes a new .semver file with an initial version v0.1.0.
    inc[rement] <version>  - Increments the specified version number according to semver2 rules. <version> must be one of [major|minor|patch].
    pre[release] <value>   - Sets the pre-release version suffix.
    meta[data] <value>     - Sets the metadata value.
    next <version>         - Format incremented specific version without saving it. <version> must be one of [major|minor|patch].
    tag                    - Print the tag for the current .semver file.
    format [-n] <format>   - Find the .semver file and print a formatted string from this..
    
DOTNET CLI WRAPPERS:
    build [args]           - Executes dotnet build, passing the current semver as a switch.
    pack [args]            - Executes dotnet pack, passing the current semver as a switch.
    publish [args]         - Executes dotnet publish, passing the current semver as a switch.
    run [args]             - Executes dotnet run, passing the current semver as a switch.
```

Format String Tokens
--------------

* Major: `%M`
* Minor: `%m`
* Patch: `%p`
* Special: `%s`
* Metadata: `%d`

Credits
-------
* [Matthew Davey](mailto:matt.davey@fsfe.org)
