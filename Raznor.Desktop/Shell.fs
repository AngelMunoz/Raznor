namespace Raznor.Desktop


module Shell =
    open System
    open Elmish
    open Avalonia
    open Avalonia.Controls
    open Avalonia.Input
    open Avalonia.Layout
    open Avalonia.Threading
    open Avalonia.FuncUI.Elmish
    open Avalonia.FuncUI.Components.Hosts
    open Avalonia.FuncUI.DSL
    open LibVLCSharp.Shared
    open Raznor.Desktop
    open Raznor.Core

    type State =
        { window: HostWindow
          title: string
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
        (* Handle Media Player Events *)
        | Playing
        | Paused
        | Stopped
        | Ended

    module Subs = 
        let playing (player: MediaPlayer) =
            let sub dispatch = 
                player.Playing.Subscribe(fun _ -> dispatch Playing) |> ignore
                ()
            Cmd.ofSub sub
        
        let paused (player: MediaPlayer) =
            let sub dispatch = 
                player.Paused.Subscribe(fun _ -> dispatch Paused) |> ignore
                ()
            Cmd.ofSub sub

        let stoped (player: MediaPlayer) =
            let sub dispatch = 
                player.Stopped.Subscribe(fun _ -> dispatch Stopped) |> ignore
                ()
            Cmd.ofSub sub

        let ended (player: MediaPlayer) =
            let sub dispatch = 
                player.EndReached.Subscribe(fun _ -> dispatch Ended) |> ignore
                ()
            Cmd.ofSub sub

    let init (window: HostWindow) (player: MediaPlayer) =
        { window = window
          title = "Raznor F# :)"
          playerState = Player.init player
          playlistState = Playlist.init }


    let private handlePlaylistExternal (msg: Playlist.ExternalMsg option) =
        match msg with
        | None -> Cmd.none
        | Some msg ->
            match msg with
            | Playlist.ExternalMsg.SetPlaylist playlist -> Cmd.none
            | Playlist.ExternalMsg.RemoveSong(int, song) -> Cmd.none
            | Playlist.ExternalMsg.PlaySong(int, song) -> Cmd.ofMsg (PlayerMsg(Player.Msg.Play song))

    let update (msg: Msg) (state: State) =
        match msg with
        | PlayerMsg playermsg ->
            let s, cmd = Player.update playermsg state.playerState
            { state with playerState = s }, Cmd.map PlayerMsg cmd
        | PlaylistMsg playlistmsg ->
            let s, cmd, external = Playlist.update playlistmsg state.playlistState
            let mapped = Cmd.map PlaylistMsg cmd
            let handled = handlePlaylistExternal external
            let batch = Cmd.batch [ mapped; handled ]
            { state with playlistState = s }, batch
        | SetTitle title -> { state with title = title }, Cmd.none
        | OpenFiles ->
            let dialog = Dialogs.getMusicFilesDialog None
            let showDialog window = dialog.ShowAsync(window) |> Async.AwaitTask
            state, Cmd.OfAsync.perform showDialog state.window AfterSelectFiles
        | OpenFolder ->
            let dialog = Dialogs.getFolderDialog
            let showDialog window = dialog.ShowAsync(window) |> Async.AwaitTask
            state, Cmd.OfAsync.perform showDialog state.window AfterSelectFolder
        | AfterSelectFolder path ->
            let songs = MusicCollections.populateFromDirectory path |> Array.toList
            state, Cmd.map PlaylistMsg (Cmd.ofMsg (Playlist.Msg.AddFiles songs))
        | AfterSelectFiles paths ->
            let songs = MusicCollections.populateSongs paths |> Array.toList

            state, Cmd.map PlaylistMsg (Cmd.ofMsg (Playlist.Msg.AddFiles songs))
        (* The follwing messages are fired from the player's subscriptions 
           I feel these are can help to handle updates accross the whole application
           There are a lot more of events the Player Emits, but for the moment
           we'll work with these *)
        | Playing ->
            state, Cmd.none
        | Paused  ->
            state, Cmd.none
        | Stopped ->
            state, Cmd.none
        | Ended   ->
            state, Cmd.none

    let menuBar state dispatch =
        Menu.create
            [ Menu.dock Dock.Top
              Menu.viewItems
                  [ MenuItem.create
                      [ MenuItem.header "Files"
                        MenuItem.viewItems
                            [ MenuItem.create
                                [ MenuItem.header "Select Files"
                                  MenuItem.icon (Image.FromString "avares://Raznor.Desktop/Assets/Icons/file-multiple-dark.png")
                                  MenuItem.onClick (fun _ -> dispatch OpenFiles) ]
                              MenuItem.create
                                  [ MenuItem.header "Select Folder"
                                    MenuItem.icon (Image.FromString "avares://Raznor.Desktop/Assets/Icons/folder-music-dark.png")
                                    MenuItem.onClick (fun _ -> dispatch OpenFolder) ] ] ] ] ]

    let view (state: State) (dispatch: Msg -> unit) =
        DockPanel.create
            [ DockPanel.verticalAlignment VerticalAlignment.Stretch
              DockPanel.horizontalAlignment HorizontalAlignment.Stretch
              DockPanel.lastChildFill false
              DockPanel.children
                  [ menuBar state dispatch
                    Playlist.view state.playlistState (PlaylistMsg >> dispatch)
                    Player.view state.playerState (PlayerMsg >> dispatch) ] ]

    type ShellWindow() as this =
        inherit HostWindow()
        do
            base.Title <- "Raznor App"
            base.Width <- 800.0
            base.Height <- 600.0
            base.MinWidth <- 526.0
            base.MinHeight <- 526.0

            //this.VisualRoot.VisualRoot.Renderer.DrawFps <- true
            //this.VisualRoot.VisualRoot.Renderer.DrawDirtyRects <- true
            let player = PlayerLib.getEmptyPlayer
            let programInit (window, player) = init window player, Cmd.none

            this.AttachDevTools(KeyGesture(Key.F12))
            let syncDispatch (dispatch: Dispatch<'msg>): Dispatch<'msg> =
                match Dispatcher.UIThread.CheckAccess() with
                | true -> fun msg -> Dispatcher.UIThread.Post(fun () -> dispatch msg)
                | false -> fun msg -> dispatch msg

            Program.mkProgram programInit update view
            |> Program.withHost this
            |> Program.withSyncDispatch syncDispatch
            |> Program.withSubscription (fun _ -> Subs.playing player)
            |> Program.withSubscription (fun _ -> Subs.paused player)
            |> Program.withSubscription (fun _ -> Subs.stoped player)
            |> Program.withSubscription (fun _ -> Subs.ended player)
            |> Program.withConsoleTrace
            |> Program.runWith (this, player)

