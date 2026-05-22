using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Minimize, maximize/restore, and close the game window (Windows standalone) or quit the app.
/// </summary>
public static class GameWindowControls
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    const int SwMinimize = 6;
    const int SwMaximize = 3;
    const int SwRestore = 9;

    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool IsZoomed(IntPtr hWnd);
#endif

    public static void Minimize()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IntPtr hwnd = GetActiveWindow();
        if (hwnd != IntPtr.Zero)
            ShowWindow(hwnd, SwMinimize);
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
        // macOS: no public minimize API; lower window order is not exposed in Unity scripting.
        Debug.Log("[GameWindowControls] Minimize is only supported on Windows standalone builds.");
#else
        Debug.Log("[GameWindowControls] Minimize is only supported on Windows standalone builds.");
#endif
    }

    public static void ToggleMaximize()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        IntPtr hwnd = GetActiveWindow();
        if (hwnd == IntPtr.Zero)
            return;

        int command = IsZoomed(hwnd) ? SwRestore : SwMaximize;
        ShowWindow(hwnd, command);
#else
        Screen.fullScreen = !Screen.fullScreen;
#endif
    }

    public static void Close()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
