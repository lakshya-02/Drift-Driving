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
    public int isEnginerunning = 0; // 0: Off, 1: Starting, 2: Running
    public float reversePowerMultiplier = 0.75f; // Add this line. Set > 1 for faster reverse.
    private EngineAudio engineAudioComponent; // Cached reference
    public float steeringSensitivity = 1.0f; // Adjust turn sensitivity
    private float forwardFrictionMultiplier = 2.0f; // Acceleration/braking grip

    void Start()
    {
        playerRB = GetComponent<Rigidbody>();
        engineAudioComponent = GetComponent<EngineAudio>();
        playerRB.centerOfMass = new Vector3(0f, -0.5f, 0f);

        ApplyFrictionSettings(colliders.frontLeft);
        ApplyFrictionSettings(colliders.frontRight);
        ApplyFrictionSettings(colliders.backLeft);
        ApplyFrictionSettings(colliders.backRight);

        InstantiateSmoke();
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
        speed = playerRB.velocity.magnitude * 3.6f;
        speedclamped = Mathf.Lerp(speedclamped, speed, Time.deltaTime * 5f);

        SetInput();
        ApplyMotor();
        ApplySteering();
        UpdateWheelMeshes();
        ApplyBrake();
        CheckParticles();
    }

    void SetInput()
    {
        gasInput = Input.GetAxis("Vertical");     // -1 (reverse), 1 (forward)
        steeringInput = Input.GetAxis("Horizontal");

        if (Mathf.Abs(gasInput) > 0 && isEnginerunning == 0)
        {
            if (engineAudioComponent != null)
            {
                StartCoroutine(engineAudioComponent.StartEngine());
                isEnginerunning = 1;
            }
        }

        float movingDirection = Vector3.Dot(transform.forward, playerRB.velocity);
        speed = playerRB.velocity.magnitude * 3.6f;

        // Calculate slip
        if (speed > 0.1f)
        {
            Vector3 velocityDir = playerRB.velocity.normalized;
            slipAngle = Vector3.Angle(transform.forward, velocityDir);
        }
        else
        {
            slipAngle = 0f;
        }

        // Reset brake
        brakeInput = 0f;

        // === REVERSE VS BRAKE FIX START ===
        bool wantsToReverse = gasInput < 0;
        bool goingForward   = movingDirection > 0.5f;
        bool goingBackward  = movingDirection < -0.5f;

        if (wantsToReverse && goingForward)
        {
            brakeInput = Mathf.Abs(gasInput);
            gasInput   = 0f;
        }
        else if (gasInput > 0 && goingBackward)
        {
            brakeInput = Mathf.Abs(gasInput);
            gasInput   = 0f;
        }
    }

    void ApplyBrake()
    {
        if (colliders.backLeft  != null) colliders.backLeft.brakeTorque  = brakeInput * brakePower * 0.3f;
        if (colliders.backRight != null) colliders.backRight.brakeTorque = brakeInput * brakePower * 0.3f;
        if (colliders.frontLeft != null) colliders.frontLeft.brakeTorque = brakeInput * brakePower * 0.7f;
        if (colliders.frontRight != null) colliders.frontRight.brakeTorque = brakeInput * brakePower * 0.7f;
    }

    void ApplyMotor()
    {
        // Allow motor if engine is STARTING (1) or fully RUNNING (2)
        if (isEnginerunning >= 1)
        {
            // Determine the torque to apply
            float currentMotorTorque = motorPower;
            if (gasInput < 0) // If we are reversing
            {
                currentMotorTorque *= reversePowerMultiplier;
            }

            if (Mathf.Abs(speed) < maxSpeed)
            {
                if (colliders.backLeft != null) colliders.backLeft.motorTorque = gasInput * currentMotorTorque;
                if (colliders.backRight != null) colliders.backRight.motorTorque = gasInput * currentMotorTorque;
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
        float speedFactor = Mathf.Clamp01(speed / maxSpeed); // Scale speed between 0 and 1
        float dynamicSteeringAngle = Mathf.Lerp(maxSteering, maxSteering * 0.5f, speedFactor); // Reduce steering at high speeds

        float movingDirection = Vector3.Dot(transform.forward, playerRB.velocity);
        float steeringAngle = steeringInput * dynamicSteeringAngle * steeringSensitivity;

        if (movingDirection < 0) // Reverse movement
        {
            steeringAngle = -steeringAngle; // Invert steering angle for reverse
        }

        if (colliders.frontLeft != null)
            colliders.frontLeft.steerAngle = steeringAngle;
        if (colliders.frontRight != null)
            colliders.frontRight.steerAngle = steeringAngle;
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

    private void ApplyFrictionSettings(WheelCollider wc)
    {
        if (wc == null) return;

        var forwardFriction = wc.forwardFriction;
        forwardFriction.stiffness = forwardFrictionMultiplier;
        wc.forwardFriction = forwardFriction;

        var sideFriction = wc.sidewaysFriction;
        sideFriction.extremumSlip = 0.2f;
        sideFriction.extremumValue = 1.5f;
        sideFriction.asymptoteSlip = 0.4f;
        sideFriction.asymptoteValue = 0.75f;
        sideFriction.stiffness = 2.2f;
        wc.sidewaysFriction = sideFriction;
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