using SodaCraft.Localizations;
using UnityEngine;

// ReSharper disable All

namespace VTModifiers.VTLib;

public class VTModifiersUI : MonoBehaviour {
    private ModBehaviour mod;
    
    private bool show = false;

    private void Start() {
        if (this.mod == null) {
            this.mod = ModBehaviour.Instance;
        }
    }

    
    void Update() {
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) {
            if (Input.GetKeyDown(KeyCode.M)) {
                show = !show;
            }
        }
    }
    private Rect windowRect = new Rect(50, 50, 400, 250);
    private bool toggleDebug = true;
    private void OnGUI() {
        windowRect = GUILayout.Window(123, windowRect, WindowFunc, "VTModifier Mod Setting");
    }

    void WindowFunc(int id) {
        toggleDebug = GUI.Toggle (new Rect (25, 25, 100, 30), toggleDebug, "Debug");
        
    }
}