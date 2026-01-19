using UnityEngine;

namespace BossFight
{
    [RequireComponent(typeof(Collider))]
    public class BossTrigger : MonoBehaviour
    {
        [SerializeField] private BossBirdController bossController;

        private void OnTriggerEnter(Collider other)
        {
            bossController?.HandleHitTrigger(other);
        }
    }
}