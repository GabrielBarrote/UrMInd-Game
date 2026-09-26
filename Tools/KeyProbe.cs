using UnityEngine;
using UnityEngine.InputSystem;

public static class KeyProbe
{
    public static string Run()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return "Keyboard.current NULO (sem foco)";
        return "W=" + kb.wKey.isPressed + " S=" + kb.sKey.isPressed
            + " A=" + kb.aKey.isPressed + " D=" + kb.dKey.isPressed
            + " Space=" + kb.spaceKey.isPressed
            + " | appFocused=" + Application.isFocused
            + " | runInBackground=" + Application.runInBackground;
    }
}
