// CameraPresets.cs
using UnityEngine;

/// <summary>
/// Einfache Datenstruktur für eine Kameraposition.
/// </summary>
[System.Serializable]
public struct CameraPreset
{
    public string name;
    public Vector3 position;
    public Vector3 rotation;    // Euler-Winkel in Grad
    public bool orthographic;   // true = ISO/Top‑View, false = Perspektive
}

/// <summary>
/// Statische Liste aller Presets – zum Auskommentieren/Einfügen im Code.
/// </summary>
public static class CameraPresets
{
    public static readonly CameraPreset[] presets = new CameraPreset[]
    {
        new CameraPreset { name = "Standard",                         position = new Vector3(13.38f, 10.07f, -8.35f), rotation = new Vector3(45f,    -45f,   0f), orthographic = true  },
        new CameraPreset { name = "Gesamt hinten",                    position = new Vector3(-5.21f,  11f,     6.2f), rotation = new Vector3(45f,    135f,   0f), orthographic = true  },
        new CameraPreset { name = "Brennofen vorne",                  position = new Vector3(6.67f, 4.28f,  -7.55f), rotation = new Vector3(30f,  -90f,   0f), orthographic = false },
        new CameraPreset { name = "Brennofen hinten",                 position = new Vector3(-6.023f, 2.06f,  -9.34f), rotation = new Vector3(10.57f,  64.2f,   0f), orthographic = false },
        new CameraPreset { name = "Multi gesamt vorne",               position = new Vector3(8.21f,3.01f,-5.13f), rotation = new Vector3(17.3f, -90, 0), orthographic = false },
        new CameraPreset { name = "Drehscheibe vorne",                position = new Vector3(4.16f,  6.37f,   -5.28f), rotation = new Vector3(45f, -45f,   0f), orthographic = false },
        new CameraPreset { name = "Drehscheibe hinten",               position = new Vector3(-5.86f,  4.09f,   3.75f), rotation = new Vector3(26.87f, -239f,   0f), orthographic = false },
        new CameraPreset { name = "Drehscheibe Seite",                position = new Vector3(5.3f,    7.51f,   4.32f), rotation = new Vector3(45f,   -139.5f,  0f), orthographic = false },
        new CameraPreset { name = "Sortierstrecke frontal",           position = new Vector3(10.12f,  5f,     -5.42f), rotation = new Vector3(36.67f,    0f,   0f), orthographic = false },
        new CameraPreset { name = "Sortierstrecke Seite",             position = new Vector3(21.2f,  6.88f,  -1.64f), rotation = new Vector3(33.97f,  -90f,   0f), orthographic = false },
        // new CameraPreset { name = "Sortierstrecke hinten",            position = new Vector3(10.5f,   6.82f,   9.44f), rotation = new Vector3(45f,   -180f,   0f), orthographic = false },
        new CameraPreset { name = "Topdown Gesamt",                   position = new Vector3(3.78f,  17.1f,  -2.83f), rotation = new Vector3(90f,      0f,   0f), orthographic = true  },
        // new CameraPreset { name = "Topdown Multiprocess",             position = new Vector3(-0.583f,11.83f, -3.32f), rotation = new Vector3(90f,    -90f,   0f), orthographic = true  },,
        // new CameraPreset { name = "Topdown Sortierstrecke",           position = new Vector3(10.53f,  8.84f,   2.21f), rotation = new Vector3(90f,      0f,   0f), orthographic = true  },
    };
}
