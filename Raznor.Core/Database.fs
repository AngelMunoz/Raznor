namespace Raznor.Core

module Database =
    open LiteDB
    open LiteDB.FSharp
    open System
    open System.IO


    let private getInstance (path: string) =
        let mapper = FSharpBsonMapper()
        new LiteDatabase(path, mapper)

    let private localAppDir =
        let appData = Environment.GetFolderPath Environment.SpecialFolder.LocalApplicationData
        let directory = Path.Combine(appData, "RaznorApp")
        match Directory.Exists directory with
        | true -> ()
        | false -> Directory.CreateDirectory directory |> ignore
        directory

    let private dbpath =
        let directory = localAppDir
        let path = Path.Combine(directory, "RaznorApp.db")
        sprintf "Filename=%s;Async=true" path

    let private db = lazy (getInstance dbpath)

    let private musicol =
        Lazy<_>.Create
                (fun _ ->
                    let col = db.Value.GetCollection<Types.MusicCollection>()
                    col.EnsureIndex(fun col -> col.name) |> ignore
                    col)

    let private colsettings =
        Lazy<_>.Create
                (fun _ ->
                    let col = db.Value.GetCollection<Types.CollectionSettings>()
                    col.EnsureIndex("name", true) |> ignore
                    col.EnsureIndex("path", true) |> ignore
                    col)

    let private songcol =
        Lazy<_>.Create
                (fun _ ->
                    let col = db.Value.GetCollection<Types.SongRecord>()
                    col.EnsureIndex(fun col -> col.path) |> ignore
                    col.EnsureIndex(fun col -> col.name) |> ignore
                    col)

    let getDatabase() = db
    let getMusicCollections() = musicol
    let getSongs() = songcol
    let getCollectionSettings() = colsettings
