// OptionsData.cs
using System.Collections.Generic;
using UnityEngine;

namespace UI.Pause
{
    public static class OptionsData
    {
        public static readonly List<Vector2Int> Resolutions = new()
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440)
        };

        public static readonly List<string> AspectRatios = new()
        {
            "16:9",
            "21:9",
            "4:3"
        };
    }
}