namespace Raznor.Core

module Songs =
    open System
    open System.IO
    open Types

    let populateSongs (paths: string array) : Types.SongRecord array =
        paths
        |> Array.Parallel.map FileInfo
        |> Array.Parallel.map (fun info -> info.Name, info.FullName)
        |> Array.Parallel.map (fun (name, path) ->
            { id = Guid.NewGuid()
              name = name
              path = path
              createdAt = DateTime.Now })

    let populateFromDirectory (path: string) : Types.SongRecord array =
        match String.IsNullOrEmpty path with
        | true -> Array.empty
        | false ->
            let dirinfo = DirectoryInfo path

            dirinfo.GetFiles()
            |> Array.filter (fun info -> info.Extension = ".mp3" || info.Extension = ".wav")
            |> Array.Parallel.map (fun info -> info.Name, info.FullName)
            |> Array.Parallel.map (fun (name, path) ->
                { id = Guid.NewGuid()
                  name = name
                  path = path
                  createdAt = DateTime.Now })

    let shuffle (original: _ list) =
        let rng = Random.Shared
        let arr = Array.copy (original |> List.toArray)
        let max = (arr.Length - 1)

        let randomSwap (arr: _ []) i =
            let pos = rng.Next(max)
            let tmp = arr.[pos]
            arr.[pos] <- arr.[i]
            arr.[i] <- tmp
            arr

        [| 0..max |]
        |> Array.fold randomSwap arr
        |> Array.toList