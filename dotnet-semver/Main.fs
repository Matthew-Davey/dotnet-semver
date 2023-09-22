open System
open System.Diagnostics
open System.IO
open FParsec

type SemVer =
    { Major: uint16
      Minor: uint16
      Patch: uint16
      Special: string option
      Metadata: string option }
    
let defaultVersion = { Major = 0us; Minor = 1us; Patch = 0us; Special = None; Metadata = None }

let exitWithMessage message exitCode =
    printfn $"%s{message}"
    exit exitCode

let locateSemver () =
    let rec search directory =
        let filePath = Path.Join(directory, ".semver")

        if directory <> Environment.CurrentDirectory then
            printfn $"semver: looking at %s{filePath}"

        if File.Exists(filePath) then
            filePath
        else
            let parent = Path.GetDirectoryName(directory)

            if Directory.Exists(parent) then
                search parent
            else
                exitWithMessage $"%s{Environment.CurrentDirectory} is not semantic versioned (SemVerMissingError)" -1

    search Environment.CurrentDirectory

let load filePath =
    let quotedIdentifier =
        let quote = pchar ''' <|> pchar '"'
        let identifier = many1Chars (letter <|> digit <|> pchar '-' <|> pchar '.')
        between quote quote (opt identifier)

    let yamlProperty name valueParser =
        pstring name .>> pchar ':' .>> spaces >>. valueParser .>> spaces

    let semVerParser =
        parse {
            do! optional (skipString "---" .>> newline)
            let! major   = yamlProperty ":major" puint16
            let! minor   = yamlProperty ":minor" puint16
            let! patch   = yamlProperty ":patch" puint16
            let! special = yamlProperty ":special" quotedIdentifier
            let! meta    = opt (yamlProperty ":metadata" quotedIdentifier) |>> Option.defaultValue None

            return { Major = major; Minor = minor; Patch = patch; Special = special; Metadata = meta }
        }
        
    match run semVerParser (File.ReadAllText(filePath)) with
    | Success(semver, _, _)  -> semver
    | Failure(message, _, _) -> raise (FormatException(message))
    
let read () = locateSemver () |> load

let save path semver =
    let contents =
        $"""---
:major: %i{semver.Major}
:minor: %i{semver.Minor}
:patch: %i{semver.Patch}
:special: '%s{semver.Special |> Option.defaultValue ""}'
:metadata: '%s{semver.Metadata |> Option.defaultValue ""}'"""

    File.WriteAllText(path, contents)

let modify transform =
    let filePath = locateSemver ()
    load filePath |> transform |> save filePath
    
let format (formatString: string) semver =
    formatString
        .Replace("%M", $"%d{semver.Major}")
        .Replace("%m", $"%d{semver.Minor}")
        .Replace("%p", $"%d{semver.Patch}")
        .Replace("%s", semver.Special |> Option.map (sprintf "-%s") |> Option.defaultValue "")
        .Replace("%d", semver.Metadata |> Option.map (sprintf "+%s") |> Option.defaultValue "")

let tagFormat = "v%M.%m.%p%s%d"

let init force =
    let filepath = Path.Join(Environment.CurrentDirectory, ".semver")

    if File.Exists(filepath) && not force then
        exitWithMessage ".semver already exists" -1
    else
        save filepath defaultVersion

let increment element semver =
    match element with
    | "major" -> { semver with Major = semver.Major + 1us; Minor = 0us; Patch = 0us }
    | "minor" -> { semver with Minor = semver.Minor + 1us; Patch = 0us }
    | "patch" -> { semver with Patch = semver.Patch + 1us }
    | other -> exitWithMessage $"%s{other} is invalid: major | minor | patch" -1
    
let setMetadata value semver =
    { semver with Metadata = value }

let setSpecial value semver =
    { semver with Special = value }
    
let spawn command (args: string list) =
    let semver = locateSemver () |> load |> format "%M.%m.%p%s%d"
    let start = ProcessStartInfo("dotnet", $"%s{command} /p:Version=%s{semver} %s{String.Join(' ', (List.toArray args))}")
    let proc = Process.Start(start)
    proc.WaitForExit()
    exit proc.ExitCode

let usage = """
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
    publish [args]         - Executes dotnet publish, passing the current semver as a switch."""

[<EntryPoint>]
let main argv =
    match Array.toList argv with
    | ["--help"]                         -> exitWithMessage usage 0
    | ["format"]                         -> exitWithMessage "required: format string" -1
    | ["format"; fmt]                    -> read () |> format fmt |> printfn "%s"
    | ["inc" | "increment"]              -> exitWithMessage "required: major | minor | patch" -1
    | ["inc" | "increment"; element]     -> modify (increment element)
    | ["init" | "initialize"]            -> init false
    | ["init" | "initialize"; "--force"] -> init true
    | ["meta" | "metadata"]              -> modify (setMetadata None)
    | ["meta" | "metadata"; value]       -> modify (setMetadata (Some value))
    | ["pre" | "prerelease"]             -> modify (setSpecial None)
    | ["pre" | "prerelease"; value]      -> modify (setSpecial (Some value))
    | ["spe" | "special"]                -> modify (setSpecial None)
    | ["spe" | "special"; value]         -> modify (setSpecial (Some value))
    | ["tag"]                            -> read () |> format tagFormat |> printfn "%s"
    | ["next"]                           -> exitWithMessage "required: major | minor | patch" -1
    | ["next"; element]                  -> read () |> increment element |> format tagFormat |> printfn "%s"
    | "build"::args                      -> spawn "build" args
    | "pack"::args                       -> spawn "pack" args
    | "publish"::args                    -> spawn "publish" args
    | []                                 -> read () |> format tagFormat |> printfn "%s"
    | _                                  -> exitWithMessage usage -1

    exit 0
