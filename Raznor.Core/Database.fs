namespace Raznor.Core

module Database =
    open LiteDB
    open LiteDB.FSharp
    open System
    open System.IO

    let getDatabase (path: string) =
        let mapper = FSharpBsonMapper()
        new LiteDatabase(path, mapper)

    let localAppDir =
        let appData = Environment.GetFolderPath Environment.SpecialFolder.LocalApplicationData
        let directory = Path.Combine(appData, "RaznorApp")
        match Directory.Exists directory with
        | true -> ()
        | false -> Directory.CreateDirectory directory |> ignore
        directory

    let dbpath =
        let directory = localAppDir
        let path = Path.Combine(directory, "RaznorApp.db")
        sprintf "Filename=%s;Async=true" path
