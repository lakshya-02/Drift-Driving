using System;
using UnityEngine;
using System.Collections;
using TMPro; // Add this line for TextMeshPro UI elements

public class VechileControl : MonoBehaviour
{
    public Rigidbody playerRB;
    public WheelParticles wheelParticles;
    public GameObject smokePrefab;
    public WheelColliders colliders;
    public WheelMeshes wheelMeshes;

    public float motorPower = 500f; // Reduced from 1000 for more controllable acceleration
    public float brakePower = 3000f;
    public float handbrakePower = 5000f; // New: Power for the handbrake
    public float maxSteering = 30f;

    public float slipAngle;
    public AnimationCurve steeringCurve;
    public float slipAllowance = 0.5f;
    public float maxSmokeEmission = 50f;
    public float minSmokeSpeed = 20f; // New: Minimum speed (km/h) to start showing smoke

    public float gasInput;
    public float brakeInput;
    private float steeringInput;
    private float speed;
    public float maxSpeed = 160f; // Reduced from 200 for a more realistic top speed
    private float speedclamped;
    public int isEnginerunning = 0; // 0: Off, 1: Starting, 2: Running
    public float reversePowerMultiplier = 0.75f; // Add this line. Set > 1 for faster reverse.
    private EngineAudio engineAudioComponent; // Cached reference
    public float steeringSensitivity = 1.0f; // Adjust turn sensitivity
    private float forwardFrictionMultiplier = 2.0f; // Acceleration/braking grip
    public UIController uiController; // Add this line

    void Start()
    {
        playerRB = GetComponent<Rigidbody>();
        engineAudioComponent = GetComponent<EngineAudio>();
        playerRB.centerOfMass = new Vector3(0f, -0.5f, 0f);

        ApplyFrictionSettings(colliders.frontLeft);
        ApplyFrictionSettings(colliders.frontRight);
        ApplyFrictionSettings(colliders.backLeft);
        ApplyFrictionSettings(colliders.backRight);

        // New: Apply suspension settings to all wheels
        ApplySuspensionSettings(colliders.frontLeft);
        ApplySuspensionSettings(colliders.frontRight);
        ApplySuspensionSettings(colliders.backLeft);
        ApplySuspensionSettings(colliders.backRight);

        InstantiateSmoke();
    }

    private void InstantiateSmoke()
    {
        if (smokePrefab == null || wheelParticles == null || colliders == null)
            return;

        // Helper function to instantiate and assign smoke particles
        ParticleSystem CreateSmokeAtWheel(WheelCollider wheelCollider)
        {
            if (wheelCollider == null) return null;

            // Instantiate smoke prefab at the wheel's position
            GameObject smokeInstance = Instantiate(smokePrefab, wheelCollider.transform.position, Quaternion.identity, transform);
            return smokeInstance.GetComponent<ParticleSystem>();
        }

        // Assign smoke particles to each wheel
        wheelParticles.frontLeft = CreateSmokeAtWheel(colliders.frontLeft);
        wheelParticles.frontRight = CreateSmokeAtWheel(colliders.frontRight);
        wheelParticles.backLeft = CreateSmokeAtWheel(colliders.backLeft);
        wheelParticles.backRight = CreateSmokeAtWheel(colliders.backRight);
    }

    // New: This coroutine now controls the entire engine start sequence and state.
    IEnumerator EngineStartSequence()
    {
        // State 1: Tell the car it's "Starting"
        isEnginerunning = 1;
        if (engineAudioComponent != null)
        {
            // Tell the audio script to play its start sound
            StartCoroutine(engineAudioComponent.StartEngine());
        }

        // Wait for the starting sound to play for a bit
        yield return new WaitForSeconds(0.6f);
        if (engineAudioComponent != null)
        {
            // Now, tell the audio script that the engine is "Running"
            engineAudioComponent.isEngineRunning = true;
        }
        
        // Wait a little longer for the sequence to feel complete
        yield return new WaitForSeconds(0.4f);

        // State 2: Tell the car it's fully "Running"
        isEnginerunning = 2;
    }

    // New: Public getter for the speedometer UI
    public float GetSpeed()
    {
        return speed;
    }

    // Changed: This method now provides a SIGNED ratio based on actual movement, not gas input.
    public float GetSignedSpeedRatio()
    {
        if (playerRB == null) return 0f;

        // Determine direction of travel relative to car's forward vector
        float movingDirection = Vector3.Dot(transform.forward, playerRB.velocity);
        
        // Return the speed ratio, preserving the sign of the direction
        return (speedclamped / maxSpeed) * Mathf.Sign(movingDirection);
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
        UpdateSmokePositions(); // Add this line
        CheckParticles();
        UpdateUI();
    }

    void SetInput()
    {
        gasInput = Input.GetAxis("Vertical");
        steeringInput = Input.GetAxis("Horizontal");

        // Changed: Call the new coroutine for the full sequence
        if (Mathf.Abs(gasInput) > 0 && isEnginerunning == 0)
        {
            StartCoroutine(EngineStartSequence());
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
        // Apply standard braking from reverse/brake logic (70% front, 30% rear)
        if (colliders.frontLeft != null) colliders.frontLeft.brakeTorque = brakeInput * brakePower * 0.7f;
        if (colliders.frontRight != null) colliders.frontRight.brakeTorque = brakeInput * brakePower * 0.7f;
        if (colliders.backLeft != null) colliders.backLeft.brakeTorque = brakeInput * brakePower * 0.3f;
        if (colliders.backRight != null) colliders.backRight.brakeTorque = brakeInput * brakePower * 0.3f;

        // Apply handbrake force ONLY to rear wheels if space is pressed
        if (Input.GetKey(KeyCode.Space))
        {
            // We use a high, fixed value for the handbrake to lock the wheels
            if (colliders.backLeft != null) colliders.backLeft.brakeTorque = handbrakePower;
            if (colliders.backRight != null) colliders.backRight.brakeTorque = handbrakePower;
        }
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

        var emissionModule = particle.emission;

        // Only emit smoke if the car is moving above the minimum speed
        if (speed < minSmokeSpeed)
        {
            emissionModule.rateOverTime = 0;
            return;
        }

        // Make smoke more sensitive to sideways slip (drifting)
        float slipValue = Mathf.Abs(hit.sidewaysSlip) * 1.5f + Mathf.Abs(hit.forwardSlip);
        float totalSlip = slipValue;

        if (totalSlip > slipAllowance)
        {
            // The amount of slip determines the emission rate.
            float slipRatio = Mathf.Clamp01((totalSlip - slipAllowance) / 2.0f);
            emissionModule.rateOverTime = slipRatio * maxSmokeEmission;
        }
        else
        {
            emissionModule.rateOverTime = 0;
        }
    }

    // New: This method adjusts the suspension to make the car sit lower.
    private void ApplySuspensionSettings(WheelCollider wc)
    {
        if (wc == null) return;

        JointSpring suspension = wc.suspensionSpring;

        // Lower the target position to make the car sag on its suspension
        // Default is 0.5. A lower value makes it sit lower.
        suspension.targetPosition = 0.3f; 
        
        // You can also adjust spring and damper for feel, but targetPosition is key for ride height
        // suspension.spring = 35000f;
        // suspension.damper = 4500f;

        wc.suspensionSpring = suspension;
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

    // New: This method updates all the UI elements
    void UpdateUI()
    {
        if (uiController == null) return;

        // When the engine is off, show idle/off state on UI
        if (isEnginerunning == 0)
        {
            if (uiController.needle != null)
            {
                uiController.needle.localEulerAngles = new Vector3(0, 0, uiController.minNeedleRotation);
            }
            if (uiController.rpmText != null)
            {
                uiController.rpmText.text = "RPM : 0";
            }
            if (uiController.gearText != null)
            {
                uiController.gearText.text = "Gear : N";
            }
            return; // Stop here if engine is off
        }

        // Update the speedometer needle rotation
        if (uiController.needle != null)
        {
            float speedRatio = GetSpeed() / maxSpeed;
            float needleRotation = Mathf.Lerp(uiController.minNeedleRotation, uiController.maxNeedleRotation, speedRatio);
            uiController.needle.localEulerAngles = new Vector3(0, 0, needleRotation);
        }

        // Update the RPM text (simulated)
        if (uiController.rpmText != null)
        {
            float rpmRatio = Mathf.Abs(GetSignedSpeedRatio());
            int simulatedRPM = 800 + (int)(rpmRatio * 6200); // Simulates RPM from 800 to 7000
            uiController.rpmText.text = "RPM : " + simulatedRPM.ToString("0");
        }

        // Update the Gear text (simulated)
        if (uiController.gearText != null)
        {
            string currentGear = "N";
            float currentSpeed = GetSpeed();
            float movingDirection = Vector3.Dot(transform.forward, playerRB.velocity);

            if (movingDirection < -0.1f && currentSpeed > 1f)
            {
                currentGear = "R";
            }
            else if (currentSpeed > 1f)
            {
                // Simple gear simulation based on speed
                if (currentSpeed < 40) currentGear = "1";
                else if (currentSpeed < 80) currentGear = "2";
                else if (currentSpeed < 120) currentGear = "3";
                else if (currentSpeed < 160) currentGear = "4";
                else currentGear = "5";
            }
            uiController.gearText.text = "Gear : " + currentGear;
        }
    }

    private void UpdateSmokePositions()
    {
        if (wheelParticles == null || wheelMeshes == null) return;

        // Update smoke positions to match wheel positions
        if (wheelParticles.frontLeft != null && wheelMeshes.frontLeft != null)
            wheelParticles.frontLeft.transform.position = wheelMeshes.frontLeft.position;

        if (wheelParticles.frontRight != null && wheelMeshes.frontRight != null)
            wheelParticles.frontRight.transform.position = wheelMeshes.frontRight.position;

        if (wheelParticles.backLeft != null && wheelMeshes.backLeft != null)
            wheelParticles.backLeft.transform.position = wheelMeshes.backLeft.position;

        if (wheelParticles.backRight != null && wheelMeshes.backRight != null)
            wheelParticles.backRight.transform.position = wheelMeshes.backRight.position;
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

    // New: Add this entire class to manage UI elements
    [Serializable]
    public class UIController
    {
        [Header("UI Elements")]
        public Transform needle; // The needle's Transform component
        public TextMeshProUGUI rpmText;
        public TextMeshProUGUI gearText;

        [Header("Needle Settings")]
        public float minNeedleRotation = 220f; // Updated default to match your UI
        public float maxNeedleRotation = -240f;
    }
}