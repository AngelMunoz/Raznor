namespace Raznor.Core

module PlayerLib =
    open LibVLCSharp.Shared

    let getMediaFromlocal (source: string) =
        use libvlc = new LibVLC()
        new Media(libvlc, source, FromType.FromPath)

    let getEmptyPlayer =
        use libvlc = new LibVLC()
        new MediaPlayer(libvlc)

    let getDiscoverer =
        let libvlc = new LibVLC()
        let description = libvlc.RendererList |> Array.head
        new RendererDiscoverer(libvlc, description.Name)
