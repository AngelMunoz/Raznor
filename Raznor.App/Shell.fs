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
          contentState: Content.State }

    type Msg =
        | PlayerMsg of Player.Msg
        | ContentMsg of Content.Msg
        | SetTitle of string
        | LoadPlaylist

    let init (window: HostWindow) =
        let title = "Raznor App F#"
        window.Title <- title
        { window = window
          title = title
          playerState = Player.init
          contentState = Content.init }

    let private getPlayList (window: HostWindow) =
        let musicpath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
        let dialog = OpenFileDialog()
        dialog.AllowMultiple <- false
        dialog.Directory <- musicpath
        async { return! dialog.ShowAsync(window) |> Async.AwaitTask }

    let update (msg: Msg) (state: State) =
        match msg with
        | PlayerMsg playermsg ->
            let s, cmd = Player.update playermsg state.playerState
            { state with playerState = s }, Cmd.map PlayerMsg cmd
        | ContentMsg contentmsg ->
            let s, cmd, external = Content.update contentmsg state.contentState
            let mapped = Cmd.map ContentMsg cmd

            let batch =
                match external with
                | Some externalmsg ->
                    match externalmsg with
                    | Content.ExternalMsg.PlaySong song ->
                        Cmd.batch
                            [ mapped
                              Cmd.ofMsg (PlayerMsg(Player.Msg.PlaySingle song)) ]
                | None -> mapped
            { state with contentState = s }, batch
        | SetTitle title -> { state with title = title }, Cmd.none
        | LoadPlaylist ->
            let action window =
                async {
                    let! paths = getPlayList window
                    let playlist = getLocalPlaylist paths
                    return playlist
                }

            let ofSuccess media = PlayerMsg(Player.Msg.SetMediaList media)
            let ps = { state.playerState with next = 0 }
            { state with playerState = ps }, Cmd.OfAsync.perform action state.window ofSuccess

    let menuBar state dispatch =
        Menu.create
            [ Menu.dock Dock.Top
              Menu.viewItems
                  [ MenuItem.create
                      [ MenuItem.header "Load Single File"
                        MenuItem.onClick (fun _ -> dispatch LoadPlaylist) ] ] ]

    let view (state: State) (dispatch: Msg -> unit) =
        DockPanel.create
            [ DockPanel.verticalAlignment VerticalAlignment.Stretch
              DockPanel.horizontalAlignment HorizontalAlignment.Stretch
              DockPanel.children
                  [ menuBar state dispatch
                    Content.view state.contentState (ContentMsg >> dispatch)
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
