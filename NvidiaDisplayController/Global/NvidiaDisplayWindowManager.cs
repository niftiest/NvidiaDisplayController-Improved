using System.Windows;
using Caliburn.Micro;
using FluentResults;
using NvidiaDisplayController.Interface.Help;
using NvidiaDisplayController.Interface.HotkeyCapture;
using NvidiaDisplayController.Objects.Entities;
using NvidiaDisplayController.Objects.Factories.Interfaces;

namespace NvidiaDisplayController.Global;

public class NvidiaDisplayWindowManager
{
    private readonly IHelpViewModelFactory _helpViewModelFactory;
    private readonly IHotkeyCaptureViewModelFactory _hotkeyCaptureViewModelFactory;
    private readonly IProfileNameViewModelFactory _profileNameViewModelFactory;
    private readonly IWindowManager _windowManager;

    public NvidiaDisplayWindowManager(
        IWindowManager windowManager,
        IHelpViewModelFactory helpViewModelFactory,
        IProfileNameViewModelFactory profileNameViewModelFactory,
        IHotkeyCaptureViewModelFactory hotkeyCaptureViewModelFactory)
    {
        _windowManager = windowManager;
        _helpViewModelFactory = helpViewModelFactory;
        _profileNameViewModelFactory = profileNameViewModelFactory;
        _hotkeyCaptureViewModelFactory = hotkeyCaptureViewModelFactory;
    }

    public void OpenHelp()
    {
        var viewModel = _helpViewModelFactory.Create();
        _windowManager.ShowDialogAsync(viewModel);
    }

    public void OpenWebsite(string urlString)
    {
        WebsiteLauncher.OpenWebsite(urlString);
    }

    public Result<string> OpenProfileNameViewModel()
    {
        var viewModel = _profileNameViewModelFactory.Create();
        var result = _windowManager.ShowDialogAsync(viewModel);
        return result.Result is true ? Result.Ok(viewModel.ProfileName) : Result.Fail("");
    }

    public Result<HotkeyBinding?> OpenHotkeyCaptureDialog()
    {
        var viewModel = _hotkeyCaptureViewModelFactory.Create();
        var result = _windowManager.ShowDialogAsync(viewModel);

        if (result.Result is true)
        {
            if (viewModel.WasCleared)
                return Result.Ok<HotkeyBinding?>(null);

            return Result.Ok(viewModel.Result);
        }

        return Result.Fail("");
    }

    public void ShowMessageBox(string message)
    {
        MessageBox.Show(message);
    }
}
