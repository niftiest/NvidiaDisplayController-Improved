using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using Caliburn.Micro;
using Ninject;
using NLog;
using NvidiaDisplayController.Global;
using NvidiaDisplayController.Global.Controllers;
using NvidiaDisplayController.Global.Extensions;
using Application = System.Windows.Application;

namespace NvidiaDisplayController.Interface.Shell;

public partial class ShellView
{
    private const int WmHotkey = 0x0312;

    private NotifyIcon? _notifyIcon;
    private HwndSource? _hwndSource;

    public ShellView()
    {
        InitializeComponent();
        Start();
    }

    [Inject] public DataController DataController { get; set; } = null!;
    [Inject] public ILogger Logger { get; set; } = null!;

    private void Start()
    {
        IoC.BuildUp(this);

        CreateSystemTrayIcon();

        GlobalEvents.UpdateToolTip += OnUpdateToolTip;
        GlobalEvents.ReRegisterHotkeys += OnReRegisterHotkeys;

        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        _hwndSource = HwndSource.FromHwnd(helper.Handle);
        _hwndSource?.AddHook(WndProc);

        RegisterAllHotkeys();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_hwndSource is null)
            return;

        HotkeyController.UnregisterAll(_hwndSource.Handle);
        _hwndSource.RemoveHook(WndProc);
    }

    private void RegisterAllHotkeys()
    {
        if (_hwndSource is null)
            return;

        DataController.Load()
            .IfSuccess(computer =>
            {
                var allProfiles = computer!.Monitors.SelectMany(m => m.Profiles);
                HotkeyController.RegisterAll(_hwndSource.Handle, allProfiles, Logger);
            });
    }

    private void OnReRegisterHotkeys()
    {
        if (_hwndSource is null)
            return;

        DataController.Load()
            .IfSuccess(computer =>
            {
                var allProfiles = computer!.Monitors.SelectMany(m => m.Profiles);
                HotkeyController.ReRegisterAll(_hwndSource.Handle, allProfiles, Logger);
            });
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && HotkeyController.TryGetHotkey(wParam, out var modifiers, out var virtualKey))
        {
            if (DataContext is ShellViewModel viewModel)
                viewModel.ApplyProfileByHotkey(modifiers, virtualKey);

            handled = true;
        }

        return IntPtr.Zero;
    }

    private void OnUpdateToolTip()
    {
        BuildToolTip();
    }

    private void CreateSystemTrayIcon()
    {
        _notifyIcon = new NotifyIcon();
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "desktop.ico");
        _notifyIcon.Icon = new Icon(iconPath);
        _notifyIcon.Visible = true;

        _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
        _notifyIcon.ContextMenuStrip.Items.Add("Show", null, OpenEvent);
        _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, ExitEvent);

        BuildToolTip();
    }

    private void BuildToolTip()
    {
        DataController.Load()
            .IfSuccess(data =>
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Nvidia Display Controller");
                foreach (var monitor in data!.Monitors)
                {
                    var activeProfile = monitor.Profiles.Single(p => p.IsActive);
                    stringBuilder.AppendLine($"{monitor.Name} - {activeProfile.Name}");
                }

                _notifyIcon!.Text = stringBuilder.ToString();
            });
    }

    private void ExitEvent(object? sender, EventArgs args)
    {
        Application.Current.Shutdown();
    }

    private void OpenEvent(object? sender, EventArgs args)
    {
        DoShow();
    }

    private void DoShow()
    {
        Show();
        WindowState = WindowState.Normal;
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
            Hide();

        base.OnStateChanged(e);
    }
}
