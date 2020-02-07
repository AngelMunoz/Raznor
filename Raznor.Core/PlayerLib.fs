namespace Raznor.Core

module PlayerLib =
    open LibVLCSharp.Shared

    let getMediaFromlocal (source: string) =
        use libvlc = new LibVLC()
        new Media(libvlc, source, FromType.FromPath)

    let getEmptyPlayer =
        use libvlc = new LibVLC()
        new MediaPlayer(libvlc)
