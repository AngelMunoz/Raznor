namespace Raznor.App

open Avalonia.FuncUI.Components
open Avalonia.FuncUI.Types
open LiteDB
open System.IO

module Content =
    open System
    open Elmish
    open Avalonia.Controls
    open Avalonia.FuncUI.DSL
    open Raznor.Core

    type State =
        { musicCollections: Types.MusicCollection list
          selectedCollection: Types.MusicCollection option
          selectedCollectionSongs: Types.SongRecord list option }

    type ExternalMsg = PlaySong of Types.SongRecord

    type Msg =
        | AddDefaultCollections
        | SetSelectedCollection of Types.MusicCollection option
        | SetSelectedCollectionSongs of Types.SongRecord list option
        | PlaySong of Types.SongRecord

    let init =
        let collections = MusicCollections.getMusicCollections
        { musicCollections = collections
          selectedCollection = None
          selectedCollectionSongs = None }

    let getpathsAndCollections (collections: Types.MusicCollection list) (collection: Types.CollectionSettings) =
        let col = collections |> List.find (fun col -> col.name = collection.name)

        let mp3 =
            Directory.EnumerateFiles(collection.path, "*.mp3", SearchOption.AllDirectories)
            |> Seq.toList
            |> List.map (fun path -> File.OpenRead(path), path)
            |> List.map (fun (filestr, path) -> filestr.Name, path)

        let wav =
            Directory.EnumerateFiles(collection.path, "*.wav", SearchOption.AllDirectories)
            |> Seq.toList
            |> List.map (fun path -> File.OpenRead(path), path)
            |> List.map (fun (filestr, path) -> filestr.Name, path)

        let mid =
            Directory.EnumerateFiles(collection.path, "*.mid", SearchOption.AllDirectories)
            |> Seq.toList
            |> List.map (fun path -> File.OpenRead(path), path)
            |> List.map (fun (filestr, path) -> filestr.Name, path)

        col, mp3 @ wav @ mid

    let createSongsFromCollection row =
        let collection, items = row
        items
        |> List.map (fun (name, path) ->
            let song: Types.SongRecord =
                { id = ObjectId.NewObjectId()
                  name = name
                  path = path
                  createdAt = DateTime.Now
                  isIn = [] }
            MusicCollections.addSongToCollection song collection)

    let update msg state =
        match msg with
        | AddDefaultCollections ->
            let collections =
                CollectionSettings.getDefaultPaths
                |> List.map MusicCollections.getPreMusiColFromPath
                |> MusicCollections.createMusicCollections

            CollectionSettings.getDefaultPaths
            |> List.map (fun collection -> getpathsAndCollections collections collection)
            |> List.map createSongsFromCollection
            |> ignore

            { state with musicCollections = collections }, Cmd.none, None
        | SetSelectedCollection collection ->
            let songs =
                match collection with
                | Some collection -> MusicCollections.getSongsByMusicCollection collection.id
                | None -> List.empty
            { state with selectedCollection = collection }, Cmd.ofMsg (SetSelectedCollectionSongs(Some songs)), None
        | SetSelectedCollectionSongs songs -> { state with selectedCollectionSongs = songs }, Cmd.none, None
        | PlaySong song -> state, Cmd.none, Some(ExternalMsg.PlaySong song)



    let private musiColTemplate (collection: Types.MusicCollection) =
        StackPanel.create
            [ StackPanel.spacing 8.0
              StackPanel.children
                  [ TextBlock.create [ TextBlock.text collection.name ]
                    TextBlock.create [ TextBlock.text (collection.createdAt.ToShortDateString()) ] ] ]

    let private musiCollectionList (state: State) (dispatch: Msg -> unit) =
        ListBox.create
            [ ListBox.dock Dock.Left
              ListBox.onSelectedItemChanged (fun item ->
                  match isNull (item) with
                  | true -> dispatch (SetSelectedCollection None)
                  | false -> dispatch (SetSelectedCollection(Some(item :?> Types.MusicCollection))))
              ListBox.dataItems state.musicCollections
              ListBox.itemTemplate (DataTemplateView<Types.MusicCollection>.create musiColTemplate) ]

    let private emptyCollectionList (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.spacing 8.0
              StackPanel.dock Dock.Left
              StackPanel.children
                  [ TextBlock.create [ TextBlock.text "You don't seem to have a music collection." ]
                    Button.create
                        [ Button.tip "My Music and OneDrive Music Folder"
                          Button.content "Add default collections"
                          Button.onClick (fun _ -> dispatch AddDefaultCollections) ]
                    Button.create [ Button.content "Select From Folder" ] ] ]

    let private collectionList (state: State) (dispatch: Msg -> unit) =
        match state.musicCollections |> List.isEmpty with
        | true -> emptyCollectionList state dispatch :> IView
        | false -> musiCollectionList state dispatch :> IView

    let private songTemplate (song: Types.SongRecord) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.spacing 8.0
              StackPanel.onDoubleTapped(fun _ -> dispatch (PlaySong song))
              StackPanel.children [ TextBlock.create [ TextBlock.text song.name ] ] ]

    let private songRecordList (songs: Types.SongRecord list) (dispatch: Msg -> unit) =
        ListBox.create
            [ ListBox.dock Dock.Right
              ListBox.dataItems songs
              ListBox.itemTemplate (DataTemplateView<Types.SongRecord>.create(fun item -> songTemplate item dispatch)) ]

    let private emptySongList (state: State) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.spacing 8.0
              StackPanel.children [ TextBlock.create [ TextBlock.text "This Collection seems empty :(" ] ] ]

    let private songList (state: State) (dispatch: Msg -> unit) =
        match state.selectedCollectionSongs with
        | Some songs ->
            match songs |> List.isEmpty with
            | true -> emptySongList state dispatch :> IView
            | false -> songRecordList songs dispatch :> IView
        | None -> emptySongList state dispatch :> IView

    let private musicCollection (state: State) (dispatch: Msg -> unit) =
        DockPanel.create
            [ DockPanel.children
                [ collectionList state dispatch
                  songList state dispatch ] ]

    let view (state: State) (dispatch: Msg -> unit) =
        TabControl.create
            [ TabControl.dock Dock.Top
              TabControl.tabStripPlacement Dock.Top
              TabControl.viewItems
                  [ TabItem.create
                      [ TabItem.header "Music Collections"
                        TabItem.content (musicCollection state dispatch) ]
                    TabItem.create
                        [ TabItem.header "File Explorer"
                          TabItem.content "Content" ] ] ]