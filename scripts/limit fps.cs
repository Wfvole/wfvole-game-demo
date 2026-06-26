using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class limit_fps : UnitySingleton<limit_fps>
{
    public enum Limitfpstype
    {
        limit30fps = 30,
        limit60fps = 60,
        limit90fps = 90,
        limit120fps = 120,
        nomlimit = -1
    }
    public Limitfpstype limitfpstype;
    private void OnEnable()
    {
        Application.targetFrameRate = (int)limitfpstype;
    }
    public void ResetFPS()
    {
        Application.targetFrameRate = (int)limitfpstype;
    }
}
