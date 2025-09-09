using UnityEditor;
using UnityEngine;
using Events;

[CustomEditor(typeof(TriggerEventData))]
public class TriggerEventDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TriggerEventData data = (TriggerEventData)target;

        // 繪製 enum 選擇器
        data.eventType = (TriggerEventData.EventType)EditorGUILayout.EnumPopup("Event Type", data.eventType);

        EditorGUILayout.Space();

        // 根據不同類型顯示對應欄位
        switch (data.eventType)
        {
            case TriggerEventData.EventType.ChangeCamera:
                data.cameraToActivate = (Cinemachine.CinemachineVirtualCamera)EditorGUILayout.ObjectField("Camera To Activate", data.cameraToActivate, typeof(Cinemachine.CinemachineVirtualCamera), true);
                break;

            case TriggerEventData.EventType.ShowUIText:
                data.uiText = EditorGUILayout.TextArea(data.uiText, GUILayout.Height(60));
                data.displayTime = EditorGUILayout.FloatField("Display Time", data.displayTime);
                break;

            case TriggerEventData.EventType.PlaySound:
                data.soundToPlay = (AudioClip)EditorGUILayout.ObjectField("Sound To Play", data.soundToPlay, typeof(AudioClip), false);
                break;

            case TriggerEventData.EventType.OpenGameObject:
                data.objectToOpen = (GameObject)EditorGUILayout.ObjectField("Object To Open", data.objectToOpen,typeof(GameObject));
                break;
            
            case TriggerEventData.EventType.GainPower:
                EditorGUILayout.LabelField("No additional data required for GainPower.");
                break;
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }
    }
}