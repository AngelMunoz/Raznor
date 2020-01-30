namespace Raznor.Core

module PlayerLib =
    open LibVLCSharp.Shared

    let getMediaFromlocal (source: string) =
        use libvlc = new LibVLC()
        new Media(libvlc, source, FromType.FromPath)

    let getMediaFromNetwork (source: string) =
        use libvlc = new LibVLC()
        new Media(libvlc, source, FromType.FromLocation)

    let getLocalPlaylist files =
        use libvlc = new LibVLC()
        let medialist = new MediaList(libvlc)

        let allAdded =
            files
            |> Seq.map (fun file -> getMediaFromlocal file |> medialist.AddMedia)
            |> Seq.forall (fun x -> x)

        medialist

    let getNetworkPlaylist files =
        use libvlc = new LibVLC()
        let medialist = new MediaList(libvlc)
        files
        |> Seq.map getMediaFromNetwork
        |> ignore
        medialist

    let getEmptyPlayer =
        use libvlc = new LibVLC()
        new MediaPlayer(libvlc)
