namespace Raznor.Desktop


module Player =
    open Elmish
    open LibVLCSharp.Shared
    open Avalonia.Controls
    open Avalonia.FuncUI.DSL
    open Avalonia.Layout
    open Raznor.Core


    type State =
        { player: MediaPlayer }

    type Msg =
        | Play of Types.SongRecord

    let init player =
        { player = player }

    let update msg state =
        match msg with
        | Play song ->
            use media = PlayerLib.getMediaFromlocal song.path
            state.player.Play media |> ignore
            state, Cmd.none


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
              DockPanel.horizontalAlignment HorizontalAlignment.Center
              DockPanel.children
                  [ mediaButtons state dispatch
                    progressBar state dispatch
                    mediaMenu state dispatch ] ]
