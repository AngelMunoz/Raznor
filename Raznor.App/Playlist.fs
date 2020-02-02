namespace Raznor.App

module Playlist =
    open Avalonia.Controls
    open Avalonia.Input
    open Avalonia.FuncUI.Components
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI.Types
    open Elmish
    open Raznor.Core

    type State =
        { songList: Types.SongRecord list option }

    type ExternalMsg =
        | SetPlaylist of Types.SongRecord list
        | PlaySong of index: int * song: Types.SongRecord
        | RemoveSong of index: int * song: Types.SongRecord

    type Msg =
        | AddFiles of Types.SongRecord list
        | PlaySong of Types.SongRecord
        | RemoveSong of Types.SongRecord

    let init = { songList = None }

    let private tryFindSong (songlist: Types.SongRecord list option) (song: Types.SongRecord) =
        match songlist with
        | Some songlist ->
            match songlist |> List.tryFindIndex (fun (sng: Types.SongRecord) -> sng.id = song.id) with
            | Some index -> Some index
            | None -> None
        | None -> None

    let private filteredSongList (songlist: Types.SongRecord list option) (song: Types.SongRecord) =
        match songlist with
        | Some slist ->
            slist
            |> List.filter (fun item -> item.id <> song.id)
            |> Some
        | None -> None

    let update (msg: Msg) (state: State): State * Cmd<Msg> * ExternalMsg option =
        match msg with
        | AddFiles files -> { state with songList = Some files }, Cmd.none, None
        | PlaySong song ->
            let index = tryFindSong state.songList song
            match index with
            | Some index -> state, Cmd.none, Some(ExternalMsg.PlaySong(index, song))
            | None -> state, Cmd.none, None
        | RemoveSong song ->
            let index = tryFindSong state.songList song
            let songlist = filteredSongList state.songList song
            match index with
            | Some index -> { state with songList = songlist }, Cmd.none, Some(ExternalMsg.RemoveSong(index, song))
            | None -> state, Cmd.none, None

    let private songTemplate (song: Types.SongRecord) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.spacing 8.0
              StackPanel.onDoubleTapped (fun _ -> dispatch (PlaySong song))
              StackPanel.onKeyUp (fun keyargs ->
                  match keyargs.Key with
                  | Key.Enter -> dispatch (PlaySong song)
                  | Key.Delete -> dispatch (RemoveSong song)
                  /// eventually add other shortcuts to re-arrange songs or something alike
                  | _ -> ())
              StackPanel.children [ TextBlock.create [ TextBlock.text song.name ] ] ]

    let private songRecordList (songs: Types.SongRecord list) (dispatch: Msg -> unit) =
        ListBox.create
            [ ListBox.dataItems songs
              ListBox.itemTemplate (DataTemplateView<Types.SongRecord>.create(fun item -> songTemplate item dispatch)) ]

    let private emptySongList (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.spacing 8.0
              StackPanel.children
                  [ TextBlock.create
                      [ TextBlock.text "Nothing to play here :). Select something from your collections" ] ] ]

    let private songList (state: State) (dispatch: Msg -> unit) =
        match state.songList with
        | Some songs ->
            match songs |> List.isEmpty with
            | true -> emptySongList state dispatch :> IView
            | false -> songRecordList songs dispatch :> IView
        | None -> emptySongList state dispatch :> IView

    let view (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.dock Dock.Top
              StackPanel.children [ songList state dispatch ] ]
