using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YesterdayMap.Shelter;

[CustomEditor(typeof(GeneratorPowerManager))]
public sealed class GeneratorPowerManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GeneratorPowerManager manager = (GeneratorPowerManager)target;

        EditorGUILayout.Space(10f);
        EditorGUILayout.LabelField("Generator Power Test", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("0% / OFF"))
            {
                SetTestPower(manager, 0f);
            }

            if (GUILayout.Button("20% / FLICKER"))
            {
                SetTestPower(manager, 20f);
            }

            if (GUILayout.Button("100% / ON"))
            {
                SetTestPower(manager, 100f);
            }
        }

        string stateLabel = manager.CurrentState switch
        {
            GeneratorPowerManager.PowerState.Off => "OFF",
            GeneratorPowerManager.PowerState.Critical => "LOW POWER - FLICKER",
            _ => "NORMAL"
        };

        EditorGUILayout.HelpBox(
            $"Current Power: {manager.PowerPercent:0.#}%\nState: {stateLabel}\n" +
            (Application.isPlaying
                ? "The result is being applied to the wall lights now."
                : "Enter Play Mode to preview the light and flicker result."),
            MessageType.Info);
    }

    private static void SetTestPower(GeneratorPowerManager manager, float power)
    {
        Undo.RecordObject(manager, "Test generator power");
        manager.SetPowerPercent(power);
        EditorUtility.SetDirty(manager);

        if (!Application.isPlaying && manager.gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }

        SceneView.RepaintAll();
    }
}
