open System
open System.Diagnostics
open System.IO
open FSharpPlus
open FParsec

type SemVer =
    { Major: uint16
      Minor: uint16
      Patch: uint16
      Special: string option
      Metadata: string option }

type Element =
    | Major
    | Minor
    | Patch

let (|??) a b = Option.defaultValue b a

let defaultVersion =
    { Major = 0us
      Minor = 1us
      Patch = 0us
      Special = None
      Metadata = None }

let exitWithMessage exitCode message =
    printfn $"%s{message}"
    exit exitCode

let locateSemVer () =
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
                exitWithMessage -1 $"%s{Environment.CurrentDirectory} is not semantic versioned (SemVerMissingError)"

    search Environment.CurrentDirectory

let load filePath =
    let quotedIdentifier =
        let quote = choice [ pchar '''; pchar '"' ]
        let identifier = many1Chars (choice [ letter; digit; pchar '-'; pchar '.' ])
        between quote quote (opt identifier)

    let yamlProperty name valueParser =
        pstring name >>. pchar ':' >>. spaces >>. valueParser

    let semVerParser =
        optional (skipString "---" >>. newline) >>. yamlProperty ":major" puint16 .>> newline
        >>= fun major ->
            yamlProperty ":minor" puint16 .>> newline
            >>= fun minor ->
                yamlProperty ":patch" puint16 .>> newline
                >>= fun patch ->
                    yamlProperty ":special" quotedIdentifier .>> optional newline
                    >>= fun special ->
                        opt (yamlProperty ":metadata" quotedIdentifier) .>> optional newline
                        >>= fun meta ->
                            preturn
                                { Major = major
                                  Minor = minor
                                  Patch = patch
                                  Special = special
                                  Metadata = Option.flatten meta }

    match run semVerParser (File.ReadAllText(filePath)) with
    | Success(semver, _, _) -> semver
    | Failure(message, _, _) -> raise (FormatException(message))

let read = locateSemVer >> load

let save path semver =
    let contents =
        $"""---
:major: %i{semver.Major}
:minor: %i{semver.Minor}
:patch: %i{semver.Patch}
:special: '%s{semver.Special |?? ""}'
:metadata: '%s{semver.Metadata |?? ""}'"""

    File.WriteAllText(path, contents)

let update transform =
    let filePath = locateSemVer ()
    load filePath |> transform |> save filePath

let format formatString semver =
    formatString
    |> String.replace "%M" $"%d{semver.Major}"
    |> String.replace "%m" $"%d{semver.Minor}"
    |> String.replace "%p" $"%d{semver.Patch}"
    |> String.replace "%s" (semver.Special |> Option.map (sprintf "-%s") |?? "")
    |> String.replace "%d" (semver.Metadata |> Option.map (sprintf "+%s") |?? "")

let tagFormat = "v%M.%m.%p%s%d"

let init force =
    let filepath = Path.Join(Environment.CurrentDirectory, ".semver")

    if File.Exists(filepath) && not force then
        exitWithMessage -1 ".semver already exists"
    else
        save filepath defaultVersion

let increment element semver =
    match element with
    | Major ->
        { semver with
            Major = semver.Major + 1us
            Minor = 0us
            Patch = 0us }
    | Minor ->
        { semver with
            Minor = semver.Minor + 1us
            Patch = 0us }
    | Patch ->
        { semver with
            Patch = semver.Patch + 1us }

let setMetadata value semver = { semver with Metadata = value }

let setSpecial value semver = { semver with Special = value }

let spawn command args =
    let semVer = locateSemVer () |> load |> format "%M.%m.%p%s%d"

    let start =
        ProcessStartInfo("dotnet", $"""%s{command} /p:Version=%s{semVer} %s{args |?? ""}""")

    let proc = Process.Start(start)
    proc.WaitForExit()
    exit proc.ExitCode

let usage =
    """
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

type Argument =
    | Help
    | Format of FormatString: string
    | Increment of Element: Element
    | Initialize of Force: bool
    | Metadata of Value: string option
    | Special of Value: string option
    | Tag
    | Next of Element: Element
    | Dotnet of Command: string * Args: string option

let parseArguments =
    let element =
        choice
            [ stringReturn "major" Major
              stringReturn "minor" Minor
              stringReturn "patch" Patch ]

    let identifier = many1Chars (choice [ letter; digit; pchar '-'; pchar '.' ])
    let dotnetCommand = choice [ pstring "build"; pstring "pack"; pstring "publish" ]

    let abbr term abbreviated =
        pstring abbreviated >>. optional (pstring (String.drop abbreviated.Length term))

    (eof >>% Tag)
    <|> (pstring "--help" <|> pstring "help" >>. eof >>% Help)
    <|> (pstring "format" >>. spaces1 >>. many1Chars anyChar .>> eof |>> Format)
    <|> (abbr "increment" "inc" >>. spaces1 >>. element .>> eof |>> Increment)
    <|> (abbr "initialize" "init" >>. opt (spaces1 >>. pstring "--force") .>> eof |>> Option.isSome |>> Initialize)
    <|> (abbr "metadata" "meta" >>. opt (spaces1 >>. identifier) .>> eof |>> Metadata)
    <|> (abbr "prerelease" "pre" <|> abbr "special" "spe" >>. opt (spaces1 >>. identifier) .>> eof |>> Special)
    <|> (pstring "tag" >>. eof >>% Tag)
    <|> (pstring "next" >>. spaces1 >>. element .>> eof |>> Next)
    <|> (dotnetCommand .>>. (opt (spaces1 >>. many1Chars anyChar)) .>> eof |>> Dotnet)

[<EntryPoint>]
let main argv =
    match run parseArguments (String.concat " " argv) with
    | Success(command, _, _) ->
        match command with
        | Help -> exitWithMessage 0 usage
        | Format formatString -> read () |> format formatString |> printfn "%s"
        | Increment element -> update (increment element)
        | Initialize force -> init force
        | Metadata value -> update (setMetadata value)
        | Special value -> update (setSpecial value)
        | Tag -> read () |> format tagFormat |> printfn "%s"
        | Next element -> read () |> increment element |> format tagFormat |> printfn "%s"
        | Dotnet(command, args) -> spawn command args

        exit 0
    | Failure(error, _, _) ->
        eprintfn $"%s{error}"
        exit -1
