using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    private Rigidbody playerRB;
    
    public enum CameraMode { ThirdPerson, FirstPerson }

    [Header("Camera Modes")]
    public CameraMode currentMode = CameraMode.ThirdPerson;

    [Header("Camera Positioning")]
    public Vector3 thirdPersonOffset = new Vector3(0f, 3.135f, -6f);
    // New, more realistic offset for a driver's POV. Adjust these values in the Inspector.
    public Vector3 firstPersonOffset = new Vector3(0f, 1.2f, 0.5f);
    public float smoothTime = 0.15f;

    [Header("Camera LookAt")]
    public Vector3 lookAtOffset = new Vector3(0f, 1.635f, 0f); // Adjusted Y (+0.635)

    private Vector3 currentOffset;
    private Vector3 velocity = Vector3.zero;

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

        // Initialize camera position to avoid fluctuations
        currentOffset = thirdPersonOffset;
        transform.position = player.position + player.transform.TransformDirection(currentOffset);
        transform.LookAt(player.position + lookAtOffset);
    }

    void Update()
    {
        HandleCameraModeSwitching();
    }

    void FixedUpdate()
    {
        HandleCameraPosition();
    }

    void HandleCameraModeSwitching()
    {
        // Switch to First-Person POV on pressing C
        if (Input.GetKeyDown(KeyCode.C))
        {
            currentMode = CameraMode.FirstPerson;
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            currentMode = CameraMode.ThirdPerson;
        }
    }

    void HandleCameraPosition()
    {
        switch (currentMode)
        {
            case CameraMode.ThirdPerson:
                HandleThirdPersonMode();
                break;
            case CameraMode.FirstPerson:
                HandleFirstPersonMode();
                break;
        }
    }

    void HandleThirdPersonMode()
    {
        float movingDirection = Vector3.Dot(player.forward, playerRB.velocity);
        // For now, no reverse camera logic to keep it simple
        Vector3 targetOffset = thirdPersonOffset;
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.fixedDeltaTime * 5f);

        Vector3 desiredPosition = player.position + player.transform.TransformDirection(currentOffset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.LookAt(player.position + lookAtOffset);
    }

    void HandleFirstPersonMode()
    {
        Vector3 desiredPosition = player.position + player.transform.TransformDirection(firstPersonOffset);
        transform.position = desiredPosition;
        transform.rotation = player.rotation; 
    }
}