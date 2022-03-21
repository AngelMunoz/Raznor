namespace Raznor.Core

module PlayerLib =
    open LibVLCSharp.Shared

    let private libvlc = lazy (new LibVLC("--verbose=2"))

    let getMediaFromlocal (source: string) =
        new Media(libvlc.Value, source, FromType.FromPath)

    let GetPlayer = lazy (new MediaPlayer(libvlc.Value))

    let getDiscoverer =
        let description = libvlc.Value.RendererList |> Array.head
        new RendererDiscoverer(libvlc.Value, description.Name)