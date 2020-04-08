namespace Raznor

module Player =
  open System
  open System.Collections.Generic
  open Elmish
  open LibVLCSharp.Shared
  open Avalonia.Controls
  open Avalonia.Controls.Primitives
  open Avalonia.Layout
  open Avalonia.FuncUI
  open Avalonia.FuncUI.DSL
  open Raznor.Core

  type State =
    { player : MediaPlayer
      length : int64
      sliderPos : int
      loopState : Types.LoopState
      showRenderers : bool
      rendererItems : HashSet<RendererItem> }

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
    | AddRenderer of RendererItem
    | RemoveRenderer of RendererItem
    | StartCasting of string
    | Previous
    | Pause
    | Stop
    | PlayInternal
    | Next
    | Shuffle
    | ShowRenderers
    | StopCasting

  let init player =
    { player = player
      length = 0L
      sliderPos = 0
      loopState = Types.LoopState.Off
      showRenderers = false
      rendererItems = HashSet<RendererItem>() }

  let update msg state =
    match msg with
    | Play song ->
        use media = PlayerLib.getMediaFromlocal song.path
        state.player.Play media |> ignore
        state, Cmd.ofMsg (SetLength state.player.Length), None
    | Seek position ->
        let time = (position |> int64) * state.player.Length / 100L
        state, Cmd.none, None
    | SetLength length -> { state with length = length }, Cmd.none, None
    | SetPos position ->
        let pos = (position * 100L / state.player.Length) |> int
        { state with sliderPos = pos }, Cmd.none, None
    | SetLoopState loopState ->
        { state with loopState = loopState }, Cmd.none,
        Some(ExternalMsg.SetLoopState loopState)
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
    | AddRenderer rendererItem ->
        state.rendererItems.Add rendererItem |> ignore
        state, Cmd.none, None
    | RemoveRenderer rendererItem ->
        state.rendererItems.Remove rendererItem |> ignore
        state, Cmd.none, None
    | ShowRenderers -> { state with showRenderers = true }, Cmd.none, None
    | StartCasting rendererName ->
        let renderer =
          state.rendererItems
          |> Seq.tryFind (fun renderer -> renderer.Name = rendererName)
        match renderer with
        | Some renderer -> state.player.SetRenderer renderer |> ignore
        | None -> printfn "Renderer Not Found" |> ignore
        state, Cmd.none, None
    | StopCasting ->
        state.player.SetRenderer null |> ignore
        { state with showRenderers = false }, Cmd.none, None

  let private mediaButtons (state : State) (dispatch : Msg -> unit) =
    StackPanel.create
      [ StackPanel.verticalAlignment VerticalAlignment.Bottom
        StackPanel.horizontalAlignment HorizontalAlignment.Left
        StackPanel.orientation Orientation.Horizontal
        StackPanel.dock Dock.Top
        StackPanel.children
          [ Button.create
              [ Button.content Icons.previous
                Button.classes [ "mediabtn" ]
                Button.onClick (fun _ -> dispatch Previous) ]
            if state.player.IsPlaying then
              Button.create
                [ Button.content Icons.pause
                  Button.classes [ "mediabtn" ]
                  Button.onClick (fun _ -> dispatch Pause) ]
              Button.create
                [ Button.content Icons.stop
                  Button.classes [ "mediabtn" ]
                  Button.onClick (fun _ -> dispatch Stop) ]
            else
              Button.create
                [ Button.content Icons.play
                  Button.classes [ "mediabtn" ]
                  Button.onClick (fun _ -> dispatch PlayInternal) ]
            Button.create
              [ Button.content Icons.next
                Button.classes [ "mediabtn" ]
                Button.onClick (fun _ -> dispatch Next) ]
            Button.create
              [ Button.content Icons.shuffle
                Button.classes [ "mediabtn" ]
                Button.onClick (fun _ -> dispatch Shuffle) ]
            match state.loopState with
            | Types.LoopState.All ->
                Button.create
                  [ Button.content Icons.repeat
                    Button.classes [ "mediabtn" ]
                    Button.onClick
                      (fun _ -> dispatch (SetLoopState Types.LoopState.Single)) ]
            | Types.LoopState.Single ->
                Button.create
                  [ Button.content Icons.repeatOne
                    Button.classes [ "mediabtn" ]
                    Button.onClick
                      (fun _ -> dispatch (SetLoopState Types.LoopState.Off)) ]
            | Types.LoopState.Off ->
                Button.create
                  [ Button.content Icons.repeatOff
                    Button.classes [ "mediabtn" ]
                    Button.onClick
                      (fun _ -> dispatch (SetLoopState Types.LoopState.All)) ]
            if state.rendererItems.Count > 0 && not (state.showRenderers) then
              Button.create
                [ Button.content Icons.cast
                  Button.classes [ "mediabtn" ]
                  Button.onClick (fun _ -> dispatch ShowRenderers) ] ] ]

  let private progressBar (state : State) (dispatch : Msg -> unit) =
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

  let private rendererList (state : State) (dispatch : Msg -> unit) =
    StackPanel.create
      [ StackPanel.verticalAlignment VerticalAlignment.Bottom
        StackPanel.horizontalAlignment HorizontalAlignment.Center
        StackPanel.orientation Orientation.Horizontal
        StackPanel.dock Dock.Top
        StackPanel.horizontalScrollBarVisibility ScrollBarVisibility.Auto
        StackPanel.children
          [ if state.showRenderers then
              Button.create
                [ Button.classes [ "mediabtn" ]
                  Button.content Icons.castOff
                  Button.onClick (fun _ -> dispatch StopCasting) ]
            for renderer in state.rendererItems do
              Button.create
                [ Button.classes [ "rendereritem" ]
                  Button.content (renderer.Name)
                  Button.onClick
                    (fun _ -> dispatch (StartCasting renderer.Name)) ] ] ]

  let view (state : State) (dispatch : Msg -> unit) =
    DockPanel.create
      [ DockPanel.classes [ "mediabar" ]
        DockPanel.dock Dock.Bottom
        DockPanel.horizontalAlignment HorizontalAlignment.Center
        DockPanel.children
          [ if state.showRenderers then rendererList state dispatch
            progressBar state dispatch
            mediaButtons state dispatch ] ]
