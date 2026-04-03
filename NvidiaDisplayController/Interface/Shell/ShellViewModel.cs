using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using NLog;
using NvAPIWrapper.Display;
using NvAPIWrapper.GPU;
using NvidiaDisplayController.Global;
using NvidiaDisplayController.Global.Controllers;
using NvidiaDisplayController.Global.Extensions;
using NvidiaDisplayController.Interface.Monitors;
using NvidiaDisplayController.Interface.Profiles;
using NvidiaDisplayController.Objects.Entities;
using NvidiaDisplayController.Objects.Factories;
using NvidiaDisplayController.Objects.Factories.Interfaces;
using NvidiaDisplayController.Objects.HandleEvents;
using Monitor = NvidiaDisplayController.Objects.Entities.Monitor;

namespace NvidiaDisplayController.Interface.Shell;

public class ShellViewModel : Conductor<IScreen>, IHandle<ProfileSettingsEvent>
{
    private readonly DataController _dataController;
    private readonly DisplayController _displayController;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger _logger;
    private readonly MonitorViewModelFactory _monitorViewModelFactory;

    private readonly NvidiaDisplayWindowManager _nvidiaDisplayWindowManager;
    private readonly ProfileFactory _profileFactory;
    private readonly IProfileViewModelFactory _profileViewModelFactory;

    private readonly RegistryController _registryController;
    private Computer _computer = null!;
    private ObservableCollection<MonitorViewModel> _monitors = null!;
    private List<Display>? _nvidiaDisplays;
    private bool _profileSettingsIsDirty;
    private MonitorViewModel? _selectedMonitor;
    private Display? _selectedNvidiaMonitor;
    private ProfileViewModel? _selectedProfile;

    public ShellViewModel(
        IEventAggregator eventAggregator,
        MonitorViewModelFactory monitorViewModelFactory,
        DataController dataController,
        IProfileViewModelFactory profileViewModelFactory,
        ProfileFactory profileFactory,
        ILogger logger,
        DisplayController displayController,
        NvidiaDisplayWindowManager nvidiaDisplayWindowManager,
        RegistryController registryController)
    {
        _eventAggregator = eventAggregator;
        _monitorViewModelFactory = monitorViewModelFactory;
        _dataController = dataController;
        _profileViewModelFactory = profileViewModelFactory;
        _profileFactory = profileFactory;
        _logger = logger;
        _displayController = displayController;
        _nvidiaDisplayWindowManager = nvidiaDisplayWindowManager;
        _registryController = registryController;

        _eventAggregator.SubscribeOnPublishedThread(this);

        Start();
    }

    public ObservableCollection<MonitorViewModel> Monitors
    {
        get => _monitors;
        set
        {
            if (Equals(value, _monitors)) return;
            _monitors = value;
            NotifyOfPropertyChange();
        }
    }

    public override string DisplayName
    {
        get
        {
            var gpus = PhysicalGPU.GetPhysicalGPUs();
            return $"Adjust Displays - ({gpus[0].FullName})";
        }
        set { }
    }

    public MonitorViewModel? SelectedMonitor
    {
        get => _selectedMonitor;
        set
        {
            if (Equals(value, _selectedMonitor)) return;
            _selectedMonitor = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(SelectedProfile));
            NotifyOfPropertyChange(nameof(CanAddProfile));
            NotifyOfPropertyChange(nameof(ProfileGroupBoxText));
        }
    }

    public ProfileViewModel? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (Equals(value, _selectedProfile)) return;
            _selectedProfile = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanApply));
            NotifyOfPropertyChange(nameof(CanSetHotkey));
        }
    }

    public bool ProfileSettingsIsDirty
    {
        get => _profileSettingsIsDirty;
        set
        {
            if (value == _profileSettingsIsDirty) return;
            _profileSettingsIsDirty = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanApply));
        }
    }

    public bool CanApply => SelectedProfile?.ProfileSettings != null;
    public bool CanSetHotkey => SelectedProfile is not null;
    public bool CanAddProfile => SelectedMonitor is not null && SelectedMonitor.Profiles.Count < 5;

    public bool IsStartWithWindows
    {
        get => Computer.IsStartWithWindows;
        set
        {
            if (value == Computer.IsStartWithWindows) return;
            Computer.IsStartWithWindows = value;
            NotifyOfPropertyChange();
            Write();
            OnIsStartWithWindowsChanged();
        }
    }

    public bool IsApplySettingsOnStart
    {
        get => Computer.IsApplySettingsOnStart;
        set
        {
            if (value == Computer.IsApplySettingsOnStart) return;
            Computer.IsApplySettingsOnStart = value;
            NotifyOfPropertyChange();
            Write();
        }
    }

    public Display? SelectedNvidiaMonitor
    {
        get => _selectedNvidiaMonitor;
        set
        {
            if (Equals(value, _selectedNvidiaMonitor)) return;
            _selectedNvidiaMonitor = value;
            NotifyOfPropertyChange();
        }
    }

    public Computer Computer
    {
        get => _computer;
        set
        {
            if (Equals(value, _computer)) return;
            _computer = value;
            NotifyOfPropertyChange();
        }
    }

    public string ProfileGroupBoxText =>
        $"Profiles [{(SelectedMonitor == null ? 0 : SelectedMonitor.Profiles.Count)}/5]";

    private static string PaypalLink => "https://www.paypal.com/donate/?hosted_button_id=FT6HS8V8R4XYC";

    public Task HandleAsync(ProfileSettingsEvent message, CancellationToken cancellationToken)
    {
        ProfileSettingsIsDirty = message.IsDirty;
        return Task.CompletedTask;
    }

    private void OnIsStartWithWindowsChanged()
    {
        _registryController.RegisterForStartWithWindows(IsStartWithWindows);
    }

    private void Start()
    {
        _monitors = new ObservableCollection<MonitorViewModel>();

        _dataController
            .Load()
            .IfSuccess(computer =>
            {
                Computer = computer;
                Computer.Monitors.ForEach(BuildMonitorViewModel);
            })
            .Do(_ => LoadNvidiaDisplays())
            .Do(_ => ApplySettingsOnStart());
    }

    private void BuildMonitorViewModel(Monitor monitor)
    {
        _monitorViewModelFactory
            .Create(monitor)
            .IfSuccess(monitorViewModel =>
            {
                monitorViewModel.Profiles.ForEach(WireProfileEvents);
                monitorViewModel.IsSelectedChanged += OnMonitorViewModelIsSelectedChanged;

                Monitors.Add(monitorViewModel);
            });
    }

    private void LoadNvidiaDisplays()
    {
        try
        {
            _nvidiaDisplays = Display.GetDisplays().ToList();
        }
        catch (Exception e)
        {
            _logger.Error(e);
            _nvidiaDisplayWindowManager
                .ShowMessageBox("Failed to load displays connected to GPU. " +
                                "Make sure screen is not being duplicated and or is connected to GPU. " +
                                "Some features may not function properly.");
        }
    }

    private void ApplySettingsOnStart()
    {
        if (!IsApplySettingsOnStart)
            return;

        foreach (var monitorViewModel in Monitors)
        {
            var activeProfile = monitorViewModel.Profiles.SingleOrDefault(p => p.IsActive);
            var nvidiaDisplay = _nvidiaDisplays?.SingleOrDefault(d => d.Name == monitorViewModel.ScreenName);

            if (activeProfile is not null)
                _displayController.UpdateColorSettings(
                    monitorViewModel.Display,
                    activeProfile.ProfileSettings!.ProfileSetting,
                    nvidiaDisplay);
        }
    }

    private void WireProfileEvents(ProfileViewModel profileViewModel)
    {
        profileViewModel.IsSelectedChanged += OnProfileViewModelSelectedChanged;
        profileViewModel.ProfileRemoved += OnProfileRemoved;
        profileViewModel.SetHotkeyRequested += OnSetHotkeyRequested;
        profileViewModel.ClearHotkeyRequested += OnClearHotkeyRequested;
    }

    private void OnSetHotkeyRequested(ProfileViewModel profileViewModel)
    {
        _nvidiaDisplayWindowManager
            .OpenHotkeyCaptureDialog()
            .IfSuccess(hotkeyBinding =>
            {
                if (hotkeyBinding is null)
                {
                    profileViewModel.Profile.Hotkey = null;
                    profileViewModel.RefreshHotkey();
                    Write();
                    GlobalEvents.ReRegisterHotkeys?.Invoke();
                    return;
                }

                var conflict = FindHotkeyConflict(hotkeyBinding, profileViewModel.Profile);
                if (conflict is not null)
                {
                    var monitorName = conflict.Monitor.Name;
                    _nvidiaDisplayWindowManager.ShowMessageBox(
                        $"Hotkey {hotkeyBinding.DisplayName} is already assigned to profile '{conflict.Name}' on monitor '{monitorName}'.");
                    return;
                }

                profileViewModel.Profile.Hotkey = hotkeyBinding;
                profileViewModel.RefreshHotkey();
                Write();
                GlobalEvents.ReRegisterHotkeys?.Invoke();
            });
    }

    private void OnClearHotkeyRequested(ProfileViewModel profileViewModel)
    {
        profileViewModel.Profile.Hotkey = null;
        profileViewModel.RefreshHotkey();
        Write();
        GlobalEvents.ReRegisterHotkeys?.Invoke();
    }

    private Profile? FindHotkeyConflict(HotkeyBinding hotkey, Profile excludeProfile)
    {
        foreach (var monitor in Computer.Monitors)
        {
            foreach (var profile in monitor.Profiles)
            {
                if (profile == excludeProfile)
                    continue;

                if (profile.Hotkey is not null &&
                    profile.Hotkey.Modifiers == hotkey.Modifiers &&
                    profile.Hotkey.VirtualKey == hotkey.VirtualKey)
                {
                    return profile;
                }
            }
        }

        return null;
    }

    private void OnProfileViewModelSelectedChanged(Guid guid, bool value)
    {
        if (value)
        {
            SelectedProfile = SelectedMonitor!.Profiles.Single(p => p.Guid == guid);
            foreach (var profileViewModel in SelectedMonitor.Profiles.Where(p => p.Guid != guid))
                profileViewModel.UnSelect();
        }
        else
        {
            foreach (var profileViewModel in SelectedMonitor!.Profiles)
                profileViewModel.UnSelect();
        }

        ProfileSettingsIsDirty = false;
        NotifyOfPropertyChange(nameof(CanApply));
    }

    private void OnProfileRemoved(Guid guid)
    {
        var profileViewModel = SelectedMonitor!.Profiles.Single(p => p.Guid == guid);

        SelectedMonitor.Monitor.Profiles.Remove(profileViewModel.Profile);
        SelectedMonitor?.Profiles.Remove(profileViewModel);

        NotifyOfPropertyChange(nameof(CanAddProfile));
        NotifyOfPropertyChange(nameof(ProfileGroupBoxText));

        Write();
        GlobalEvents.ReRegisterHotkeys?.Invoke();
    }

    private void Write()
    {
        _dataController.Write(Computer);
    }

    private void OnMonitorViewModelIsSelectedChanged(bool isSelected, Guid selectedMonitor)
    {
        if (SelectedMonitor != null)
            SelectedMonitor.IsSelected = false;

        SelectedMonitor = isSelected ? Monitors.Single(m => m.Guid == selectedMonitor) : null;
        SelectedNvidiaMonitor = _nvidiaDisplays?.SingleOrDefault(d => d.Name == SelectedMonitor?.ScreenName);

        SetSelectedProfile();
    }

    private void SetSelectedProfile()
    {
        SelectedProfile = SelectedMonitor?.Profiles.SingleOrDefault(p => p.IsActive);
        if (SelectedProfile is not null)
            SelectedProfile.IsSelected = true;
    }

    public void AddProfile()
    {
        _nvidiaDisplayWindowManager
            .OpenProfileNameViewModel()
            .IfSuccess(profileName =>
            {
                var profile = _profileFactory.Create(SelectedMonitor!.Monitor, profileName);
                var profileViewModel = _profileViewModelFactory.Create(profile, SelectedMonitor);

                WireProfileEvents(profileViewModel);

                SelectedMonitor?.Profiles.Add(profileViewModel);
                SelectedProfile = profileViewModel;
                SelectedProfile.IsSelected = true;

                NotifyOfPropertyChange(nameof(CanAddProfile));
                NotifyOfPropertyChange(nameof(ProfileGroupBoxText));

                Write();
            });
    }

    public void Apply()
    {
        _displayController.UpdateColorSettings(
            SelectedMonitor!.Display,
            SelectedProfile!.ProfileSettings!.ProfileSetting,
            SelectedNvidiaMonitor);

        SetActiveProfile();
        Write();

        ProfileSettingsIsDirty = false;
        GlobalEvents.UpdateToolTip.Invoke();
    }

    private void SetActiveProfile()
    {
        SelectedProfile!.IsActive = true;
        foreach (var profileViewModel in SelectedMonitor!.Profiles.Where(p => p.Guid != SelectedProfile.Guid))
            profileViewModel.Deactivate();
    }

    public void Revert()
    {
        Task.Run(async () => await _eventAggregator.PublishOnCurrentThreadAsync(new RevertEvent()));
    }

    public void Update()
    {
        Write();

        SelectedProfile!.IsUpdated();
        ProfileSettingsIsDirty = false;
    }

    public void SetHotkey()
    {
        if (SelectedProfile is null)
            return;

        OnSetHotkeyRequested(SelectedProfile);
    }

    public void OpenHelp()
    {
        _nvidiaDisplayWindowManager.OpenHelp();
    }

    public void OpenDonation()
    {
        _nvidiaDisplayWindowManager.OpenWebsite(PaypalLink);
    }

    public void ApplyProfileByHotkey(uint modifiers, uint virtualKey)
    {
        ProfileViewModel? profileViewModel = null;
        MonitorViewModel? monitorViewModel = null;

        foreach (var monitor in Monitors)
        {
            foreach (var p in monitor.Profiles)
            {
                if (p.Profile.Hotkey is not null &&
                    p.Profile.Hotkey.Modifiers == modifiers &&
                    p.Profile.Hotkey.VirtualKey == virtualKey)
                {
                    profileViewModel = p;
                    monitorViewModel = monitor;
                    break;
                }
            }

            if (profileViewModel is not null)
                break;
        }

        if (profileViewModel is null || monitorViewModel is null)
            return;

        // If the requested profile is already active, toggle back to the default (first) profile
        if (profileViewModel.IsActive && !profileViewModel.IsDefault && monitorViewModel.Profiles.Count > 0)
            profileViewModel = monitorViewModel.Profiles[0];

        var nvidiaDisplay = _nvidiaDisplays?.SingleOrDefault(d => d.Name == monitorViewModel.ScreenName);

        // Use the entity's ProfileSetting directly — the ViewModel's ProfileSettings
        // may be null if this profile isn't currently selected in the UI
        _displayController.UpdateColorSettings(
            monitorViewModel.Display,
            profileViewModel.Profile.ProfileSetting,
            nvidiaDisplay);

        // Update active state for this monitor's profiles
        profileViewModel.IsActive = true;
        foreach (var p in monitorViewModel.Profiles.Where(p => p != profileViewModel))
            p.Deactivate();

        Write();
        GlobalEvents.UpdateToolTip.Invoke();
    }
}
