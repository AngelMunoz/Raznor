module Raznor.Core.Components

open Avalonia.Input
open Avalonia.Layout
open Avalonia.Media.Imaging
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL

open Raznor.Core.Types
open Avalonia.FuncUI.Types

type Shell =

    static member Menubar(searchingChromecast: bool, onSelectFiles, onOpenDirectory, onToggleChromecast) =
        Menu.create [
            Menu.dock Dock.Top
            Menu.viewItems [
                MenuItem.create [
                    MenuItem.header "Files"
                    MenuItem.viewItems [
                        MenuItem.create [
                            MenuItem.header "Select Files"
                            MenuItem.icon (Image.FromString "avares://Raznor.Core/Assets/Icons/file-multiple-dark.png")
                            MenuItem.onClick (fun _ -> onSelectFiles ())
                        ]
                        MenuItem.create [
                            MenuItem.header "Select Folder"
                            MenuItem.icon (Image.FromString "avares://Raznor.Core/Assets/Icons/folder-music-dark.png")
                            MenuItem.onClick (fun _ -> onOpenDirectory ())
                        ]
                    ]
                ]
                MenuItem.create [
                    MenuItem.header (
                        sprintf
                            "%s Chromecast"
                            (if searchingChromecast then
                                 "Disable"
                             else
                                 "Enable")
                    )
                    MenuItem.onClick (fun _ -> onToggleChromecast ())
                ]
            ]
        ]

type Playlist =

    static member SongList(selected: int, songs: SongRecord list, onSongSelected: SongRecord -> unit) =
        let songTemplate song =
            StackPanel.create [
                StackPanel.spacing 8.0
                StackPanel.onDoubleTapped (fun _ -> onSongSelected song)
                StackPanel.onKeyUp (fun keyargs ->
                    match keyargs.Key with
                    | Key.Enter -> onSongSelected song
                    | _ -> ())
                StackPanel.children [
                    TextBlock.create [
                        TextBlock.text song.name
                    ]
                ]
            ]

        ListBox.create [
            ListBox.dataItems songs
            ListBox.maxHeight 596.0
            ListBox.selectedIndex selected
            ListBox.itemTemplate (DataTemplateView<Types.SongRecord>.create songTemplate)
        ]

    static member EmptySongList() =
        StackPanel.create [
            StackPanel.spacing 8.0
            StackPanel.onPointerPressed (fun args ->
                let point = args.GetPosition null
                printfn "%A" point)
            StackPanel.children [
                TextBlock.create [
                    TextBlock.text "Nothing to play here :)"
                ]
            ]
        ]

type Player =

    static member MediaButtons
        (
            isPlaying: bool,
            loopState: LoopState,
            onPlayStateChange,
            onRequestPlay,
            onLoopStateChanged,
            onShuffleRequested
        ) =

        StackPanel.create [
            StackPanel.verticalAlignment VerticalAlignment.Bottom
            StackPanel.horizontalAlignment HorizontalAlignment.Left
            StackPanel.orientation Orientation.Horizontal
            StackPanel.dock Dock.Top
            StackPanel.children [
                Button.create [
                    Button.content Icons.previous
                    Button.classes [ "mediabtn" ]
                    Button.onClick (fun _ -> onRequestPlay PlayDirection.Previous)
                ]
                if isPlaying then
                    Button.create [
                        Button.content Icons.pause
                        Button.classes [ "mediabtn" ]
                        Button.onClick (fun _ -> onPlayStateChange PlayState.Pause)
                    ]

                    Button.create [
                        Button.content Icons.stop
                        Button.classes [ "mediabtn" ]
                        Button.onClick (fun _ -> onPlayStateChange PlayState.Stop)
                    ]
                else
                    Button.create [
                        Button.content Icons.play
                        Button.classes [ "mediabtn" ]
                        Button.onClick (fun _ -> onPlayStateChange PlayState.Play)
                    ]
                Button.create [
                    Button.content Icons.next
                    Button.classes [ "mediabtn" ]
                    Button.onClick (fun _ -> onRequestPlay PlayDirection.Next)
                ]
                Button.create [
                    Button.content Icons.shuffle
                    Button.classes [ "mediabtn" ]
                    Button.onClick (fun _ -> onShuffleRequested ())
                ]
                match loopState with
                | LoopState.All ->
                    Button.create [
                        Button.content Icons.repeat
                        Button.classes [ "mediabtn" ]
                        Button.onClick (fun _ -> onLoopStateChanged LoopState.Single)
                    ]
                | LoopState.Single ->
                    Button.create [
                        Button.content Icons.repeatOne
                        Button.classes [ "mediabtn" ]
                        Button.onClick (fun _ -> onLoopStateChanged LoopState.Off)
                    ]
                | LoopState.Off ->
                    Button.create [
                        Button.content Icons.repeatOff
                        Button.classes [ "mediabtn" ]
                        Button.onClick (fun _ -> onLoopStateChanged LoopState.All)
                    ]
            ]
        ]

    static member ProgressBar(sliderPosition: int) =
        StackPanel.create [
            StackPanel.verticalAlignment VerticalAlignment.Bottom
            StackPanel.horizontalAlignment HorizontalAlignment.Center
            StackPanel.orientation Orientation.Horizontal
            StackPanel.dock Dock.Bottom
            StackPanel.children [
                Slider.create [
                    Slider.minimum 0.0
                    Slider.maximum 100.0
                    Slider.width 428.0
                    Slider.horizontalAlignment HorizontalAlignment.Center
                    Slider.value sliderPosition
                ]
            ]
        ]

    static member Player(children: IView list) =
        DockPanel.create [
            DockPanel.classes [ "mediabar" ]
            DockPanel.dock Dock.Bottom
            DockPanel.horizontalAlignment HorizontalAlignment.Center
            DockPanel.children children
        ]