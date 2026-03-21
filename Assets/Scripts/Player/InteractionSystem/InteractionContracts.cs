using DefaultNamespace.Thought;
using UnityEngine;

namespace Player.InteractionSystem
{
    public interface ICollectable
    {
        // 吸收到玩家時呼叫（自行決定存檔、加分…）
        void Collect();
        bool NeedCollectAnimation { get; } // 新增：是否立即收集
        bool IsSpellStateActive { get; }
    }

    public interface IMagnetAttachable
    {
        // 吸到手上/從手上離開
        bool CanAttach { get; } // 新增：是否可被吸附的檢查
        void OnMagnetAttached(Transform parent);
        void OnMagnetDetached();
    }

    public interface IThrowable
    {
        // 被拋前通知（可在這裡開啟重力/碰撞等）
        void OnBeforeThrow();
    }
    public interface IHitReceiver
    {
        void OnHit(ThoughtPayloadSO payload);
    }
}