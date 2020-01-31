namespace Raznor.Core


module MusicCollections =
    open System
    open LiteDB
    open Types

    let getMusicCollections: Types.MusicCollection list =
        let col = Database.getMusicCollections()
        col.Value.Find(Query.All(1)) |> Seq.toList


    let createMusicCollections (cols: Types.MusicCollection seq) =
        let col = Database.getMusicCollections()
        sprintf "%A" cols |> ignore
        col.Value.Insert(cols) |> ignore
        col.Value.Find(Query.All(1)) |> Seq.toList


    let getSongsByMusicCollection (identifier: ObjectId) =
        let col = Database.getSongs()
        col.Value.Find(fun song -> song.isIn |> List.exists (fun id -> id = identifier)) |> Seq.toList

    let addNewSong (song: SongRecord) =
        let col = Database.getSongs()
        col.Value.Insert(song) |> ignore

    let addNewSongBatch (songs: SongRecord array) =
        let col = Database.getSongs()
        col.Value.InsertBulk(songs) |> ignore

    let getAllSongs =
        let col = Database.getSongs()
        col.Value.Find(Query.All(1)) |> Seq.toList

    let getPreMusiColFromPath (settings: CollectionSettings): MusicCollection =
        { id = ObjectId.NewObjectId()
          name = settings.name
          createdAt = DateTime.Now
          updatedAt = DateTime.Now }
