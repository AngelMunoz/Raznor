namespace Raznor.App

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI
open Raznor.App.Shell
open LibVLCSharp.Shared
open Raznor.Core.Database

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Load "avares://Avalonia.Themes.Default/DefaultTheme.xaml"
        this.Styles.Load "avares://Avalonia.Themes.Default/Accents/BaseDark.xaml"
        this.Styles.Load "avares://Raznor.App/Styles.xaml"
        Core.Initialize()

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime -> desktopLifetime.MainWindow <- ShellWindow()
        | _ -> ()

module Program =

    [<EntryPoint>]
    let main (args: string []) =
        use db = getDatabase dbpath
        db.Dispose()
        AppBuilder.Configure<App>().UsePlatformDetect().UseSkia().StartWithClassicDesktopLifetime(args)
