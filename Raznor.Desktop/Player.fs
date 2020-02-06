namespace Raznor.Desktop


module Player =
    open Elmish
    open LibVLCSharp.Shared
    open Avalonia.Controls
    open Avalonia.Media
    open Avalonia.Media.Imaging
    open Avalonia.Layout
    open Avalonia.FuncUI.DSL
    open Raznor.Core


    type State =
        { player: MediaPlayer
          length: int64
          sliderPos: int }

    type ExternalMsg =
        | Next
        | Previous
        | Play

    type Msg =
        | Play of Types.SongRecord
        | Seek of double
        | SetPos of int64
        | SetLength of int64
        | Previous
        | Pause
        | Stop
        | PlayInternal
        | Next

    let init player =
        { player = player
          length = 0L
          sliderPos = 0 }

    let update msg state =
        match msg with
        | Play song ->
            use media = PlayerLib.getMediaFromlocal song.path
            state.player.Play media |> ignore
            state, Cmd.ofMsg (SetLength state.player.Length), None
        | Seek position ->
            let time = (position |> int64) * state.player.Length / 100L
            (* find a way to differentiate from user action vs player event *)
            state, Cmd.none, None
        | SetLength length -> { state with length = length }, Cmd.none, None
        | SetPos position ->
            let pos = (position * 100L / state.player.Length) |> int
            { state with sliderPos = pos }, Cmd.none, None
        | Previous ->
            state.player.PreviousChapter()
            state, Cmd.none, Some ExternalMsg.Previous
        | Next ->
            state.player.NextChapter()
            state, Cmd.none, Some ExternalMsg.Next
        | Pause ->
            state.player.Pause()
            state, Cmd.none, None
        | Stop ->
            state.player.Stop()
            state, Cmd.none, None
        | PlayInternal -> state, Cmd.none, Some ExternalMsg.Play




    let private mediaButtons (state: State) (dispatch: Msg -> unit) =
        let backwards = "avares://Raznor.Desktop/Assets/Icons/skip-previous-outline-light.png"
        let play = "avares://Raznor.Desktop/Assets/Icons/play-outline-light.png"
        let pause = "avares://Raznor.Desktop/Assets/Icons/pause-circle-outline-light.png"
        let stop = "avares://Raznor.Desktop/Assets/Icons/stop-circle-outline-light.png"
        let next = "avares://Raznor.Desktop/Assets/Icons/skip-next-outline-light.png"
        StackPanel.create
            [ StackPanel.verticalAlignment VerticalAlignment.Bottom
              StackPanel.horizontalAlignment HorizontalAlignment.Left
              StackPanel.orientation Orientation.Horizontal
              StackPanel.dock Dock.Bottom
              StackPanel.children
                  [ yield Button.create
                              [ Button.background (Bitmap.Create backwards |> ImageBrush)
                                Button.onClick (fun _ -> dispatch Previous) ]
                    if state.player.IsPlaying then
                        yield Button.create
                                  [ Button.background (Bitmap.Create pause |> ImageBrush)
                                    Button.onClick (fun _ -> dispatch Pause) ]
                        yield Button.create
                                  [ Button.background (Bitmap.Create stop |> ImageBrush)
                                    Button.onClick (fun _ -> dispatch Stop) ]
                    else
                        yield Button.create
                                  [ Button.background (Bitmap.Create play |> ImageBrush)
                                    Button.onClick (fun _ -> dispatch PlayInternal) ]
                    yield Button.create
                              [ Button.background (Bitmap.Create next |> ImageBrush)
                                Button.onClick (fun _ -> dispatch Next) ] ] ]

    let private progressBar (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.verticalAlignment VerticalAlignment.Bottom
              StackPanel.horizontalAlignment HorizontalAlignment.Stretch
              StackPanel.orientation Orientation.Horizontal
              StackPanel.dock Dock.Top
              StackPanel.width 420.0
              StackPanel.children
                  [ Slider.create
                      [ Slider.minimum 0.0
                        Slider.maximum 100.0
                        Slider.value (state.sliderPos |> double)
                        Slider.onValueChanged (fun value -> dispatch (Seek value)) ] ] ]

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
                  [ progressBar state dispatch
                    mediaButtons state dispatch ] ]
