using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CrashTester))]
public class CashTesterEditor : Editor {
    public void OnSceneGUI() {
        if (Event.current.isKey) {
            if (EditorUtility.DisplayDialog("title", "message", "ok", "cancel")) {
                Debug.Log("ok");
            }
            else {
                Debug.Log("cancel");
            }
        }
    }
}
