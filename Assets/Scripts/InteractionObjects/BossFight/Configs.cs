using System;
using UnityEngine;

namespace BossFight
{
    [Serializable]
    public class LandingConfig
    {
        [Header("Telegraph Settings")]
        public float telegraphTime = 0.8f;
        public float telegraphStartRadius = 2.6f;
        public float telegraphEndRadius = 0.8f;

        [Header("Movement Settings")]
        public float hoverHeight = 35f;
        public float riseSpeed = 20f;
        public float descendSpeed = 22f;
        public float stunDuration = 1.2f;

        [Header("Combat Settings")]
        public float landAoERadius = 2.6f;
        public int landAoEDamage = 1;

        [Header("Rock Spawning")]
        public GameObject rockPrefab;
        public int rockCount = 6;
        public float rockSpawnRadius = 3.5f;
    }
    
    [Serializable]
    public class ChargeConfig
    {
        public float windup = 0.45f;
        public float distance = 14f;
        public float speed = 28f;
        public float width = 1.6f;
        public int   damage = 1;
        public float recover = 0.6f;

        // 地貼參數（若你剛剛已做）
        public bool  stickToGround = true;
        public float groundOffset = 0.03f;
        public float probeUp = 50f;
        public float probeDown = 80f;
    }
}