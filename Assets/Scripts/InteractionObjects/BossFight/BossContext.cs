using UnityEngine;

namespace BossFight
{
    public class BossContext
    {
        public Transform ModelRoot;
        public Transform Player;
        public Animator Anim;
        public BossServices Services;   // 封裝移動/貼地/傷害/射線/特效等
        public BossBirdController Owner; // 需要回呼時可用
    }
}