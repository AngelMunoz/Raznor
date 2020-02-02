namespace Raznor.App

module Content =
    open Avalonia.Controls
    open Avalonia.FuncUI.Components
    open Avalonia.FuncUI.DSL
    open Avalonia.FuncUI.Types
    open Elmish
    open Raznor.Core

    type State =
        { musicCollections: Types.MusicCollection list
          selectedCollection: Types.MusicCollection option
          selectedCollectionSongs: Types.SongRecord list option
          collectionsLoading: bool
          currentSelection: Types.SongRecord list }

    type ExternalMsg = AddToPlayList of Types.SongRecord list

    type Msg =
        | AddDefaultCollections
        | SetCollectionsLoading of bool
        | SetSelectedCollection of Types.MusicCollection option
        | SetSelectedCollectionSongs of Types.SongRecord list option
        | AfterDefaultCollections of Types.MusicCollection list
        | SelectionItemsChanged of Types.SongRecord list
        | AddSongsToPlaylist of Types.SongRecord list

    let init =
        let collections = MusicCollections.getMusicCollections
        { musicCollections = collections
          selectedCollection = None
          selectedCollectionSongs = None
          collectionsLoading = false
          currentSelection = List.empty }

    let update msg state =
        match msg with
        | SetCollectionsLoading isLoading -> { state with collectionsLoading = isLoading }, Cmd.none, None
        | AddDefaultCollections ->
            let cmd =
                Cmd.batch
                    [ Cmd.ofMsg (SetCollectionsLoading true)
                      Cmd.OfFunc.perform MusicCollections.createDefaultSongs (CollectionSettings.getDefaultPaths)
                          (fun _ -> AfterDefaultCollections(MusicCollections.defaultMusicCollections)) ]
            state, cmd, None
        | AfterDefaultCollections collections ->
            { state with
                  musicCollections = collections
                  collectionsLoading = false }, Cmd.none, None
        | SetSelectedCollection collection ->
            let songs =
                match collection with
                | Some collection -> MusicCollections.getSongsByMusicCollection collection.id
                | None -> List.empty
            { state with selectedCollection = collection }, Cmd.ofMsg (SetSelectedCollectionSongs(Some songs)), None
        | SetSelectedCollectionSongs songs -> { state with selectedCollectionSongs = songs }, Cmd.none, None
        | SelectionItemsChanged songs -> { state with currentSelection = songs }, Cmd.none, None
        | AddSongsToPlaylist songs -> state, Cmd.none, Some(ExternalMsg.AddToPlayList(songs))



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
                          Button.isEnabled (not state.collectionsLoading)
                          Button.onClick (fun _ -> dispatch AddDefaultCollections) ] ] ]

    let private collectionList (state: State) (dispatch: Msg -> unit) =
        match state.musicCollections |> List.isEmpty with
        | true -> emptyCollectionList state dispatch :> IView
        | false -> musiCollectionList state dispatch :> IView

    let private songTemplate (song: Types.SongRecord) (dispatch: Msg -> unit) =
        StackPanel.create
            [ StackPanel.spacing 8.0
              StackPanel.children [ TextBlock.create [ TextBlock.text song.name ] ] ]

    let private songRecordList (songs: Types.SongRecord list) (dispatch: Msg -> unit) =
        ListBox.create
            [ ListBox.dock Dock.Right
              ListBox.dataItems songs
              ListBox.selectionMode SelectionMode.Multiple
              ListBox.onSelectedItemsChanged (fun items ->
                  let songs =
                      seq {
                          for item in items do
                              yield item :?> Types.SongRecord
                      }
                      |> Seq.toList
                  dispatch (SelectionItemsChanged songs))
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
                  songList state dispatch
                  StackPanel.create
                      [ StackPanel.dock Dock.Top
                        StackPanel.children
                            [ if state.currentSelection.Length > 0 then
                                yield Button.create
                                          [ Button.content "Add Songs to Playlist"
                                            Button.onClick
                                                (fun _ -> dispatch (AddSongsToPlaylist state.currentSelection)) ] ] ] ] ]

    let view (state: State) (dispatch: Msg -> unit) =
        TabControl.create
            [ TabControl.dock Dock.Left
              TabControl.tabStripPlacement Dock.Bottom
              TabControl.viewItems
                  [ TabItem.create
                      [ TabItem.header "Music Collections"
                        TabItem.content (musicCollection state dispatch) ]
                    TabItem.create
                        [ TabItem.header "File Explorer"
                          TabItem.content "Content" ] ] ]
