using System;
using UnityEngine;
using System.Collections; // Required for IEnumerator if you ever use it directly here

public class VechileControl : MonoBehaviour
{
    public Rigidbody playerRB;
    public WheelParticles wheelParticles;
    public GameObject smokePrefab;
    public WheelColliders colliders;
    public WheelMeshes wheelMeshes;

    public float motorPower = 1000f;
    public float brakePower = 3000f;
    public float maxSteering = 30f;

    public float slipAngle;
    public AnimationCurve steeringCurve;
    public float slipAllowance = 0.5f;

    public float gasInput;
    public float brakeInput;
    private float steeringInput;
    private float speed;
    public float maxSpeed = 200f;
    private float speedclamped;
    public int isEnginerunning = 0; // 0: Off, 1: Starting, 2: Running // UNCOMMENTED
    private EngineAudio engineAudioComponent; // Cached reference

    void Start()
    {
        playerRB = GetComponent<Rigidbody>();
        engineAudioComponent = GetComponent<EngineAudio>(); // Cache the component
        if (engineAudioComponent == null)
        {
            Debug.LogError("EngineAudio component not found on this VechileControl GameObject!", this);
        }
        InstantiateSmoke();
        // Initialize other things if needed
    }
   
    void InstantiateSmoke()
    {
        if (smokePrefab != null)
        {
            float wheelRadius = colliders.frontRight != null ? colliders.frontRight.radius : 0f;

            if (wheelParticles != null)
            {
                if (colliders.frontRight != null)
                    wheelParticles.frontRight = Instantiate(smokePrefab, colliders.frontRight.transform.position - Vector3.up * wheelRadius, Quaternion.identity, colliders.frontRight.transform).GetComponent<ParticleSystem>();
                if (colliders.frontLeft != null)
                    wheelParticles.frontLeft = Instantiate(smokePrefab, colliders.frontLeft.transform.position - Vector3.up * wheelRadius, Quaternion.identity, colliders.frontLeft.transform).GetComponent<ParticleSystem>();
                if (colliders.backRight != null)
                    wheelParticles.backRight = Instantiate(smokePrefab, colliders.backRight.transform.position - Vector3.up * wheelRadius, Quaternion.identity, colliders.backRight.transform).GetComponent<ParticleSystem>();
                if (colliders.backLeft != null)
                    wheelParticles.backLeft = Instantiate(smokePrefab, colliders.backLeft.transform.position - Vector3.up * wheelRadius, Quaternion.identity, colliders.backLeft.transform).GetComponent<ParticleSystem>();
            }
        }
    }

    public float GetSpeedRatio()
    {
        var gas = Mathf.Clamp(Mathf.Abs(gasInput), 0.5f, 1f);
        return speedclamped * gas / maxSpeed;
    }

    void Update()
    {
        // Consider getting speed from playerRB.velocity.magnitude * 3.6f for km/h if that's more intuitive
        // The current RPM based speed is fine if it works for your calculations.
        if (colliders.backRight != null) // Added null check for safety
        {
            speed = colliders.backRight.rpm * colliders.backRight.radius * Mathf.PI * 2f * 60f / 1000f;
        }
        else
        {
            speed = 0f; // Default if backRight wheel is not assigned
        }
        speedclamped = Mathf.Lerp(speedclamped, speed, Time.deltaTime);
        SetInput();
        ApplyMotor();
        ApplySteering();
        UpdateWheelMeshes();
        ApplyBrake();
        CheckParticles();
    }

    void SetInput()
    {
        gasInput = Input.GetAxis("Vertical");

        // If gas is pressed AND engine is currently OFF (state 0)
        if (Mathf.Abs(gasInput) > 0 && isEnginerunning == 0) {
            if (engineAudioComponent != null) {
                StartCoroutine(engineAudioComponent.StartEngine());
                isEnginerunning = 1;
            }
            else
            {
                Debug.LogWarning("EngineAudioComponent is null. Engine sound/sequence won't start. For testing, directly setting engine to running.", this);
                // isEnginerunning = 2; // Fallback for testing movement without audio
            }
        }
        steeringInput = Input.GetAxis("Horizontal");

        float currentSpeed = 0f;
        if (playerRB != null) // Added null check for safety
        {
            currentSpeed = playerRB.velocity.magnitude;
        }


        if (currentSpeed > 0.1f)
        {
            Vector3 velocityDirection = playerRB.velocity.normalized;
            slipAngle = Vector3.Angle(transform.forward, velocityDirection);
        }
        else
        {
            slipAngle = 0f;
        }

        if (slipAngle > 120f && gasInput < 0 && currentSpeed > 1.0f)
        {
            brakeInput = Mathf.Abs(gasInput);
            gasInput = 0;
        }
        else
        {
            // This will clear brakeInput if the above condition isn't met.
            // If you have a separate brake button, this logic would need adjustment.
            brakeInput = 0;
        }
    }

    void ApplyBrake()
    {
        if (colliders.backLeft != null) colliders.backLeft.brakeTorque = brakeInput * brakePower * 0.3f;
        if (colliders.backRight != null) colliders.backRight.brakeTorque = brakeInput * brakePower * 0.3f;
        if (colliders.frontLeft != null) colliders.frontLeft.brakeTorque = brakeInput * brakePower * 0.7f;
        if (colliders.frontRight != null) colliders.frontRight.brakeTorque = brakeInput * brakePower * 0.7f;
    }

    void ApplyMotor()
    {
        // Allow motor if engine is STARTING (1) or fully RUNNING (2)
        if (isEnginerunning >= 1) // Changed from "> 1" to ">= 1"
        {
            if (Mathf.Abs(speed) < maxSpeed)
            {
                if (colliders.backLeft != null) colliders.backLeft.motorTorque = gasInput * motorPower;
                if (colliders.backRight != null) colliders.backRight.motorTorque = gasInput * motorPower;
            }
            else
            {
                if (colliders.backLeft != null) colliders.backLeft.motorTorque = 0f;
                if (colliders.backRight != null) colliders.backRight.motorTorque = 0f;
            }
        }
        else // Engine is OFF (state 0)
        {
            if (colliders.backLeft != null) colliders.backLeft.motorTorque = 0f;
            if (colliders.backRight != null) colliders.backRight.motorTorque = 0f;
        }
    }

    void ApplySteering()
    {
        float steeringAngle = steeringInput * maxSteering;

        // Optional: Use this if your steeringCurve is smooth
        // float steeringAngle = steeringInput * steeringCurve.Evaluate(speed);

        if (colliders.frontLeft != null) colliders.frontLeft.steerAngle = steeringAngle;
        if (colliders.frontRight != null) colliders.frontRight.steerAngle = steeringAngle;
    }

    void UpdateWheelMeshes()
    {
        UpdateWheelPosition(colliders.frontLeft, wheelMeshes.frontLeft);
        UpdateWheelPosition(colliders.frontRight, wheelMeshes.frontRight);
        UpdateWheelPosition(colliders.backLeft, wheelMeshes.backLeft);
        UpdateWheelPosition(colliders.backRight, wheelMeshes.backRight);
    }

    void UpdateWheelPosition(WheelCollider col, Transform mesh)
    {
        if (col == null || mesh == null) return; // CORRECTED SYNTAX
        Vector3 pos;
        Quaternion rot;
        col.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    void CheckParticles()
    {
        WheelHit hit;

        if (colliders.frontLeft != null && wheelParticles.frontLeft != null && colliders.frontLeft.GetGroundHit(out hit))
            ToggleSmoke(wheelParticles.frontLeft, hit);
        if (colliders.frontRight != null && wheelParticles.frontRight != null && colliders.frontRight.GetGroundHit(out hit))
            ToggleSmoke(wheelParticles.frontRight, hit);
        if (colliders.backLeft != null && wheelParticles.backLeft != null && colliders.backLeft.GetGroundHit(out hit))
            ToggleSmoke(wheelParticles.backLeft, hit);
        if (colliders.backRight != null && wheelParticles.backRight != null && colliders.backRight.GetGroundHit(out hit))
            ToggleSmoke(wheelParticles.backRight, hit);
    }

    void ToggleSmoke(ParticleSystem particle, WheelHit hit)
    {
        if (particle == null) return;

        if ((Mathf.Abs(hit.forwardSlip) + Mathf.Abs(hit.sidewaysSlip)) > slipAllowance)
        {
            if (!particle.isPlaying)
                particle.Play();
        }
        else
        {
            if (particle.isPlaying)
                particle.Stop();
        }
    }

    [Serializable]
    public class WheelColliders
    {
        public WheelCollider frontLeft;
        public WheelCollider frontRight;
        public WheelCollider backLeft;
        public WheelCollider backRight;
    }

    [Serializable]
    public class WheelMeshes
    {
        public Transform frontLeft;
        public Transform frontRight;
        public Transform backLeft;
        public Transform backRight;
    }

    [Serializable]
    public class WheelParticles
    {
        public ParticleSystem frontLeft;
        public ParticleSystem frontRight;
        public ParticleSystem backLeft;
        public ParticleSystem backRight;
    }
}
