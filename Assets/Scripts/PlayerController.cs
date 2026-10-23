using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float speed = 18f;

    private Rigidbody rb;
    private AudioSource audioSource;
    private AudioClip pickupSound;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        pickupSound = CreatePickupSound();
    }

    private void FixedUpdate()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || (GameManager.Instance != null && GameManager.Instance.IsFinished))
            return;

        float moveX = 0f;
        float moveZ = 0f;

        // The camera looks toward negative Z with its screen-right pointing toward negative X.
        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) moveX += 1f;
        if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) moveX -= 1f;
        if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) moveZ -= 1f;
        if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) moveZ += 1f;

        Vector3 movement = new Vector3(moveX, 0f, moveZ).normalized;
        rb.AddForce(movement * speed, ForceMode.Force);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PickUp"))
            return;

        other.gameObject.SetActive(false);
        if (audioSource != null && pickupSound != null)
            audioSource.PlayOneShot(pickupSound, 0.7f);
        if (GameManager.Instance != null)
            GameManager.Instance.Collect();
    }

    private static AudioClip CreatePickupSound()
    {
        const int sampleRate = 22050;
        const float duration = 0.22f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float envelope = Mathf.Pow(1f - time / duration, 2f);
            float frequency = 660f + 1200f * time;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * 0.35f;
        }

        AudioClip clip = AudioClip.Create("Pick Up Chime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
