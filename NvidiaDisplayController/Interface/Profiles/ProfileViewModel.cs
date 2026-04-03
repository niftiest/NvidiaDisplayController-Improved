using System;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using NvidiaDisplayController.Interface.Monitors;
using NvidiaDisplayController.Interface.ProfileSettings;
using NvidiaDisplayController.Objects.Entities;
using NvidiaDisplayController.Objects.Factories.Interfaces;

namespace NvidiaDisplayController.Interface.Profiles;

public class ProfileViewModel : Screen
{
    private readonly IProfileSettingViewModelFactory _profileSettingViewModelFactory;
    private bool _callEvent;
    private bool _isSelected;
    private ProfileSettingViewModel? _profileSettings;

    public ProfileViewModel(Profile profile, MonitorViewModel monitorViewModel,
        IProfileSettingViewModelFactory profileSettingViewModelFactory)
    {
        _profileSettingViewModelFactory = profileSettingViewModelFactory;
        Profile = profile;
        MonitorViewModel = monitorViewModel;

        Start();
    }

    public Profile Profile { get; }
    public Action<Guid> ProfileRemoved { get; set; } = null!;
    public Action<ProfileViewModel>? SetHotkeyRequested { get; set; }
    public Action<ProfileViewModel>? ClearHotkeyRequested { get; set; }
    public string Name => Profile.Name;
    public Guid Guid { get; set; }

    public string HotkeyDisplayName => Profile.Hotkey?.DisplayName ?? string.Empty;
    public bool HasHotkey => Profile.Hotkey is not null;

    public ProfileSettingViewModel? ProfileSettings
    {
        get => _profileSettings;
        set
        {
            if (Equals(value, _profileSettings)) return;
            _profileSettings = value;
            NotifyOfPropertyChange();
        }
    }

    public ContextMenu ContextMenu { get; set; } = null!;
    public MonitorViewModel MonitorViewModel { get; set; }

    public new bool IsActive
    {
        get => Profile.IsActive;
        set
        {
            if (value == Profile.IsActive) return;
            Profile.IsActive = value;
            NotifyOfPropertyChange();
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (value == _isSelected) return;
            _isSelected = value;
            NotifyOfPropertyChange();
            if (_callEvent)
            {
                BuildProfileSettings();
                IsSelectedChanged.Invoke(Guid, value);
            }
        }
    }

    public Action<Guid, bool> IsSelectedChanged { get; set; } = null!;
    public bool IsDefault => Profile.IsDefault;

    private void Start()
    {
        Guid = Guid.NewGuid();
        _callEvent = true;

        CreateContextMenu();
        BuildProfileSettings();
    }

    private void BuildProfileSettings()
    {
        ProfileSettings = _profileSettingViewModelFactory
            .Create(Profile.ProfileSetting, Profile.IsDefault);
    }

    public void IsUpdated()
    {
        ProfileSettings?.IsUpdated();
    }

    private void CreateContextMenu()
    {
        ContextMenu = new ContextMenu();

        var setHotkeyItem = new MenuItem { Header = "Set Hotkey..." };
        setHotkeyItem.Click += OnSetHotkeyClicked;
        ContextMenu.Items.Add(setHotkeyItem);

        if (HasHotkey)
        {
            var clearHotkeyItem = new MenuItem { Header = "Clear Hotkey" };
            clearHotkeyItem.Click += OnClearHotkeyClicked;
            ContextMenu.Items.Add(clearHotkeyItem);
        }

        if (!Profile.IsDefault)
        {
            ContextMenu.Items.Add(new Separator());
            var removeItem = new MenuItem { Header = "Remove" };
            removeItem.Click += OnRemoveClicked;
            ContextMenu.Items.Add(removeItem);
        }
    }

    private void OnSetHotkeyClicked(object sender, RoutedEventArgs e)
    {
        SetHotkeyRequested?.Invoke(this);
    }

    private void OnClearHotkeyClicked(object sender, RoutedEventArgs e)
    {
        ClearHotkeyRequested?.Invoke(this);
    }

    private void OnRemoveClicked(object sender, RoutedEventArgs e)
    {
        ProfileRemoved.Invoke(Guid);
    }

    public void RefreshHotkey()
    {
        NotifyOfPropertyChange(nameof(HotkeyDisplayName));
        NotifyOfPropertyChange(nameof(HasHotkey));
        CreateContextMenu();
        NotifyOfPropertyChange(nameof(ContextMenu));
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void UnSelect()
    {
        _callEvent = false;
        {
            IsSelected = false;
            ProfileSettings = null;
        }
        _callEvent = true;
    }
}
