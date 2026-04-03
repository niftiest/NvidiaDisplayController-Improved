using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NLog;
using NvidiaDisplayController.Objects.Entities;

namespace NvidiaDisplayController.Global.Controllers;

public static class HotkeyController
{
    private const int HotkeyIdBase = 9001;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private static readonly Dictionary<int, (uint Modifiers, uint VirtualKey)> IdToHotkey = new();
    private static int _nextId = HotkeyIdBase;

    public static void RegisterAll(IntPtr hwnd, IEnumerable<Profile> profiles, ILogger logger)
    {
        foreach (var profile in profiles)
        {
            if (profile.Hotkey is null)
                continue;

            var id = _nextId++;
            if (!RegisterHotKey(hwnd, id, profile.Hotkey.Modifiers, profile.Hotkey.VirtualKey))
            {
                logger.Warn($"Failed to register global hotkey {profile.Hotkey.DisplayName} for profile '{profile.Name}' " +
                            $"(id {id}). Another application may have registered it. Error: {Marshal.GetLastWin32Error()}");
                continue;
            }

            IdToHotkey[id] = (profile.Hotkey.Modifiers, profile.Hotkey.VirtualKey);
        }
    }

    public static void UnregisterAll(IntPtr hwnd)
    {
        foreach (var id in IdToHotkey.Keys)
            UnregisterHotKey(hwnd, id);

        IdToHotkey.Clear();
        _nextId = HotkeyIdBase;
    }

    public static void ReRegisterAll(IntPtr hwnd, IEnumerable<Profile> profiles, ILogger logger)
    {
        UnregisterAll(hwnd);
        RegisterAll(hwnd, profiles, logger);
    }

    public static bool TryGetHotkey(IntPtr wParam, out uint modifiers, out uint virtualKey)
    {
        var id = wParam.ToInt32();
        if (IdToHotkey.TryGetValue(id, out var hotkey))
        {
            modifiers = hotkey.Modifiers;
            virtualKey = hotkey.VirtualKey;
            return true;
        }

        modifiers = 0;
        virtualKey = 0;
        return false;
    }
}
