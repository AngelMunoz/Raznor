namespace Raznor.App


module Player =
    open Elmish
    open LibVLCSharp.Shared
    open Avalonia.Controls
    open Avalonia.FuncUI.DSL
    open Avalonia.Layout
    open Raznor.Core


    type State =
        { playlist: MediaList option
          player: MediaPlayer option
          playlistSize: int
          next: int }

    type Msg =
        | SetMediaList of MediaList
        | Play of int
        | PlaySingle of Types.SongRecord

    let init =
        { playlist = None
          player = None
          playlistSize = 0
          next = 0 }

    let update msg state =
        match msg with
        | SetMediaList playlist ->
            let player =
                match state.player with
                | None -> Some PlayerLib.getEmptyPlayer
                | player -> player

            match state.playlist with
            | Some pl -> pl.Dispose()
            | None -> ()
            { state with
                  player = player
                  playlist = Some playlist
                  playlistSize = playlist.Count
                  next = 0 }, Cmd.ofMsg (Play state.next)
        | Play next ->
            match state.player, state.playlist with
            | (Some player, Some playlist) ->
                let item = playlist.Item next
                player.Play item |> ignore
                { state with next = next + 1 }, Cmd.none
            | (_, _) -> state, Cmd.none
        | PlaySingle song ->
            let player =
                match state.player with
                | Some player -> player
                | None -> PlayerLib.getEmptyPlayer
            match state.playlist with
            | Some pl -> pl.Dispose()
            | None -> ()
            use media = PlayerLib.getMediaFromlocal song.path
            player.Play(media) |> ignore
            { state with player = Some player }, Cmd.none


    let private mediaButtons (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.verticalAlignment VerticalAlignment.Bottom
              StackPanel.horizontalAlignment HorizontalAlignment.Left
              StackPanel.children [ TextBlock.create [ TextBlock.text "Media Buttons" ] ] ]

    let private progressBar (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.verticalAlignment VerticalAlignment.Bottom
              StackPanel.horizontalAlignment HorizontalAlignment.Center
              StackPanel.children [ TextBlock.create [ TextBlock.text "Progress Bar" ] ] ]

    let private mediaMenu (state: State) (dispatch: Msg -> unit) =
        Menu.create
            [ Menu.classes [ "mediamenu" ]
              Menu.dock Dock.Bottom
              Menu.verticalAlignment VerticalAlignment.Bottom
              Menu.horizontalAlignment HorizontalAlignment.Right
              Menu.viewItems [ MenuItem.create [ MenuItem.header "Some Settings" ] ] ]

    let view (state: State) (dispatch: Msg -> unit) =
        DockPanel.create
            [ DockPanel.classes [ "mediabar" ]
              DockPanel.dock Dock.Bottom
              DockPanel.children
                  [ mediaButtons state dispatch
                    progressBar state dispatch
                    mediaMenu state dispatch ] ]
