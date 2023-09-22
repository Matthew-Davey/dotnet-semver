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
# Find the .semver file and print a formatted string from this.
semver                    # => v2.3.4

# Initialize the .semver file.
semver init

# Print the tag for the current .semver file.
semver tag                # => v0.1.0

semver inc minor          # => v0.2.0
semver pre "alpha.45"     # => v0.2.0-alpha.45
semver meta "md5.abc123"  # => v0.2.0-alpha.45+md5.abc123
semver format "%M.%m.x"   # => 0.2.x
semver meta               # => v0.2.0-alpha.45
git tag -a `semver tag`
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
git config --global alias.semtag '!git tag -a $(semver tag) -m "tagging $(semver tag)"'
```

Usage
-----

```shell
USAGE: dotnet semver [--help] [init | inc <version> |  meta <value> | special <value> | tag | format <format>]

OPTIONS:
    --help - Display this list of options.

SUBCOMMANDS:
    init             - Initializes a new .semver file with an initial version v0.1.0.
    inc <version>    - Increments the specified version number according to semver2 rules. <version> must be one of [major|minor|patch].
    special <value>  - Sets the special value.
    meta <value>     - Sets the metadata value.
    tag              - Print the tag for the current .semver file.
    format <format>  - Find the .semver file and print a formatted string from this.
    
DOTNET CLI WRAPPERS:
    build [args]     - Executes dotnet build, passing the current semver as a switch.
    pack [args]      - Executes dotnet pack, passing the current semver as a switch.
    publish [args]   - Executes dotnet publish, passing the current semver as a switch.
```

Credits
-------
* [Matthew Davey](mailto:matt.davey@fsfe.org)
