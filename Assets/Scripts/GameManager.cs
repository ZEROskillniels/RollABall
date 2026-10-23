using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float levelDuration = 180f;
    public bool IsFinished { get; private set; }

    private int totalPickups;
    private int collectedPickups;
    private float remainingTime;
    private bool won;
    private GUIStyle hudStyle;
    private GUIStyle messageStyle;

    private void Awake()
    {
        Instance = this;
        totalPickups = GameObject.FindGameObjectsWithTag("PickUp").Length;
        remainingTime = levelDuration;
    }

    private void Update()
    {
        if (!IsFinished)
        {
            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
            if (remainingTime <= 0f)
                IsFinished = true;
        }

        if (IsFinished && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Collect()
    {
        if (IsFinished)
            return;

        collectedPickups++;
        if (collectedPickups >= totalPickups && totalPickups > 0)
        {
            won = true;
            IsFinished = true;
            LaunchConfetti();
        }
    }

    private void OnGUI()
    {
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label);
            hudStyle.fontSize = 25;
            hudStyle.fontStyle = FontStyle.Bold;
            hudStyle.normal.textColor = Color.white;

            messageStyle = new GUIStyle(hudStyle);
            messageStyle.fontSize = 42;
            messageStyle.alignment = TextAnchor.MiddleCenter;
        }

        GUI.Box(new Rect(18, 18, 295, 80), GUIContent.none);
        GUI.Label(new Rect(35, 25, 280, 35), $"Score: {collectedPickups}/{totalPickups}", hudStyle);
        GUI.Label(new Rect(35, 60, 280, 35), $"Time: {Mathf.CeilToInt(remainingTime)} s", hudStyle);

        if (IsFinished)
        {
            string message = won ? "Congratulations!" : "Time's up!";
            GUI.Box(new Rect(Screen.width / 2f - 280, Screen.height / 2f - 70, 560, 140), GUIContent.none);
            GUI.Label(new Rect(Screen.width / 2f - 260, Screen.height / 2f - 55, 520, 70), message, messageStyle);
            GUI.Label(new Rect(Screen.width / 2f - 260, Screen.height / 2f + 20, 520, 35), "Press R to restart", hudStyle);
        }
    }

    private void LaunchConfetti()
    {
        GameObject celebration = new GameObject("Victory Confetti");
        GameObject player = GameObject.Find("Player");
        celebration.transform.position = player != null ? player.transform.position + Vector3.up * 2f : Vector3.up * 2f;

        ParticleSystem particles = celebration.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.duration = 4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.gravityModifier = 0.55f;
        main.maxParticles = 250;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.25f), new Color(0.3f, 0.9f, 1f));

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 180) });

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader != null)
            renderer.material = new Material(particleShader);

        particles.Play();
        Destroy(celebration, 5f);
    }
}
