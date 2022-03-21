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
        { player: MediaPlayer
          length: int64
          sliderPos: int
          loopState: Types.LoopState
          showRenderers: bool
          rendererItems: HashSet<RendererItem> }

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

    let view (state: State) (dispatch: Msg -> unit) =
        let onPlayStateChange (state: Types.PlayState) =
            match state with
            | Types.PlayState.Play -> dispatch PlayInternal
            | Types.PlayState.Pause -> dispatch Pause
            | Types.PlayState.Stop -> dispatch Stop

        let onRequestPlay (direction: Types.PlayDirection) =
            match direction with
            | Types.PlayDirection.Next -> dispatch Next
            | Types.PlayDirection.Previous -> dispatch Previous
            | Types.PlayDirection.Direct _ -> ()

        let onLoopStateChanged (loopState: Types.LoopState) =
            match loopState with
            | Types.LoopState.All -> dispatch (SetLoopState Types.LoopState.Single)
            | Types.LoopState.Single -> dispatch (SetLoopState Types.LoopState.Off)
            | Types.LoopState.Off -> dispatch (SetLoopState Types.LoopState.All)


        let onShuffleRequested () = dispatch Shuffle

        Components.Player.Player [
            Components.Player.ProgressBar(state.sliderPos)
            Components.Player.MediaButtons(
                state.player.IsPlaying,
                state.loopState,
                onPlayStateChange,
                onRequestPlay,
                onLoopStateChanged,
                onShuffleRequested
            )
        ]