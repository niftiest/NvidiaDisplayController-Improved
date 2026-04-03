using System.Text;
using System.Windows.Input;
using Caliburn.Micro;
using NvidiaDisplayController.Objects.Entities;

namespace NvidiaDisplayController.Interface.HotkeyCapture;

public class HotkeyCaptureViewModel : Screen
{
    private const uint ModAlt = 1;
    private const uint ModControl = 2;
    private const uint ModShift = 4;

    private string _capturedKeyText = "Press a key combination...";
    private uint _modifiers;
    private uint _virtualKey;
    private bool _hasCapture;
    private bool _cleared;

    public override string DisplayName
    {
        get => "Set Hotkey";
        set { }
    }

    public string CapturedKeyText
    {
        get => _capturedKeyText;
        set
        {
            if (value == _capturedKeyText) return;
            _capturedKeyText = value;
            NotifyOfPropertyChange();
            NotifyOfPropertyChange(nameof(CanSave));
        }
    }

    public bool CanSave => _hasCapture;
    public bool WasCleared => _cleared;

    public uint Modifiers => _modifiers;
    public uint VirtualKey => _virtualKey;

    public HotkeyBinding? Result { get; private set; }

    public void OnPreviewKeyDown(Key key, ModifierKeys modifiers)
    {
        if (key == Key.System)
            key = Key.LeftAlt;

        if (IsModifierKey(key))
            return;

        _modifiers = 0;
        if ((modifiers & ModifierKeys.Control) != 0) _modifiers |= ModControl;
        if ((modifiers & ModifierKeys.Alt) != 0) _modifiers |= ModAlt;
        if ((modifiers & ModifierKeys.Shift) != 0) _modifiers |= ModShift;

        _virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        _hasCapture = true;

        CapturedKeyText = BuildDisplayName(_modifiers, key);
    }

    private static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin
            or Key.System;
    }

    private static string BuildDisplayName(uint modifiers, Key key)
    {
        var sb = new StringBuilder();
        if ((modifiers & ModControl) != 0) sb.Append("Ctrl+");
        if ((modifiers & ModAlt) != 0) sb.Append("Alt+");
        if ((modifiers & ModShift) != 0) sb.Append("Shift+");
        sb.Append(key.ToString());
        return sb.ToString();
    }

    public void Save()
    {
        if (_hasCapture)
        {
            Result = new HotkeyBinding(_modifiers, _virtualKey, CapturedKeyText);
        }

        TryCloseAsync(true);
    }

    public void Clear()
    {
        _cleared = true;
        Result = null;
        TryCloseAsync(true);
    }

    public void Cancel()
    {
        TryCloseAsync(false);
    }
}
