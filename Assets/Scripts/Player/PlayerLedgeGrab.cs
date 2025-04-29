using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLedgeGrab : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask ledgeLayer;
    [SerializeField] private float grabOffset = 0.5f; // 微調吸到邊的偏移
    [SerializeField] private float grabDetectionHeight = 1.2f; // 玩家高於這個點才能抓
    [SerializeField] private float ledgeCheckDistance = 0.5f; // 檢測前方距離
    [SerializeField] private float climbSpeed = 2f; // 抓牆時左右移動速度

    private Rigidbody rb;
    private Animator anim;
    private bool isGrabbing;
    private Collider currentCollider;
    
    [SerializeField] private MonoBehaviour inputSourceRef;
    private IPlayerInputSource input;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        input = inputSourceRef as IPlayerInputSource;
        
        if (input == null)
            Debug.LogError("Input source 不符合 IPlayerInputSource");
    }

    private void FixedUpdate()
    {
        if (isGrabbing)
        {
            HandleLedgeMovement();

            if ( Input.GetKey(KeyCode.Q))//input.JumpPressed)
            {
                ReleaseLedge();
            }
        }
        else
        {
            CheckForLedgeGrab();
        }
    }

    private void CheckForLedgeGrab()
    {
        Vector3 rayStart = transform.position + Vector3.up * grabDetectionHeight;
        Ray ray = new Ray(rayStart, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, ledgeCheckDistance, ledgeLayer))
        {
            StartLedgeGrab(hit.point);
        }
    }

    private void StartLedgeGrab(Vector3 ledgePoint)
    {
        isGrabbing = true;
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;
        transform.position = ledgePoint + Vector3.down * grabOffset;
        anim.SetBool("IsLedgeGrabbing", true);
    }

    private void HandleLedgeMovement()
    {
        /*Vector2 moveInput = input.MoveInput;
        Vector3 move = transform.right * moveInput.x * climbSpeed * Time.deltaTime;
        transform.position += move;*/
    }

    private IEnumerator ReleaseLedge()
    {
        isGrabbing = false;
        anim.SetBool("IsLedgeGrabbing", false);
        
        yield return new WaitForSeconds(2f);
        transform.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, (currentCollider.bounds.center.y + currentCollider.bounds.size.y * 0.5f + 0.03f),1), transform.position.z);
        transform.position = transform.position + transform.forward;
        rb.useGravity = true;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ledge"))
        {
            currentCollider = other.collider;
        }
    }
}
