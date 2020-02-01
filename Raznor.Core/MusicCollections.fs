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
          cols 
          |> Seq.filter(fun musicol -> not (col.Value.Exists(fun existing -> existing.name = musicol.name)))
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
    
    let getPreMusiColFromPath (settings: Types.CollectionSettings): Types.MusicCollection =
      { id = ObjectId.NewObjectId()
        name = settings.name
        createdAt = DateTime.Now
        updatedAt = DateTime.Now }

    let getPathsAndCollections (targets: Types.MusicCollection list) (targetSettings: Types.CollectionSettings) =
      let col = targets |> List.find (fun col -> col.name = targetSettings.name)
      let di = DirectoryInfo(targetSettings.path)

      let getFiles (extension: string) =
          let getFileAndPath (file: FileInfo) = file.Name, file.FullName
          di.GetFiles(extension, SearchOption.AllDirectories) |> Array.Parallel.map getFileAndPath

      let files =
          [| getFiles "*.mp3"
             getFiles "*.wav"
             getFiles "*.mid" |]
          |> Array.Parallel.collect (fun list -> list)

      col.id, files

    let createSongsFromCollection (row: ObjectId * (string * string) []) =
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
