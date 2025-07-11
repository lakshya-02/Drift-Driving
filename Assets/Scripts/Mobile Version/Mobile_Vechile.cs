using System;
using UnityEngine;

public class Mobile_Vechile : MonoBehaviour
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
    public Mybutton gasButton;
    public Mybutton brakeButton;
    public Mybutton rightButton;
    public Mybutton leftButton;

    void Start()
    {
        playerRB = GetComponent<Rigidbody>();
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

    void Update()
    {
        speed = playerRB.velocity.magnitude;
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
        
        if (gasButton.isPressed)
        {
            gasInput += gasButton.dampenPress;
        }
        if (brakeButton.isPressed)
        {
            gasInput -= brakeButton.dampenPress; 
        }

        steeringInput = Input.GetAxis("Horizontal");

        if (rightButton.isPressed)
        {
            steeringInput += rightButton.dampenPress;
        }
        if (leftButton.isPressed)
        {
            steeringInput -= leftButton.dampenPress;
        }
        
        float currentSpeed = playerRB.velocity.magnitude;

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
            brakeInput = 0;
        }
    }

    void ApplyBrake()
    {
        colliders.backLeft.brakeTorque = brakeInput * brakePower * 0.3f;
        colliders.backRight.brakeTorque = brakeInput * brakePower * 0.3f;
        colliders.frontLeft.brakeTorque = brakeInput * brakePower * 0.7f;
        colliders.frontRight.brakeTorque = brakeInput * brakePower * 0.7f;
    }

    void ApplyMotor()
    {
        colliders.backLeft.motorTorque = gasInput * motorPower;
        colliders.backRight.motorTorque = gasInput * motorPower;
    }

    void ApplySteering()
    {
        float steeringAngle = steeringInput * maxSteering;
        colliders.frontLeft.steerAngle = steeringAngle;
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
        if (col == null || mesh == null) return;
        Vector3 pos;
        Quaternion rot;
        col.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    void CheckParticles()
    {
        WheelHit hit;

        if (colliders.frontLeft.GetGroundHit(out hit))
            ToggleSmoke(wheelParticles.frontLeft, hit);
        if (colliders.frontRight.GetGroundHit(out hit))
            ToggleSmoke(wheelParticles.frontRight, hit);
        if (colliders.backLeft.GetGroundHit(out hit))
            ToggleSmoke(wheelParticles.backLeft, hit);
        if (colliders.backRight.GetGroundHit(out hit))
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
