namespace Raznor.Core

module MusicCollections =
    open LiteDB
    open System
    open System.IO
    open Types

    let getMusicCollections: Types.MusicCollection list =
        let col = Database.getMusicCollections()
        col.Value.Find(Query.All(1)) |> Seq.toList


    let createMusicCollections (cols: Types.MusicCollection seq) =
        let col = Database.getMusicCollections()
        let finalcols =
            cols |> Seq.filter (fun musicol -> not (col.Value.Exists(fun existing -> existing.name = musicol.name)))
        col.Value.Insert(finalcols) |> ignore
        col.Value.Find(Query.All(1)) |> Seq.toList


    let getSongsByMusicCollection (identifier: ObjectId) =
        let col = Database.getSongs()
        col.Value.Find(fun song -> song.belongsTo = identifier) |> Seq.toList

    let addNewSong (song: SongRecord) =
        let col = Database.getSongs()
        col.Value.Insert(song) |> ignore

    let addNewSongBatch (songs: SongRecord array) =
        let col = Database.getSongs()
        col.Value.InsertBulk(songs) |> ignore

    let getAllSongs =
        let col = Database.getSongs()
        col.Value.Find(Query.All(1)) |> Seq.toList

    let private getPreMusiColFromPath (settings: Types.CollectionSettings): Types.MusicCollection =
        { id = ObjectId.NewObjectId()
          name = settings.name
          createdAt = DateTime.Now
          updatedAt = DateTime.Now }

    let private getPathsAndCollections (targets: Types.MusicCollection list) (targetSettings: Types.CollectionSettings) =
        let col = targets |> List.find (fun col -> col.name = targetSettings.name)
        let di = DirectoryInfo(targetSettings.path)

        let getFiles (extension: string) =
            let getFileAndPath (file: FileInfo) = file.Name, file.FullName
            di.GetFiles(extension, SearchOption.AllDirectories) |> Array.Parallel.map getFileAndPath

        let files =
            [| getFiles "*.mp3"
               getFiles "*.wav"
               getFiles "*.mid" |]
            |> Array.Parallel.collect id

        col.id, files

    let private createSongsFromCollection (row: ObjectId * (string * string) []) =
        let collection, items = row

        let songRecord (name, path): Types.SongRecord =
            { id = ObjectId.NewObjectId()
              name = name
              path = path
              createdAt = DateTime.Now
              belongsTo = collection }
        items
        |> Array.Parallel.map songRecord
        |> addNewSongBatch


    let defaultMusicCollections =
        let collections = getMusicCollections
        let emptylist = collections |> List.isEmpty
        match emptylist with
        | true ->
            CollectionSettings.getDefaultPaths
            |> List.map getPreMusiColFromPath
            |> createMusicCollections
        | false -> collections

    let populateSongs (paths: string array): Types.SongRecord array =
        paths
        |> Array.Parallel.map FileInfo
        |> Array.Parallel.map (fun info -> info.Name, info.FullName)
        |> Array.Parallel.map (fun (name, path) ->
            { id = ObjectId.NewObjectId()
              name = name
              path = path
              createdAt = DateTime.Now
              belongsTo = null })

    let populateFromDirectory (path: string): Types.SongRecord array =
        match String.IsNullOrEmpty path with
        | true -> Array.empty
        | false ->
            let dirinfo = DirectoryInfo path
            dirinfo.GetFiles()
            |> Array.filter (fun info -> info.Extension = ".mp3" || info.Extension = ".wav" || info.Extension = ".mid")
            |> Array.Parallel.map (fun info -> info.Name, info.FullName)
            |> Array.Parallel.map (fun (name, path) ->
                { id = ObjectId.NewObjectId()
                  name = name
                  path = path
                  createdAt = DateTime.Now
                  belongsTo = null })


    let createDefaultSongs (paths: Types.CollectionSettings list) =
        paths
        |> List.map (fun collection -> getPathsAndCollections defaultMusicCollections collection)
        |> List.map createSongsFromCollection
