using System;
using System.Threading;
using System.Windows;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace TranslatorCsV2;

public partial class App : Application
{
    private Mutex? _single;
    private Translator? _translator;

    protected override void OnStartup(StartupEventArgs e)
    {
        var mutex = new Mutex(true, @"Global\TranslatorCsV2_c9f4e2", out bool first);
        if (!first)
        {
            mutex.Dispose();
            Shutdown();
            return;
        }
        _single = mutex;

        DispatcherUnhandledException += (_, ev) =>
        {
            MessageBox.Show(ev.Exception.Message, "Translator", MessageBoxButton.OK, MessageBoxImage.Error);
            ev.Handled = true;
        };

        base.OnStartup(e);
        _translator = new Translator();
        _translator.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _translator?.Dispose();
        _single?.ReleaseMutex();
        _single?.Dispose();
        base.OnExit(e);
    }
}
