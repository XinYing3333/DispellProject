/*
// Assets/Editor/LevelSequenceTriggerEditor.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(LevelSequenceTrigger))]
public class LevelSequenceStepDrawer : Editor
{
    SerializedProperty playOnEnterProp, requiredTagProp, cooldownProp;
    SerializedProperty onlyOnceProp, persistIdProp, stepsProp;

    ReorderableList list;

    void OnEnable()
    {
        playOnEnterProp = serializedObject.FindProperty("playOnEnter");
        requiredTagProp = serializedObject.FindProperty("requiredTag");
        cooldownProp    = serializedObject.FindProperty("cooldown");
        onlyOnceProp    = serializedObject.FindProperty("onlyOnce");
        persistIdProp   = serializedObject.FindProperty("persistId");
        stepsProp       = serializedObject.FindProperty("steps");

        list = new ReorderableList(serializedObject, stepsProp, true, true, true, true);
        list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Steps (in order)");

        // 高度＆繪製都交給 PropertyDrawer(StepDrawer)
        list.elementHeightCallback = index =>
        {
            var el = stepsProp.GetArrayElementAtIndex(index);
            // +4 是上下 padding，防止卡框
            return EditorGUI.GetPropertyHeight(el, true) + 4f;
        };

        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var el = stepsProp.GetArrayElementAtIndex(index);
            var r  = new Rect(rect.x, rect.y + 2f, rect.width, rect.height - 4f);
            EditorGUI.PropertyField(r, el, GUIContent.none, true); // 這裡會觸發 StepDrawer
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Trigger", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(playOnEnterProp);
        EditorGUILayout.PropertyField(requiredTagProp);
        EditorGUILayout.PropertyField(cooldownProp);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("One-shot / Persistence", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(onlyOnceProp);
        EditorGUILayout.PropertyField(persistIdProp);

        EditorGUILayout.Space();

        list.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
*/
