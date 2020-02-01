namespace Raznor.Core

module CollectionSettings =
    open System
    open System.IO
    open Types
    open LiteDB

    let createBasicPaths =
        let col = Database.getCollectionSettings()

        let collections: CollectionSettings list =
            let onedrivepath = Environment.GetEnvironmentVariable("OneDriveConsumer")

            let onedrivemusic =
                match String.IsNullOrEmpty(onedrivepath) with
                | true -> ""
                | false -> Path.Combine(onedrivepath, "Music")

            [ yield { id = ObjectId.NewObjectId()
                      name = "My Music"
                      path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
                      createdAt = DateTime.Now }
              if not (String.IsNullOrEmpty(onedrivepath)) then
                  yield { id = ObjectId.NewObjectId()
                          name = "One Drive Music"
                          path = onedrivemusic
                          createdAt = DateTime.Now } ]
        col.Value.Insert(collections) |> ignore
        col.Value.Find(Query.All(1)) |> Seq.toList

    let getDefaultPaths =
        let col = Database.getCollectionSettings()
        let found = col.Value.Find(Query.All(1)) |> Seq.toList

        let paths =
            match found |> List.isEmpty with
            | true -> createBasicPaths
            | false -> found
        paths
