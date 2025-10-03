using DialogSystem;
using UnityEngine;
using Player.InteractionSystem;
    
namespace Player
{
    public class PlayerInteractor
    {
        [SerializeField] Transform origin;
        [SerializeField] float range = 2.2f;
        [SerializeField] LayerMask mask;
        [SerializeField] KeyCode key = KeyCode.E;

        IInteractable _current;

        void Update()
        {
            //if (GameState.IsInputLocked) return;

            _current = FindInteractable();
            //UI_InteractHint.Show(_current?.Prompt);

            if (_current != null && Input.GetKeyDown(key))
                _current.Interact();
        }

        IInteractable FindInteractable()
        {
            if (Physics.Raycast(new Ray(origin.position, origin.forward), out var hit, range, mask))
                return hit.collider.GetComponentInParent<IInteractable>();
            return null;
        }
    }
}