using Cinemachine;
using Player;
using UnityEngine;
using UnityEngine.InputSystem;


public class ThirdPersonShooterController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private float normalSensitivity;
    [SerializeField] private float aimSensitivity;

    private PlayerInputHandler _inputHandler;


    void Start()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
    }
    
    private void Update()
    {
        aimVirtualCamera.gameObject.SetActive(_inputHandler.IsAiming);
    }
}