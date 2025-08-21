// TransitionEffect.cs
using System.Collections;
using UnityEngine;

public abstract class TransitionEffect : MonoBehaviour
{
    /// <summary>
    /// 播放轉場效果；請使用 UnscaledDeltaTime。
    /// return：等待此協程結束代表效果播放完成。
    /// </summary>
    public abstract IEnumerator Play();
}