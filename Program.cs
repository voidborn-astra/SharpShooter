// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.Reflection;

namespace SharpShooter;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length >= 3 && args[0] == "--report")
        {
            try
            {
                var runs = args.Skip(2).Select(path => LogParser.Parse(path)).ToArray();
                HtmlReport.Write(args[1], runs, new());
                return 0;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.WriteLine(exception);
                return 1;
            }
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    /// <summary>Display version, such as "0.1.0", without build metadata.</summary>
    public static string Version { get; } =
        (typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0")
        .Split('+')[0];

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<ShooterApp>().UsePlatformDetect().WithInterFont();
}

public sealed partial class ShooterApp : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}
