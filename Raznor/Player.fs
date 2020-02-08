namespace Raznor



module Player =
    open Elmish
    open LibVLCSharp.Shared
    open Avalonia.Controls
    open Avalonia.Layout
    open Avalonia.FuncUI
    open Avalonia.FuncUI.DSL
    open Raznor.Core

    type State =
        { player: MediaPlayer
          length: int64
          sliderPos: int
          loopState: Types.LoopState }


    type ExternalMsg =
        | Next
        | Previous
        | Play
        | Shuffle
        | SetLoopState of Types.LoopState

    type Msg =
        | Play of Types.SongRecord
        | Seek of double
        | SetPos of int64
        | SetLength of int64
        | SetLoopState of Types.LoopState
        | Previous
        | Pause
        | Stop
        | PlayInternal
        | Next
        | Shuffle

    let init player =
        { player = player
          length = 0L
          sliderPos = 0
          loopState = Types.LoopState.Off }

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
        | SetLoopState loopState ->
            { state with loopState = loopState }, Cmd.none, Some(ExternalMsg.SetLoopState loopState)
        | Shuffle -> state, Cmd.none, Some ExternalMsg.Shuffle
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
        StackPanel.create
            [ StackPanel.verticalAlignment VerticalAlignment.Bottom
              StackPanel.horizontalAlignment HorizontalAlignment.Left
              StackPanel.orientation Orientation.Horizontal
              StackPanel.dock Dock.Top
              StackPanel.children
                  [ yield Button.create
                              [ Button.content Icons.previous
                                Button.classes [ "mediabtn" ]
                                Button.onClick (fun _ -> dispatch Previous) ]
                    if state.player.IsPlaying then
                        yield Button.create
                                  [ Button.content Icons.pause
                                    Button.classes [ "mediabtn" ]
                                    Button.onClick (fun _ -> dispatch Pause) ]
                        yield Button.create
                                  [ Button.content Icons.stop
                                    Button.classes [ "mediabtn" ]
                                    Button.onClick (fun _ -> dispatch Stop) ]
                    else
                        yield Button.create
                                  [ Button.content Icons.play
                                    Button.classes [ "mediabtn" ]
                                    Button.onClick (fun _ -> dispatch PlayInternal) ]
                    yield Button.create
                              [ Button.content Icons.next
                                Button.classes [ "mediabtn" ]
                                Button.onClick (fun _ -> dispatch Next) ]
                    yield Button.create
                              [ Button.content Icons.shuffle
                                Button.classes [ "mediabtn" ]
                                Button.onClick (fun _ -> dispatch Shuffle) ]
                    match state.loopState with
                    | Types.LoopState.All ->
                        yield Button.create
                                  [ Button.content Icons.repeat
                                    Button.classes [ "mediabtn" ]
                                    Button.onClick (fun _ -> dispatch (SetLoopState Types.LoopState.Single)) ]
                    | Types.LoopState.Single ->
                        yield Button.create
                                  [ Button.content Icons.repeatOne
                                    Button.classes [ "mediabtn" ]
                                    Button.onClick (fun _ -> dispatch (SetLoopState Types.LoopState.Off)) ]
                    | Types.LoopState.Off ->
                        yield Button.create
                                  [ Button.content Icons.repeatOff
                                    Button.classes [ "mediabtn" ]
                                    Button.onClick (fun _ -> dispatch (SetLoopState Types.LoopState.All)) ] ] ]

    let private progressBar (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.verticalAlignment VerticalAlignment.Bottom
              StackPanel.horizontalAlignment HorizontalAlignment.Center
              StackPanel.orientation Orientation.Horizontal
              StackPanel.dock Dock.Bottom
              StackPanel.children
                  [ Slider.create
                      [ Slider.minimum 0.0
                        Slider.maximum 100.0
                        Slider.width 428.0
                        Slider.horizontalAlignment HorizontalAlignment.Center
                        Slider.value (state.sliderPos |> double)
                        Slider.onValueChanged (fun value -> dispatch (Seek value)) ] ] ]

    let view (state: State) (dispatch: Msg -> unit) =
        DockPanel.create
            [ DockPanel.classes [ "mediabar" ]
              DockPanel.dock Dock.Bottom
              DockPanel.horizontalAlignment HorizontalAlignment.Center
              DockPanel.children
                  [ progressBar state dispatch
                    mediaButtons state dispatch ] ]
