namespace Raznor

module Playlist =
    open System
    open Elmish
    open Avalonia.Controls
    open Avalonia.Controls.Primitives
    open Avalonia.Input
    open Avalonia.FuncUI
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI.Types
    open Raznor.Core
    open Raznor.Core.Components

    type State =
        { songList: Types.SongRecord list option
          currentIndex: int
          loopState: Types.LoopState }

    type ExternalMsg = PlaySong of index: int * song: Types.SongRecord

    type Msg =
        | GetAny
        | GetNext
        | GetPrevious
        | Shuffle
        | AddFiles of Types.SongRecord list
        | PlaySong of Types.SongRecord
        | SetLoopState of Types.LoopState

    let init =
        { songList = None
          currentIndex = 0
          loopState = Types.LoopState.Off }

    let private shuffle (org: _ list) =
        let rng = Random()
        let arr = Array.copy (org |> List.toArray)
        let max = (arr.Length - 1)

        let randomSwap (arr: _ []) i =
            let pos = rng.Next(max)
            let tmp = arr.[pos]
            arr.[pos] <- arr.[i]
            arr.[i] <- tmp
            arr

        [| 0..max |]
        |> Array.fold randomSwap arr
        |> Array.toList

    let private tryFindSong (songlist: Types.SongRecord list option) (song: Types.SongRecord) =
        match songlist with
        | Some songlist ->
            match songlist
                  |> List.tryFindIndex (fun (sng: Types.SongRecord) -> sng.id = song.id)
                with
            | Some index -> Some index
            | None -> None
        | None -> None

    let update (msg: Msg) (state: State) : State * Cmd<Msg> * ExternalMsg option =
        match msg with
        | AddFiles files -> { state with songList = Some files }, Cmd.none, None
        | SetLoopState loopState -> { state with loopState = loopState }, Cmd.none, None
        | PlaySong song ->
            let index = tryFindSong state.songList song

            match index with
            | Some index -> { state with currentIndex = index }, Cmd.none, Some(ExternalMsg.PlaySong(index, song))
            | None -> state, Cmd.none, None
        | GetAny ->
            match state.songList with
            | Some songs ->
                if songs.IsEmpty then
                    state, Cmd.none, None
                else
                    state, Cmd.ofMsg (PlaySong songs.Head), None
            | None -> state, Cmd.none, None
        | GetNext ->
            match state.songList with
            | Some songs ->
                if songs.IsEmpty then
                    state, Cmd.none, None
                else if state.currentIndex + 1 >= songs.Length then
                    match state.loopState with
                    | Types.LoopState.Off -> state, Cmd.none, None
                    | Types.LoopState.All -> { state with currentIndex = 0 }, Cmd.ofMsg (PlaySong songs.Head), None
                    | Types.LoopState.Single -> state, Cmd.ofMsg (PlaySong(songs.Item(state.currentIndex))), None
                else
                    match state.loopState with
                    | Types.LoopState.Single -> state, Cmd.ofMsg (PlaySong(songs.Item(state.currentIndex))), None
                    | _ ->
                        let song = songs.Item(state.currentIndex + 1)
                        state, Cmd.ofMsg (PlaySong song), None
            | None -> state, Cmd.none, None
        | GetPrevious ->
            match state.songList with
            | Some songs ->
                if songs.IsEmpty then
                    state, Cmd.none, None
                else if (state.currentIndex - 1) < 0 then
                    state, Cmd.none, None
                else
                    let song = songs.Item(state.currentIndex - 1)
                    state, Cmd.ofMsg (PlaySong song), None
            | None -> state, Cmd.none, None
        | Shuffle ->
            match state.songList with
            | Some songs ->
                let shuffled = shuffle songs

                { state with
                    songList = Some shuffled
                    currentIndex = 0 },
                Cmd.none,
                None
            | None -> state, Cmd.none, None

    let view (state: State) (dispatch: Msg -> unit) =
        StackPanel.create [
            StackPanel.dock Dock.Top
            StackPanel.name "playlist"
            StackPanel.verticalScrollBarVisibility ScrollBarVisibility.Auto
            StackPanel.maxHeight 420.0
            StackPanel.children [
                match state.songList with
                | Some songs ->
                    match songs |> List.isEmpty with
                    | true -> Playlist.EmptySongList()
                    | false -> Playlist.SongList(state.currentIndex, songs, (PlaySong >> dispatch))
                | None -> Playlist.EmptySongList()
            ]
        ]