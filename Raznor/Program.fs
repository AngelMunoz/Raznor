namespace Raznor

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI
open Raznor.Shell
open LibVLCSharp.Shared

type App() =
  inherit Application()

  member this.Initialize() =
    this.Styles.Load "avares://Citrus.Avalonia/Rust.xaml"
    this.Styles.Load "avares://Raznor/Styles.xaml"
    Core.Initialize()

  member this.OnFrameworkInitializationCompleted() =
    match this.ApplicationLifetime with
    | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
        desktopLifetime.MainWindow <- ShellWindow()
    | _ -> ()

module Program =
  [<EntryPoint>]
  let main (args : string []) =
    AppBuilder.Configure<App>().UsePlatformDetect().UseSkia()
      .StartWithClassicDesktopLifetime(args)
