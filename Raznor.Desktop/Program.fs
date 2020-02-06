namespace Raznor.Desktop

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI
open Raznor.Desktop.Shell
open LibVLCSharp.Shared

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Load "avares://Citrus.Avalonia/Magma.xaml"
        this.Styles.Load "avares://Raznor.Desktop/Styles.xaml"
        Core.Initialize()

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime -> desktopLifetime.MainWindow <- ShellWindow()
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main (args: string []) =
        AppBuilder.Configure<App>().UsePlatformDetect().UseSkia().StartWithClassicDesktopLifetime(args)
