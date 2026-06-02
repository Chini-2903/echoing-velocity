using UnityEngine;
using TMPro;

public class WorldFluxManager : MonoBehaviour
{
    public static WorldFluxManager Instance;

    [Header("UI References")]
    public TextMeshProUGUI stateText;

    [Header("Audio Sources")]
    public AudioSource modeChangeAudio;
    public AudioSource dilationAudio;
    public AudioSource crushDropAudio;

    [HideInInspector] public float currentJumpMultiplier = 1f;
    private int currentStateIndex = 0;

    private readonly string[] stateNames = { "STABLE (NORMAL)", "MOON (LOW GRAVITY)", "CRUSH (HEAVY GRAVITY)", "DILATION (SLOW-MO)" };
    private readonly float[] gravityValues = { -19.62f, -4f, -50f, -19.62f };
    private readonly float[] jumpMultipliers = { 1f, 2.5f, 0.4f, 1f };
    private readonly float[] timeScales = { 1f, 1f, 1f, 0.3f };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ApplyState(0, true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ApplyState(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ApplyState(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ApplyState(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ApplyState(3);
    }

    void ApplyState(int index, bool isStarting = false)
    {
        // Don't play the mode change sound on game boot
        if (!isStarting && currentStateIndex != index)
        {
            if (modeChangeAudio != null) modeChangeAudio.Play();

            // Play specific cinematic sounds for dramatic modes
            if (index == 3 && dilationAudio != null) dilationAudio.Play();   // DILATION
        }

        currentStateIndex = index;
        Physics.gravity = new Vector3(0, gravityValues[index], 0);
        currentJumpMultiplier = jumpMultipliers[index];
        Time.timeScale = timeScales[index];

        if (stateText != null) stateText.text = "WORLD STATE: " + stateNames[index];
    }
}