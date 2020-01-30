namespace Raznor.Core


module MusicCollections =
    open System
    open LiteDB
    open Types

    let private musicol (db: LiteDatabase) =
        let col = db.GetCollection<Types.MusicCollection>()
        col.EnsureIndex(fun col -> col.name) |> ignore
        col

    let private songcol (db: LiteDatabase) =
        let col = db.GetCollection<Types.SongRecord>()
        col.EnsureIndex(fun col -> col.path) |> ignore
        col.EnsureIndex(fun col -> col.name) |> ignore
        col

    let getMusicCollections: Types.MusicCollection list =
        use db = Database.getDatabase Database.dbpath
        let col = musicol db
        col.Find(Query.All(1)) |> Seq.toList


    let createMusicCollections (cols: Types.MusicCollection seq) =
        use db = Database.getDatabase Database.dbpath
        let col = musicol db
        sprintf "%A" cols |> ignore
        col.Insert(cols) |> ignore
        col.Find(Query.All(1)) |> Seq.toList


    let getSongsByMusicCollection (identifier: ObjectId) =
        use db = Database.getDatabase Database.dbpath
        let col = songcol db
        col.Find(fun song -> song.isIn |> List.exists (fun id -> id = identifier)) |> Seq.toList

    let addSongToCollection (song: SongRecord) (collection: MusicCollection) =
        use db = Database.getDatabase Database.dbpath
        let soncol = songcol db
        match soncol.Exists(fun dbsong -> song.id = dbsong.id) with
        | true ->
            let song = soncol.FindOne(fun son -> son.id = song.id)

            let toSave =
                match song.isIn |> List.exists (fun id -> collection.id = id) with
                | true -> song
                | false -> { song with isIn = collection.id :: song.isIn }
            soncol.Update(toSave) |> ignore
        | false ->
            let song = { song with isIn = [ collection.id ] }
            soncol.Insert(song) |> ignore

    let getAllSongs =
        use db = Database.getDatabase Database.dbpath
        let col = songcol db
        col.Find(Query.All(1)) |> Seq.toList

    let getPreMusiColFromPath (settings: CollectionSettings): MusicCollection =
        { id = ObjectId.NewObjectId()
          name = settings.name
          createdAt = DateTime.Now
          updatedAt = DateTime.Now }
