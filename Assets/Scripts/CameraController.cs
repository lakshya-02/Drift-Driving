using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    private Rigidbody playerRB;

    public enum CameraMode { ThirdPerson, FirstPerson }
    public CameraMode currentMode = CameraMode.ThirdPerson;

    [Header("Camera Positioning")]
    public Vector3 thirdPersonOffset = new Vector3(0f, 3.135f, -6f);
    public Vector3 reverseOffset = new Vector3(0f, 3.135f, 6f);
    public Vector3 firstPersonOffset = new Vector3(0f, 1.2f, 0.5f);
    public float smoothTime = 0.15f;

    [Header("Camera LookAt")]
    public Vector3 lookAtOffset = new Vector3(0f, 1.635f, 0f);

    private Vector3 velocity = Vector3.zero;
    private Vector3 currentOffset;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("Player transform not assigned in CameraController.", this);
            this.enabled = false;
            return;
        }
        playerRB = player.GetComponent<Rigidbody>();
        if (playerRB == null)
        {
            Debug.LogError("Player Rigidbody not found by CameraController.", this);
            this.enabled = false;
            return;
        }

        currentOffset = thirdPersonOffset;
        transform.position = player.position + player.transform.TransformDirection(currentOffset);
        transform.LookAt(player.position + lookAtOffset);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (currentMode == CameraMode.ThirdPerson)
                currentMode = CameraMode.FirstPerson;
            else
                currentMode = CameraMode.ThirdPerson;
        }
    }

    void FixedUpdate()
    {
        if (currentMode == CameraMode.ThirdPerson)
        {
            float movingDirection = Vector3.Dot(player.forward, playerRB.velocity);
            Vector3 targetOffset = movingDirection < -0.1f ? reverseOffset : thirdPersonOffset;
            currentOffset = Vector3.SmoothDamp(currentOffset, targetOffset, ref velocity, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);
        }
    }

    void LateUpdate()
    {
        switch (currentMode)
        {
            case CameraMode.ThirdPerson:
                Vector3 desiredPosition = player.position + player.transform.TransformDirection(currentOffset);
                transform.position = desiredPosition;
                transform.LookAt(player.position + lookAtOffset);
                break;
            case CameraMode.FirstPerson:
                Vector3 fpPosition = player.position + player.transform.TransformDirection(firstPersonOffset);
                transform.position = fpPosition;
                transform.rotation = player.rotation;
                break;
        }
    }
}