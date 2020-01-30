namespace Raznor.Core

module CollectionSettings =
    open System
    open System.IO
    open Types
    open LiteDB

    let private colSettings (db: LiteDatabase) =
        let col = db.GetCollection<Types.CollectionSettings>()
        col.EnsureIndex "path" |> ignore
        col.EnsureIndex "name" |> ignore
        col

    let createBasicPaths =
        use db = Database.getDatabase Database.dbpath
        let col = colSettings db

        let collections: CollectionSettings list =
            let onedrivepath = Environment.GetEnvironmentVariable("OneDriveConsumer")
            let onedrivemusic = Path.Combine(onedrivepath, "Music")
            [ yield { id = ObjectId.NewObjectId()
                      name = "My Music"
                      path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
                      createdAt = DateTime.Now }
              if not (String.IsNullOrEmpty(onedrivepath)) then
                  yield { id = ObjectId.NewObjectId()
                          name = "One Drive Music"
                          path = onedrivemusic
                          createdAt = DateTime.Now } ]
        col.Insert(collections) |> ignore
        col.Find(Query.All(1)) |> Seq.toList

    let getDefaultPaths =
        use db = Database.getDatabase Database.dbpath
        let col = colSettings db
        let found = col.Find(Query.All(1)) |> Seq.toList

        let paths =
            match found |> List.isEmpty with
            | true -> createBasicPaths
            | false -> found
        paths
