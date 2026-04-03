namespace NvidiaDisplayController.Objects.Entities;

public class HotkeyBinding
{
    public uint Modifiers { get; set; }
    public uint VirtualKey { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public HotkeyBinding()
    {
    }

    public HotkeyBinding(uint modifiers, uint virtualKey, string displayName)
    {
        Modifiers = modifiers;
        VirtualKey = virtualKey;
        DisplayName = displayName;
    }
}
