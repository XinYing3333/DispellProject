using UnityEngine;
using Ink.Runtime;

namespace DialogSystem
{
    public class InkExternalFunctions
    {
        // 記錄這個工具是否已對當前 Story 綁定過（最小改動：不動 DialogueManager 的邏輯）
        private bool _bound = false;

        public void Bind(Story story, Animator emoteAnimator)
        {
            if (story == null) return;
            if (_bound) return; // 已綁就不重複綁

            // ★ 你的 Ink 版本沒有 Action<T> 多載，要用 Func<T, object> 並回傳 null
            story.BindExternalFunction<string>("playEmote", (string emoteName) =>
            {
                PlayEmote(emoteName, emoteAnimator);
                return null; // 必須回傳 object；這裡沒用所以回傳 null
            });

            _bound = true;
        }

        public void Unbind(Story story) 
        {
            if (story == null) return;
            if (!_bound) return; // 沒綁過就不解綁，避免 has-not-been-bound 例外

            try
            {
                story.UnbindExternalFunction("playEmote");
            }
            catch (System.Exception e)
            {
                // 就算外部已解除或 Story 已重建，也不要讓流程炸掉
                Debug.LogWarning($"[InkExternalFunctions] Unbind ignored: {e.Message}");
            }

            _bound = false;
        }

        public void PlayEmote(string emoteName, Animator emoteAnimator)
        {
            if (string.IsNullOrEmpty(emoteName))
            {
                Debug.LogWarning("[InkExternalFunctions] emoteName is null/empty.");
                return;
            }

            if (emoteAnimator != null) 
            {
                emoteAnimator.Play(emoteName, 0, 0f);
                // Debug.Log("PlayEmote called: " + emoteName);
            }
            else 
            {
                Debug.LogWarning("Tried to play emote, but emote animator was not initialized when entering dialogue mode.");
            }
        }
    }
}