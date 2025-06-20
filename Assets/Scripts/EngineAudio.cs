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
        // If the engine isn't running, silence all sounds.
        if (!isEngineRunning || vehicleControl == null)
        {
            if (idleSound != null) idleSound.volume = 0;
            if (runningSound != null) runningSound.volume = 0;
            if (reverseSound != null) reverseSound.volume = 0;
            return;
        }

        // Get the single, reliable value from VechileControl
        float signedSpeedRatio = vehicleControl.GetSignedSpeedRatio();
        float speedRatio = Mathf.Abs(signedSpeedRatio); // Unsigned ratio for volume/pitch

        revLimiter = speedRatio > LimiterEngage ? (Mathf.Sin(Time.time * LimiterFrequency) + 1f) * LimiterSound * (speedRatio - LimiterEngage) : 0f;

        if (idleSound != null)
            idleSound.volume = Mathf.Lerp(0.1f, idleMaxVolume, speedRatio - revLimiter);

        // Determine which sound to play based on the sign of the speed ratio
        if (signedSpeedRatio > 0.01f) // Moving forward
        {
            if (reverseSound != null) reverseSound.volume = 0;
            if (runningSound != null)
            {
                runningSound.volume = Mathf.Lerp(0.3f, runningMaxVolume, speedRatio - revLimiter);
                runningSound.pitch = Mathf.Lerp(0.3f, runningMaxPitch, speedRatio - revLimiter);
            }
        }
        else if (signedSpeedRatio < -0.01f) // Moving in reverse
        {
            if (runningSound != null) runningSound.volume = 0;
            if (reverseSound != null)
            {
                reverseSound.volume = Mathf.Lerp(0.2f, reverseMaxVolume, speedRatio);
                reverseSound.pitch = Mathf.Lerp(0.2f, reverseMaxPitch, speedRatio);
            }
        }
        else // Idle or coasting at zero speed
        {
            if (runningSound != null) runningSound.volume = 0;
            if (reverseSound != null) reverseSound.volume = 0;
        }
    }
    
    // This coroutine is now only responsible for playing the start sound.
    public IEnumerator StartEngine()
    {
        if (startingSound != null)
        {
            startingSound.Play();
        }
        yield return null;
    }
}