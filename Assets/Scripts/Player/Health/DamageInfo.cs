using UnityEngine;


namespace Player
{
    public struct DamageInfo
    {
        public int amount;               // 以「格」計算（=心心的格數）
        public Vector3 hitDirection;     // 擊退方向（世界座標）
        public float knockbackForce;     // 擊退力度
        public bool bypassIFrames;       // 是否忽略無敵時間（少用）

        public DamageInfo(int amount, Vector3 dir, float force, bool bypass = false)
        {
            this.amount = amount;
            hitDirection = dir;
            knockbackForce = force;
            bypassIFrames = bypass;
        }
    }
}