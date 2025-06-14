using System.Collections;
using UnityEngine;

public class EngineAudio : MonoBehaviour
{
    public AudioSource runningSound;
    public float runningMaxVolume;
    public float runningMaxPitch;
    public AudioSource reverseSound;
    public float reverseMaxVolume;
    public float reverseMaxPitch;
    public AudioSource idleSound;
    public float idleMaxVolume;
    public float speedRatio;
    private float revLimiter;
    public float LimiterSound = 1f;
    public float LimiterFrequency = 3f;
    public float LimiterEngage = 0.8f;
    public bool isEngineRunning = false;
    public AudioSource startingSound;

    private VechileControl vehicleControl;

    void Start()
    {
        vehicleControl = GetComponent<VechileControl>();
        if (idleSound != null) idleSound.volume = 0;
        if (runningSound != null) runningSound.volume = 0;
        if (reverseSound != null) reverseSound.volume = 0;
    }

    void Update()
    {
        float speedSign = 0;
        if (vehicleControl != null)
        {
            float currentSpeedRatio = vehicleControl.GetSpeedRatio();
            speedSign = Mathf.Sign(currentSpeedRatio);
            speedRatio = Mathf.Abs(currentSpeedRatio);
        }
        else return;

        revLimiter = speedRatio > LimiterEngage ? (Mathf.Sin(Time.time * LimiterFrequency) + 1f) * LimiterSound * (speedRatio - LimiterEngage) : 0f;

        if (isEngineRunning)
        {
            if (idleSound != null)
                idleSound.volume = Mathf.Lerp(0.1f, idleMaxVolume, speedRatio - revLimiter);

            if (speedSign > 0) // Moving forward
            {
                if (reverseSound != null) reverseSound.volume = 0;
                if (runningSound != null)
                {
                    runningSound.volume = Mathf.Lerp(0.3f, runningMaxVolume, speedRatio - revLimiter);
                    runningSound.pitch = Mathf.Lerp(0.3f, runningMaxPitch, speedRatio - revLimiter);
                }
            }
            else if (speedSign < 0) // Moving in reverse
            {
                if (runningSound != null) runningSound.volume = 0;
                if (reverseSound != null)
                {
                    reverseSound.volume = Mathf.Lerp(0f, reverseMaxVolume, speedRatio);
                    reverseSound.pitch = Mathf.Lerp(0.2f, reverseMaxPitch, speedRatio);
                }
            }
            else // No movement
            {
                if (runningSound != null) runningSound.volume = 0;
                if (reverseSound != null) reverseSound.volume = 0;
            }
        }
        else
        {
            if (idleSound != null) idleSound.volume = 0;
            if (runningSound != null) runningSound.volume = 0;
            if (reverseSound != null) reverseSound.volume = 0;
        }
    }
    public IEnumerator StartEngine()
    {
        if (startingSound != null) startingSound.Play();
        if (vehicleControl != null)
        {
        }
        yield return new WaitForSeconds(0.6f);
        isEngineRunning = true;
        yield return new WaitForSeconds(0.4f);
        if (vehicleControl != null)
        {
        }
        else // engineAudioComponent is null
        {
            Debug.LogWarning("EngineAudioComponent is null. Engine sound/sequence won't start. For testing, directly setting engine to running.", this);
            // isEnginerunning = 2; // Fallback for testing movement without audio (THIS IS COMMENTED OUT)
        }
    }
}