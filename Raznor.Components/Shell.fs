namespace Raznor.Components

open System

open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media.Imaging

open Avalonia.FuncUI
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.DSL

open LibVLCSharp.Shared
open Raznor.Core
open Raznor.Core.Components
open Raznor.Core.Types

module Shell =
    let private playSong (player: Lazy<MediaPlayer>) (song: SongRecord option) =
        let player = player.Value

        match song with
        | Some song ->
            use media = PlayerLib.getMediaFromlocal song.path

            try
                player.Play media |> ignore
            with
            | ex ->
                eprintfn "%O" ex
                player.Play media |> ignore
        | None -> ()

    let private onFilesSelected (window: Window) (dialog: OpenFileDialog) (playlist: IWritable<_>) =
        task {
            let! paths = dialog.ShowAsync window

            let paths =
                if isNull paths then
                    Array.empty
                else
                    paths

            let songs = Songs.populateSongs paths |> Array.toList
            playlist.Set songs
        }
        |> Async.AwaitTask
        |> Async.Start

    let private onDirectorySelected (window: Window) (dialog: OpenFolderDialog) (playlist: IWritable<_>) =
        task {
            let! path = dialog.ShowAsync window
            if isNull path then return ()
            let songs = Songs.populateFromDirectory path |> Array.toList
            playlist.Set songs
        }
        |> Async.AwaitTask
        |> Async.Start

    let private toggleChromecastSearch (searching: IWritable<bool>) (discoverer: RendererDiscoverer) =
        if searching.Current then
            discoverer.Stop()
            searching.Set false
        else
            discoverer.Start() |> searching.Set

    let private onTimeChanged
        (player: MediaPlayer)
        (state: IWritable<PlayerState>)
        (args: MediaPlayerTimeChangedEventArgs)
        =
        let length = player.Length
        let pos = (args.Time * 100L / length) |> int

        { state.Current with
            sliderPos = pos
            length = length
            state = Play }
        |> state.Set

    let private onRendererAdded
        (state: IWritable<Map<string, RendererItem>>)
        (args: RendererDiscovererItemAddedEventArgs)
        =
        state.Current
        |> Map.add args.RendererItem.Name args.RendererItem
        |> state.Set

    let private onRendererRemoved
        (state: IWritable<Map<string, RendererItem>>)
        (args: RendererDiscovererItemDeletedEventArgs)
        =
        state.Current
        |> Map.remove args.RendererItem.Name
        |> state.Set

    let private tryGetNewSong
        (loopState: LoopState)
        (playDirection: PlayDirection)
        (playlist: SongRecord list)
        (song: SongRecord option)
        =
        let index =
            match song with
            | Some song ->
                playlist
                |> List.tryFindIndex (fun s -> s = song)
                |> Option.defaultValue -1
            | None -> -1

        let index =
            match playDirection with
            | Next -> index + 1
            | Previous -> index - 1
            | Direct song ->
                playlist
                |> List.tryFindIndex (fun s -> s = song)
                |> Option.defaultValue -1

        match loopState with
        | LoopState.All -> playlist |> List.tryItem index
        | LoopState.Single -> song
        | LoopState.Off ->
            if index >= playlist.Length then
                None
            else
                playlist |> List.tryItem index

    let private onSongSelected
        (player: Lazy<MediaPlayer>)
        (playerState: IWritable<PlayerState>)
        playlist
        loopState
        song
        =
        let song = tryGetNewSong loopState (PlayDirection.Direct song) playlist (Some song)

        { playerState.Current with song = song }
        |> playerState.Set

        playSong player song


    let private onShuffleRequested (playlist: IWritable<SongRecord list>) () =
        playlist.Current |> Songs.shuffle |> playlist.Set

    let private onLoopStateChanged (playerState: IWritable<PlayerState>) (loopState: LoopState) =
        { playerState.Current with loopState = loopState }
        |> playerState.Set

    let private onRequestPlay
        (player: Lazy<MediaPlayer>)
        (playlist: IReadable<SongRecord list>)
        (playerState: IWritable<PlayerState>)
        (playDirection: PlayDirection)
        =
        let song =
            match playDirection with
            | Direct requested -> Some requested
            | _ -> tryGetNewSong playerState.Current.loopState playDirection playlist.Current playerState.Current.song


        { playerState.Current with
            song = song
            state = Play }
        |> playerState.Set

        playSong player song

    let private onPlayStateChange
        (player: Lazy<MediaPlayer>)
        (playerState: IWritable<PlayerState>)
        (playlist: IReadable<SongRecord list>)
        (playState: PlayState)
        =
        match playerState.Current.song, playState with
        | None, Play ->
            let song = tryGetNewSong playerState.Current.loopState Next playlist.Current None

            { playerState.Current with song = song }
            |> playerState.Set

            playSong player song
        | Some _, Play -> player.Value.Play() |> ignore

        | _, Pause -> player.Value.Pause()
        | _, Stop -> player.Value.Stop()

        { playerState.Current with state = playState }
        |> playerState.Set

    let private getIndex (playlist: SongRecord list) (song: SongRecord option) =
        match song with
        | None -> -1
        | Some song ->
            playlist
            |> List.tryFindIndex (fun s -> s = song)
            |> Option.defaultValue -1

    let private onMediaEnded
        (player: Lazy<MediaPlayer>)
        (playlist: IWritable<_>)
        (currentSong: IReadable<_>)
        (playerState: IWritable<_>)
        _
        =
        async {
            player.Value.Media.Dispose()
            let index = getIndex playlist.Current currentSong.Current
            // HACK: don't immediately play a new song wait a few ms so VLC doesn't hang and crash the app
            do! Async.Sleep(200)

            let index =
                match playerState.Current.loopState with
                | All ->
                    if index + 1 >= playlist.Current.Length then
                        0
                    else
                        index
                | Off ->
                    if index + 1 >= playlist.Current.Length then
                        -1
                    else
                        index
                | Single -> index

            match playlist.Current |> List.tryItem index with
            | Some song -> onRequestPlay player playlist playerState (Direct song)
            | None -> PlayerState.Empty |> playerState.Set
        }
        |> Async.Start

    /// This is our main view, as with any function, we can request the parameters we need
    /// this is a good place to request resources from the external world, like window we're running in.
    let View (window: Window, player: Lazy<MediaPlayer>, discoverer: RendererDiscoverer) =
        let filesDialog = AvaloniaDialogs.getMusicFilesDialog None
        let directoryDialog = AvaloniaDialogs.getFolderDialog
        // Components are the new kids in town, they are inspired by web models and are code first
        // When you use "Component(fun ctx -> ...)" instead of "Component.create(id, fun ctx -> ...)"
        // you're creating an instance of a component which is compatible with Avalonia Component's "Content"
        // so you could be able to embed these kind of components in other Avalonia Components, not just Window.
        // "Component.create(id, fun ctx -> ...)" this is a FuncUI.DSL version for the components so you can create
        // multiple components and use them inside the usual Avalonia.FuncUI DSL
        Component (fun ctx ->
            // most likely when you work with components
            // you will want to keep the state of what the function is doing
            // for that we use ctx.useState
            let searching = ctx.useState false
            let playlist = ctx.useState List.empty
            let playerState = ctx.useState PlayerState.Empty
            let rendererMap = ctx.useState Map.empty

            // When you want yo extract properties from another state record
            // or do some kind of computed value based on what is on the state
            // you can use State.readMap which will take your readable/writable values
            // and produce a new readable value from the result of the mapping function

            let sliderPosition =
                playerState
                |> State.readMap (fun state -> state.sliderPos)

            let isPlaying =
                playerState
                |> State.readMap (fun state -> state.state = Play)

            let currentSong =
                playerState
                |> State.readMap (fun state -> state.song)

            let loopState =
                playerState
                |> State.readMap (fun state -> state.loopState)

            // When you want to do side effects like subscriptions, http requests
            // and other similar things which need to happen either after the initial render or
            // when a Writable/Readable value changes or on every render, they are good do go here
            ctx.useEffect (
                (fun _ ->
                    let subs =
                        [ player.Value.EndReached
                          |> Observable.subscribe (onMediaEnded player playlist currentSong playerState)
                          player.Value.TimeChanged
                          |> Observable.subscribe (onTimeChanged player.Value playerState)
                          discoverer.ItemAdded
                          |> Observable.subscribe (onRendererAdded rendererMap)
                          discoverer.ItemDeleted
                          |> Observable.subscribe (onRendererRemoved rendererMap) ]
                    // effects can return a disposable so you can cleanup things when the component is
                    // getting disposed
                    // you can also return unit for simpler effects that don't require a cleanup value
                    { new IDisposable with
                        member _.Dispose() : unit =
                            for sub in subs do
                                sub.Dispose() }),
                [ EffectTrigger.AfterInit ]
            )
            // from here and on, it is you'r usual Avalonia.FuncUI
            DockPanel.create [
                DockPanel.verticalAlignment VerticalAlignment.Stretch
                DockPanel.horizontalAlignment HorizontalAlignment.Stretch
                DockPanel.lastChildFill false
                DockPanel.children [
                    Shell.Menubar(
                        // if you need the "current" value of a readable/writable
                        // you can use the ".Current" property to access it :)
                        searching.Current,
                        (fun _ -> onFilesSelected window filesDialog playlist),
                        (fun _ -> onDirectorySelected window directoryDialog playlist),
                        (fun _ -> toggleChromecastSearch searching discoverer)
                    )
                    match playlist.Current with
                    | [] -> Playlist.EmptySongList()
                    | songs ->
                        Playlist.SongList(
                            getIndex playlist.Current currentSong.Current,
                            songs,
                            onSongSelected player playerState playlist.Current loopState.Current
                        )

                        Player.Player [
                            Player.ProgressBar sliderPosition.Current
                            Player.MediaButtons(
                                isPlaying.Current,
                                loopState.Current,
                                onPlayStateChange player playerState playlist,
                                onRequestPlay player playlist playerState,
                                onLoopStateChanged playerState,
                                onShuffleRequested playlist
                            )
                        ]
                ]
            ])


type ShellWindow() as this =
    inherit HostWindow()

    do
        base.Title <- "Raznor App"
        base.Width <- 800.0
        base.Height <- 600.0
        base.MinWidth <- 617.0
        base.MinHeight <- 624.0
        base.Icon <- WindowIcon(Bitmap.Create "avares://Raznor.Components/PEZMUSIC.png")
        this.Content <- Shell.View(this, PlayerLib.GetPlayer, PlayerLib.getDiscoverer)
#if DEBUG
        this.AttachDevTools(KeyGesture(Key.F12))
#endif