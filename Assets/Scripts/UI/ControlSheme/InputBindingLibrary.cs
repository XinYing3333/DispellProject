using System;
using System.Collections.Generic;
using DefaultNamespace.ControlSheme;
using UnityEngine;

[CreateAssetMenu(fileName = "InputBindingLibrary", menuName = "UI/InputBindingLibrary")]
public class InputBindingLibrary : ScriptableObject
{
    [Serializable]
    public struct BindingPair
    {
        public ActionName actionName; // 例如 "Jump", "Interact"
        public Sprite keyboardSprite;
        public Sprite gamepadSprite;
    }

    public List<BindingPair> bindings;

    public Sprite GetSprite(ActionName action, bool isGamepad)
    {
        var pair = bindings.Find(x => x.actionName == action);
        return isGamepad ? pair.gamepadSprite : pair.keyboardSprite;
    }
}