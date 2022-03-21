namespace Raznor

module Shell =
    open System
    open Elmish
    open Avalonia
    open Avalonia.Controls
    open Avalonia.Input
    open Avalonia.Layout
    open Avalonia.Media.Imaging
    open Avalonia.FuncUI.Elmish
    open Avalonia.FuncUI.Hosts
    open Avalonia.FuncUI.DSL
    open LibVLCSharp.Shared
    open Raznor.Core
    open Raznor.Core.Components

    type State =
        { window: HostWindow
          title: string
          discoverer: RendererDiscoverer
          searching: bool
          playerState: Player.State
          playlistState: Playlist.State }

    type Msg =
        | PlayerMsg of Player.Msg
        | PlaylistMsg of Playlist.Msg
        | SetTitle of string
        | OpenFiles
        | OpenFolder
        | AfterSelectFolder of string
        | AfterSelectFiles of string array
        | ToggleFindRenderer
        | Ended
        | TimeChanged of int64
        | AddRenderer of RendererItem
        | RemoveRenderer of RendererItem

    module Subs =
        let ended (player: MediaPlayer) =
            let sub dispatch =
                player.EndReached.Subscribe(fun _ -> dispatch Ended)
                |> ignore

            Cmd.ofSub sub

        let timechanged (player: MediaPlayer) =
            let sub dispatch =
                player.TimeChanged.Subscribe(fun args -> dispatch (TimeChanged args.Time))
                |> ignore

            Cmd.ofSub sub

        let rendererUpdate (discoverer: RendererDiscoverer) =
            let sub dispatch =
                discoverer.ItemAdded.Subscribe(fun args -> dispatch (AddRenderer args.RendererItem))
                |> ignore

                discoverer.ItemDeleted.Subscribe(fun args -> dispatch (RemoveRenderer args.RendererItem))
                |> ignore

            Cmd.ofSub sub

    let init (window: HostWindow) (player: MediaPlayer) (discoverer: RendererDiscoverer) =
        { window = window
          title = "Raznor F# :)"
          discoverer = discoverer
          searching = false
          playerState = Player.init player
          playlistState = Playlist.init }

    let private handlePlaylistExternal (msg: Playlist.ExternalMsg option) =
        match msg with
        | None -> Cmd.none
        | Some msg ->
            match msg with
            | Playlist.ExternalMsg.PlaySong (int, song) ->
                Cmd.batch [
                    Cmd.ofMsg (PlayerMsg(Player.Msg.Play song))
                    Cmd.ofMsg (SetTitle song.name)
                ]

    let private handlePlayerExternal (msg: Player.ExternalMsg option) =
        match msg with
        | None -> Cmd.none
        | Some msg ->
            match msg with
            | Player.ExternalMsg.Play -> Cmd.ofMsg (PlaylistMsg(Playlist.Msg.GetAny))
            | Player.ExternalMsg.Next -> Cmd.ofMsg (PlaylistMsg(Playlist.Msg.GetNext))
            | Player.ExternalMsg.Previous -> Cmd.ofMsg (PlaylistMsg(Playlist.Msg.GetPrevious))
            | Player.ExternalMsg.Shuffle -> Cmd.ofMsg (PlaylistMsg(Playlist.Msg.Shuffle))
            | Player.ExternalMsg.SetLoopState loopstate -> Cmd.ofMsg (PlaylistMsg(Playlist.Msg.SetLoopState loopstate))

    let update (msg: Msg) (state: State) =
        match msg with
        | PlayerMsg playermsg ->
            let s, cmd, external = Player.update playermsg state.playerState

            let handled = handlePlayerExternal external
            let mapped = Cmd.map PlayerMsg cmd
            let batch = Cmd.batch [ mapped; handled ]
            { state with playerState = s }, batch
        | PlaylistMsg playlistmsg ->
            let s, cmd, external = Playlist.update playlistmsg state.playlistState

            let mapped = Cmd.map PlaylistMsg cmd
            let handled = handlePlaylistExternal external
            let batch = Cmd.batch [ mapped; handled ]
            { state with playlistState = s }, batch
        | SetTitle title ->
            state.window.Title <- title
            { state with title = title }, Cmd.none
        | OpenFiles ->
            let dialog = AvaloniaDialogs.getMusicFilesDialog None

            let showDialog window =
                dialog.ShowAsync(window) |> Async.AwaitTask

            state, Cmd.OfAsync.perform showDialog state.window AfterSelectFiles
        | OpenFolder ->
            let dialog = AvaloniaDialogs.getFolderDialog

            let showDialog window =
                dialog.ShowAsync(window) |> Async.AwaitTask

            state, Cmd.OfAsync.perform showDialog state.window AfterSelectFolder
        | AfterSelectFolder path ->
            let songs = Songs.populateFromDirectory path |> Array.toList

            state, Cmd.map PlaylistMsg (Cmd.ofMsg (Playlist.Msg.AddFiles songs))
        | AfterSelectFiles paths ->
            let paths =
                if isNull paths then
                    Array.empty
                else
                    paths

            let songs = Songs.populateSongs paths |> Array.toList

            state, Cmd.map PlaylistMsg (Cmd.ofMsg (Playlist.Msg.AddFiles songs))
        | ToggleFindRenderer ->
            if state.searching then
                state.discoverer.Stop()
            else
                state.discoverer.Start() |> ignore

            { state with searching = not state.searching }, Cmd.none
        | Ended -> state, Cmd.map PlaylistMsg (Cmd.ofMsg (Playlist.Msg.GetNext))
        | TimeChanged time -> state, Cmd.map PlayerMsg (Cmd.ofMsg (Player.Msg.SetPos time))
        | AddRenderer rendererItem -> state, Cmd.map PlayerMsg (Cmd.ofMsg (Player.Msg.AddRenderer rendererItem))
        | RemoveRenderer rendererItem -> state, Cmd.map PlayerMsg (Cmd.ofMsg (Player.Msg.RemoveRenderer rendererItem))

    let view (state: State) (dispatch: Msg -> unit) =
        DockPanel.create [
            DockPanel.verticalAlignment VerticalAlignment.Stretch
            DockPanel.horizontalAlignment HorizontalAlignment.Stretch
            DockPanel.lastChildFill false
            DockPanel.children [
                Shell.Menubar(
                    state.searching,
                    (fun _ -> dispatch OpenFiles),
                    (fun _ -> dispatch OpenFolder),
                    (fun _ -> dispatch ToggleFindRenderer)
                )
                Playlist.view state.playlistState (PlaylistMsg >> dispatch)
                Player.view state.playerState (PlayerMsg >> dispatch)
            ]
        ]

    type ShellWindow() as this =
        inherit HostWindow()

        do
            base.Title <- "Raznor App"
            base.Width <- 800.0
            base.Height <- 600.0
            base.MinWidth <- 617.0
            base.MinHeight <- 624.0
            base.Icon <- WindowIcon(Bitmap.Create "avares://Raznor.MVU/PEZMUSIC.png")
            let player = PlayerLib.GetPlayer
            let discoverer = PlayerLib.getDiscoverer

            let programInit (window, player, discoverer) = init window player discoverer, Cmd.none

            Program.mkProgram programInit update view
            |> Program.withHost this
            |> Program.withSubscription (fun _ -> Subs.ended player.Value)
            |> Program.withSubscription (fun _ -> Subs.timechanged player.Value)
            |> Program.withSubscription (fun _ -> Subs.rendererUpdate discoverer)
            |> Program.withConsoleTrace
            |> Program.runWith (this, player.Value, discoverer)
#if DEBUG
            this.AttachDevTools(KeyGesture(Key.F12))
#endif