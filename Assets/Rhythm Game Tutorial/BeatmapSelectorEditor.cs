#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BeatmapSelector))]
public class BeatmapSelectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Add selected TextAssets as options"))
        {
            var selector = (BeatmapSelector)target;
            var selected = Selection.objects;
            int added = 0;

            Undo.RecordObject(selector, "Add Beatmap Options");

            var list = new System.Collections.Generic.List<BeatmapOption>(selector.options ?? new BeatmapOption[0]);

            foreach (var obj in selected)
            {
                if (obj is TextAsset ta)
                {
                    bool exists = list.Exists(o => o.chartFile == ta);
                    if (!exists)
                    {
                        var opt = new BeatmapOption { label = ta.name, chartFile = ta, audioClip = null, icon = null };
                        list.Add(opt);
                        added++;
                    }
                }
            }

            selector.options = list.ToArray();
            EditorUtility.SetDirty(selector);

            if (added > 0)
                EditorUtility.DisplayDialog("BeatmapSelector", $"Added {added} beatmap(s) to options.", "OK");
            else
                EditorUtility.DisplayDialog("BeatmapSelector", "No TextAsset selected or all selected charts already exist in options.", "OK");
        }

        if (GUILayout.Button("Clear all options"))
        {
            var selector = (BeatmapSelector)target;
            if (EditorUtility.DisplayDialog("Clear options", "Remove all beatmap options from this selector?", "Yes", "No"))
            {
                Undo.RecordObject(selector, "Clear Beatmap Options");
                selector.options = new BeatmapOption[0];
                EditorUtility.SetDirty(selector);
            }
        }
    }
}
#endif