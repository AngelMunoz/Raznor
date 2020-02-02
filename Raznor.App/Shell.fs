namespace Raznor.App

open System

module Shell =
    open Elmish
    open Avalonia.Controls
    open Avalonia.Layout
    open Avalonia.FuncUI.Elmish
    open Avalonia.FuncUI.Components.Hosts
    open Avalonia.FuncUI.DSL
    open Raznor.App
    open Raznor.Core.PlayerLib

    type State =
        { window: HostWindow
          title: string
          playerState: Player.State
          contentState: Content.State
          playlistState: Playlist.State }

    type Msg =
        | PlayerMsg of Player.Msg
        | ContentMsg of Content.Msg
        | PlaylistMsg of Playlist.Msg
        | SetTitle of string

    let init (window: HostWindow) =
        let title = "Raznor App F#"
        window.Title <- title
        { window = window
          title = title
          playerState = Player.init
          contentState = Content.init
          playlistState = Playlist.init }


    let private handlePlaylistExternal (msg: Playlist.ExternalMsg option) =
        match msg with
        | None -> Cmd.none
        | Some msg ->
            match msg with
            | Playlist.ExternalMsg.SetPlaylist playlist -> Cmd.ofMsg (PlayerMsg(Player.Msg.SetPlaylist(playlist)))
            | Playlist.ExternalMsg.PlaySong(int, song) -> Cmd.ofMsg (PlayerMsg(Player.Msg.PlaySongAt(int, song)))
            | Playlist.ExternalMsg.RemoveSong(int, song) -> Cmd.ofMsg (PlayerMsg(Player.Msg.DeleteSongAt(int, song)))

    let handleContentExternal (msg: Content.ExternalMsg option) =
        match msg with
        | None -> Cmd.none
        | Some msg ->
            match msg with
            | Content.ExternalMsg.AddToPlayList songs -> Cmd.ofMsg (PlaylistMsg(Playlist.Msg.AddFiles(songs)))

    let update (msg: Msg) (state: State) =
        match msg with
        | PlayerMsg playermsg ->
            let s, cmd = Player.update playermsg state.playerState
            { state with playerState = s }, Cmd.map PlayerMsg cmd
        | ContentMsg contentmsg ->
            let s, cmd, external = Content.update contentmsg state.contentState
            let mapped = Cmd.map ContentMsg cmd
            let handled = handleContentExternal external
            let batch = Cmd.batch [ mapped; handled ]
            { state with contentState = s }, batch
        | PlaylistMsg playlistmsg ->
            let s, cmd, external = Playlist.update playlistmsg state.playlistState
            let mapped = Cmd.map PlaylistMsg cmd
            let handled = handlePlaylistExternal external
            let batch = Cmd.batch [ mapped; handled ]
            { state with playlistState = s }, batch
        | SetTitle title -> { state with title = title }, Cmd.none

    let menuBar state dispatch = Menu.create [ Menu.dock Dock.Top ]

    let view (state: State) (dispatch: Msg -> unit) =
        DockPanel.create
            [ DockPanel.verticalAlignment VerticalAlignment.Stretch
              DockPanel.horizontalAlignment HorizontalAlignment.Stretch
              DockPanel.lastChildFill false
              DockPanel.children
                  [ menuBar state dispatch
                    Content.view state.contentState (ContentMsg >> dispatch)
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


            Program.mkProgram programInit update view
            |> Program.withHost this
            |> Program.withConsoleTrace
            |> Program.runWith this
