using Mobge;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CrashTester))]
public class CashTesterEditor : Editor {

    private ShortcutTools _shortcuts;
    private void OnEnable() {
        _shortcuts = new ShortcutTools();
        _shortcuts.AddTool(new ShortcutTools.Tool("test tool") {
            activation = new ShortcutTools.ActivationRule() {
                key = KeyCode.D,
            },
            onPress = () => true,
            onRelease = DeleteSelection,
        });
    }
    private void DeleteSelection() {
        if (EditorUtility.DisplayDialog("title", "message", "ok", "cancel")) {
            Debug.Log("ok");
        }
        else {
            Debug.Log("cancel");
        }
    }
    public void OnSceneGUI() {
        _shortcuts.HandleInput();
        Repaint();
        // if (Event.current.isKey) {
        //     if (EditorUtility.DisplayDialog("title", "message", "ok", "cancel")) {
        //         Debug.Log("ok");
        //     }
        //     else {
        //         Debug.Log("cancel");
        //     }
        // }
    }
}
