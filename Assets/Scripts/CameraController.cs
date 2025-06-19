using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform player;
    private Rigidbody playerRB;

    public enum CameraMode { ThirdPerson, FirstPerson, Static }

    [Header("Camera Modes")]
    public CameraMode currentMode = CameraMode.ThirdPerson;

    [Header("Camera Positioning")]
    public Vector3 thirdPersonOffset = new Vector3(0f, 3.135f, -6f); // Adjusted Y (+0.635)
    public Vector3 reverseOffset = new Vector3(0f, 3.135f, 6f);      // Adjusted Y (+0.635)
    public Vector3 firstPersonOffset = new Vector3(0f, 2.135f, 0f);  // Adjusted Y (+0.635)
    public Vector3 staticCameraPosition = new Vector3(0f, 10.635f, -10f); // Adjusted Y (+0.635)
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
        // Switch camera modes using keys
        if (Input.GetKeyDown(KeyCode.X))
        {
            currentMode = CameraMode.FirstPerson;
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            currentMode = CameraMode.Static;
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
            case CameraMode.Static:
                HandleStaticMode();
                break;
        }
    }

    void HandleThirdPersonMode()
    {
        float movingDirection = Vector3.Dot(player.forward, playerRB.velocity);
        Vector3 targetOffset = movingDirection >= 0 ? thirdPersonOffset : reverseOffset;
        currentOffset = Vector3.Lerp(currentOffset, targetOffset, Time.fixedDeltaTime * 5f);

        Vector3 desiredPosition = player.position + player.transform.TransformDirection(currentOffset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.LookAt(player.position + lookAtOffset);
    }

    void HandleFirstPersonMode()
    {
        // No reverse mode for first-person POV
        Vector3 desiredPosition = player.position + player.transform.TransformDirection(firstPersonOffset);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        transform.LookAt(player.position + lookAtOffset);
    }

    void HandleStaticMode()
    {
        // Fixed position for static camera
        transform.position = staticCameraPosition;
        transform.LookAt(player.position + lookAtOffset);
    }
}