using UnityEngine;

namespace Events
{
    public class CheckpointSetter : MonoBehaviour
    {
        private Transform _playerCheckpoint;

        private void Awake()
        {
            _playerCheckpoint = GameObject.FindGameObjectWithTag("CheckPoint")?.transform;
            EventBus<Vector3>.Subscribe(OnSetCheckpoint);
        }

        private void OnDestroy()
        {
            EventBus<Vector3>.Unsubscribe(OnSetCheckpoint);
        }

        private void OnSetCheckpoint(Vector3 position)
        {
            if (_playerCheckpoint != null)
                _playerCheckpoint.position = position;
        }
    }
}