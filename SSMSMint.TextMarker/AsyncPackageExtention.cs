using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using NLog;
using SSMSMint.Shared.Extentions;
using SSMSMint.Shared.Settings;
using System;
using System.Collections.Generic;
using Task = System.Threading.Tasks.Task;

namespace SSMSMint.TextMarker;

public static class AsyncPackageExtention
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private static readonly Dictionary<Window, VsTextLinesEventsListener> _events = new();

    public static void InitializeTextMarker(this AsyncPackage package, WindowEvents winEvents)
    {
        if (winEvents == null)
        {
            _logger.Error($"{nameof(InitializeTextMarker)} not Initialized. Registered window events not found");
            return;
        }
        winEvents.WindowCreated += async (window) => await OnWindowCreated(window, package);
        winEvents.WindowClosing += OnWindowClosing;

        _logger.Info($"{nameof(InitializeTextMarker)} Initialized");
    }

    private static async Task OnWindowCreated(Window window, AsyncPackage package)
    {
        try
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settings = (SSMSMintSettings)package.GetDialogPage(typeof(SSMSMintSettings)) ?? throw new Exception("Settings not found");
            if (!settings.TextMarkersEnabled)
                return;

            window.GetWindowFrame(out var windowFrame);

            if (windowFrame == null)
                return;

            windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out object lines);

            if (lines == null || lines is not IVsTextLines)
                return;

            var tagger = new TextMarkerTagger();
            var listener = new VsTextLinesEventsListener((IVsTextLines)lines, tagger);

            if (!_events.ContainsKey(window))
                _events.Add(window, listener);

            tagger.RefreshTextMarkers((IVsTextLines)lines);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            throw;
        }
    }
    private static void OnWindowClosing(Window window)
    {
        try
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_events.TryGetValue(window, out var listener))
            {
                listener.Dispose();
                _events.Remove(window);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
            throw;
        }
    }
}
