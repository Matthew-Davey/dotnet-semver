open System
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

let load path =
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

    match run semVerParser (File.ReadAllText(path)) with
    | Success(semver, _, _)  -> semver
    | Failure(message, _, _) -> raise (FormatException(message))

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

let printFormat (formatString: string) =
    let semver = locateSemver () |> load

    formatString
        .Replace("%M", $"%d{semver.Major}")
        .Replace("%m", $"%d{semver.Minor}")
        .Replace("%p", $"%d{semver.Patch}")
        .Replace("%s", semver.Special |> Option.map (sprintf "-%s") |> Option.defaultValue "")
        .Replace("%d", semver.Metadata |> Option.map (sprintf "+%s") |> Option.defaultValue "")
    |> printfn "%s"

let printTag () = printFormat "v%M.%m.%p%s%d"

let init () =
    let filepath = Path.Join(Environment.CurrentDirectory, ".semver")

    if File.Exists(filepath) then
        exitWithMessage ".semver already exists" -1
    else
        save filepath defaultVersion

let increment =
    function
    | "major" -> modify (fun semver -> { semver with Major = semver.Major + 1us; Minor = 0us; Patch = 0us })
    | "minor" -> modify (fun semver -> { semver with Minor = semver.Minor + 1us; Patch = 0us })
    | "patch" -> modify (fun semver -> { semver with Patch = semver.Patch + 1us })
    | other -> exitWithMessage $"%s{other} is invalid: major | minor | patch" -1

let setMetadata value =
    modify (fun semver -> { semver with Metadata = value })

let setSpecial value =
    modify (fun semver -> { semver with Special = value })

let usage =
        """
USAGE: dotnet semver [--help] [init | inc <version> |  meta <value> | special <value> | tag | format <format>]

OPTIONS:
    --help - Display this list of options.

SUBCOMMANDS:
    init             - Initializes a new .semver file with an initial version v0.1.0.
    inc <version>    - Increments the specified version number according to semver2 rules. <version> must be one of [major|minor|patch].
    special <value>  - Sets the special value.
    meta <value>     - Sets the metadata value.
    tag              - Print the tag for the current .semver file.
    format <format>  - Find the .semver file and print a formatted string from this."""

[<EntryPoint>]
let main argv =
    match argv with
    | [| "--help" |]         -> exitWithMessage usage 0
    | [| "format" |]         -> exitWithMessage "required: format string" -1
    | [| "format"; format |] -> printFormat format
    | [| "inc" |]            -> exitWithMessage "required: major | minor | patch" -1
    | [| "inc"; element |]   -> increment element
    | [| "init" |]           -> init ()
    | [| "meta" |]           -> setMetadata None
    | [| "meta"; value |]    -> setMetadata (Some value)
    | [| "special" |]        -> setSpecial None
    | [| "special"; value |] -> setSpecial (Some value)
    | [| "tag" |]            -> printTag ()
    | [||]                   -> printTag ()
    | _                      -> exitWithMessage usage -1

    exit 0
