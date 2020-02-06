namespace Raznor.Desktop

open Avalonia.Input



module Shell =
    open System
    open Elmish
    open Avalonia
    open Avalonia.Controls
    open Avalonia.Layout
    open Avalonia.Media
    open Avalonia.Media.Imaging
    open Avalonia.FuncUI.Elmish
    open Avalonia.FuncUI.Components.Hosts
    open Avalonia.FuncUI.DSL
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

    let init (window: HostWindow) =
        let title = "Raznor App F#"
        window.Title <- title
        { window = window
          title = title
          playerState = Player.init
          playlistState = Playlist.init }


    let private handlePlaylistExternal (msg: Playlist.ExternalMsg option) =
        match msg with
        | None -> Cmd.none
        | Some msg ->
            match msg with
            | Playlist.ExternalMsg.SetPlaylist playlist -> Cmd.ofMsg (PlayerMsg(Player.Msg.SetPlaylist(playlist)))
            | Playlist.ExternalMsg.PlaySong(int, song) -> Cmd.ofMsg (PlayerMsg(Player.Msg.PlaySongAt(int, song)))
            | Playlist.ExternalMsg.RemoveSong(int, song) -> Cmd.ofMsg (PlayerMsg(Player.Msg.DeleteSongAt(int, song)))

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
            let programInit (window: ShellWindow) = init window, Cmd.none

            this.AttachDevTools(KeyGesture(Key.F12))

            Program.mkProgram programInit update view
            |> Program.withHost this
            |> Program.withConsoleTrace
            |> Program.runWith this
