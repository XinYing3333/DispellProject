#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 依 StepKind 顯示對應欄位；高度與繪製都用 Unity 的 GetPropertyHeight 來算，避免重疊
/// </summary>
[CustomPropertyDrawer(typeof(Step), true)]
public class StepDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float line = EditorGUIUtility.singleLineHeight;

        // 外框
        var box = new Rect(position.x, position.y, position.width, GetPropertyHeight(property, label));
        EditorGUI.HelpBox(box, GUIContent.none.text, MessageType.None);

        float x = position.x + 8f;
        float y = position.y + 6f;
        float w = position.width - 16f;

        // 標題
        EditorGUI.LabelField(new Rect(x, y, w, line), "Step");
        y += line + spacing;

        // kind
        var kindProp = property.FindPropertyRelative("kind");
        EditorGUI.PropertyField(new Rect(x, y, w, line), kindProp);
        y += line + spacing;

        var k = (StepKind)kindProp.enumValueIndex;

        // 共用小工具：畫一個序列化欄位並向下推 y
        void Draw(string name, string display = null, bool includeChildren = true)
        {
            var p = property.FindPropertyRelative(name);
            float h = EditorGUI.GetPropertyHeight(p, includeChildren);
            EditorGUI.PropertyField(new Rect(x, y, w, h), p,
                new GUIContent(display ?? ObjectNames.NicifyVariableName(name)), includeChildren);
            y += h + spacing;
        }

        switch (k)
        {
            case StepKind.LockMovement:
                Draw("boolValue", "Locked", false);
                break;

            case StepKind.Wait:
                Draw("seconds", "Seconds", false);
                break;

            case StepKind.SetObjective:
                Draw("text", "Text", true); // TextArea 需要 includeChildren=true 才能有正確高度
                break;

            case StepKind.ToggleObject:
                Draw("targetGO", "Target", false);
                Draw("boolValue", "Active", false);
                break;

            case StepKind.SetFlag:
                Draw("flagKey", "Flag Key", false);
                Draw("boolValue", "Value", false);
                break;

            case StepKind.StartDialogue:
                Draw("inkJSON", "Ink JSON", false);
                Draw("emoteAnimator", "Emote Animator (optional)", false);
                Draw("lockMoveDuringDialogue", "Lock Movement", false);
                break;

            case StepKind.PlayCutscene:
                Draw("director", "PlayableDirector", false);
                Draw("vcam", "VCam (optional)", false);
                Draw("skippable", "Skippable", false);
                break;

            case StepKind.PlaySFX:
                Draw("audioSource", "AudioSource", false);
                Draw("clipOverride", "Clip Override (optional)", false);
                Draw("volume", "Volume", false);
                break;
            case StepKind.PlayTutorial:
                Draw("clip", "Clip", false);
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float total = 0f;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float line = EditorGUIUtility.singleLineHeight;

        // 外框內的 padding + 標題 + kind 行
        total += 6f;
        total += line + spacing; // "Step"
        total += line + spacing; // kind

        var kind = (StepKind)property.FindPropertyRelative("kind").enumValueIndex;

        float H(string name, bool includeChildren = true)
        {
            var p = property.FindPropertyRelative(name);
            return EditorGUI.GetPropertyHeight(p, includeChildren) + spacing;
        }

        switch (kind)
        {
            case StepKind.LockMovement:
                total += H("boolValue", false);
                break;

            case StepKind.Wait:
                total += H("seconds", false);
                break;

            case StepKind.SetObjective:
                total += H("text", true);
                break;

            case StepKind.ToggleObject:
                total += H("targetGO", false) + H("boolValue", false);
                break;

            case StepKind.SetFlag:
                total += H("flagKey", false) + H("boolValue", false);
                break;

            case StepKind.StartDialogue:
                total += H("inkJSON", false) + H("emoteAnimator", false) + H("lockMoveDuringDialogue", false);
                break;

            case StepKind.PlayCutscene:
                total += H("director", false) + H("vcam", false) + H("skippable", false);
                break;

            case StepKind.PlaySFX:
                total += H("audioSource", false) + H("clipOverride", false) + H("volume", false);
                break;
            case StepKind.PlayTutorial:
                total += H("clip", false);
                break;
        }

        total += 6f; // 底部 padding
        return total;
    }
}
#endif