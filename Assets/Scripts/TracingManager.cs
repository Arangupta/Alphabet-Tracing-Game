using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the tracing process for letters made up of strokes.
/// 
/// Responsibilities:
/// - Handles user input (mouse/touch) for drawing
/// - Validates stroke progress against LetterStroke paths
/// - Provides visual feedback (colors, lines, particles, animations, sounds)
/// - Manages stroke lifecycle (begin, process, cancel, complete)
/// - Signals when the full letter is completed
/// </summary>
public class TracingManager : MonoBehaviour
{
    [Header("Strokes (in order)")]
    public List<LetterStroke> strokes = new List<LetterStroke>();
    private int currentStroke = 0;

    [Header("User drawing prefab")]
    public GameObject userLinePrefab;      // Prefab for user-drawn line
    public float minPointDistance = 0.03f; // Min distance before adding a new line point
    public float startTolerance = 0.35f;   // Extra tolerance for starting a stroke

    [Header("Visual/Feedback")]
    public Color onPathColor = Color.green; // Line color when on path
    public Color offPathColor = Color.red;  // Line color when off path

    [Header("UI")]
    public LetterManager letterManager; 
    private Button nextButton; // Reference to "Next" button

    [Header("Effects")]
    public ParticleSystem completionParticles; // Particle effect when complete
    public Animator celebrationAnimator;       // Animator for celebration animation

    [Header("Sounds")]
    public AudioSource drawingAudioSource;  // Audio while drawing
    public AudioClip drawingClip;

    public AudioSource correctClipSource;   // Audio for correct stroke
    public AudioClip correctClip;

    public AudioSource errorClipSource;     // Audio for error/cancel
    public AudioClip errorClip;

    public AudioSource Celebration_SoundSource; // Audio for celebration
    public AudioClip Celebration_Sound;

    // Runtime state
    private LineRenderer currentLine;
    private List<Vector3> currentPoints = new List<Vector3>();
    private bool isDrawing = false;
    private bool isCompleted = false;

    // ---------------- UNITY LIFECYCLE ----------------
    void Start()
    {
        // Auto-assign next button if not set
        if (nextButton == null)
            nextButton = GameObject.Find("NextButton").GetComponent<Button>();

        // Auto-collect strokes if not manually assigned
        if (strokes.Count == 0)
        {
            foreach (var s in GetComponentsInChildren<LetterStroke>())
                strokes.Add(s);
        }

        ResetAll();
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
    }

    // ---------------- INPUT HANDLING ----------------
    /// <summary>
    /// Converts screen position to world space.
    /// </summary>
    Vector3 GetWorldPoint(Vector3 screenPos)
    {
        float z = -Camera.main.transform.position.z;
        Vector3 w = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
        w.z = 0f;
        return w;
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0)) 
            BeginDraw(GetWorldPoint(Input.mousePosition));
        else if (Input.GetMouseButton(0) && isDrawing) 
            ProcessSample(GetWorldPoint(Input.mousePosition));
        else if (Input.GetMouseButtonUp(0) && isDrawing) 
            EndDraw();
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch t = Input.GetTouch(0);
        Vector3 w = GetWorldPoint(t.position);

        if (t.phase == TouchPhase.Began) BeginDraw(w);
        else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && isDrawing) ProcessSample(w);
        else if ((t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) && isDrawing) EndDraw();
    }

    // ---------------- DRAWING LIFECYCLE ----------------
    /// <summary>
    /// Begins a new stroke if user starts near stroke start point.
    /// </summary>
    void BeginDraw(Vector3 start)
    {
        if (currentStroke >= strokes.Count) return;

        LetterStroke stroke = strokes[currentStroke];
        Vector3 startPoint = stroke.GetStartPoint();

        // Must start near the stroke's first point
        if (Vector3.Distance(start, startPoint) > stroke.tolerance + startTolerance)
        {
            PlayError();
            return;
        }

        // Create new user line
        GameObject go = Instantiate(userLinePrefab, Vector3.zero, Quaternion.identity);
        go.SetActive(true);
        currentLine = go.GetComponent<LineRenderer>();

        if (currentLine == null) { Destroy(go); return; }

        currentPoints.Clear();
        isDrawing = true;

        // Start drawing sound
        if (drawingAudioSource != null && drawingClip != null)
        {
            drawingAudioSource.clip = drawingClip;
            drawingAudioSource.loop = true;
            drawingAudioSource.Play();
        }

        AddPointToLine(start);
        stroke.SamplePoint(start);
    }

    /// <summary>
    /// Processes ongoing drawing input samples.
    /// </summary>
    void ProcessSample(Vector3 sample)
    {
        if (!isDrawing || currentStroke >= strokes.Count) return;

        LetterStroke stroke = strokes[currentStroke];
        bool onPath = stroke.IsSampleOnPath(sample, out _, out _);

        if (onPath)
        {
            SetLineColor(onPathColor);

            // Add new point only if distance is large enough
            if (currentPoints.Count == 0 ||
                Vector3.Distance(currentPoints[currentPoints.Count - 1], sample) >= minPointDistance)
            {
                AddPointToLine(sample);
            }

            stroke.SamplePoint(sample);

            // Stroke complete?
            if (stroke.IsComplete())
                OnStrokeComplete();
        }
        else
        {
            // Immediately cancel if outside tolerance
            CancelCurrentStroke();
        }
    }

    /// <summary>
    /// Ends stroke when user releases input.
    /// </summary>
    void EndDraw()
    {
        if (!isDrawing) return;

        // Incomplete stroke -> cancel
        if (currentStroke < strokes.Count && !strokes[currentStroke].IsComplete())
            CancelCurrentStroke();

        isDrawing = false;

        if (drawingAudioSource != null && drawingAudioSource.isPlaying)
            drawingAudioSource.Stop();

        currentLine = null;
        currentPoints.Clear();
    }

    /// <summary>
    /// Cancels the current stroke attempt and resets progress.
    /// </summary>
    void CancelCurrentStroke()
    {
        // Remove user line
        if (currentLine != null)
            Destroy(currentLine.gameObject);

        // Reset stroke
        strokes[currentStroke].ResetStroke();

        currentLine = null;
        currentPoints.Clear();
        isDrawing = false;

        if (drawingAudioSource != null && drawingAudioSource.isPlaying)
            drawingAudioSource.Stop();

        PlayError();
    }

    // ---------------- HELPERS ----------------
    void AddPointToLine(Vector3 p)
    {
        currentPoints.Add(p);
        if (currentLine != null)
        {
            currentLine.positionCount = currentPoints.Count;
            currentLine.SetPositions(currentPoints.ToArray());
        }
    }

    void SetLineColor(Color c)
    {
        if (currentLine == null) return;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        currentLine.colorGradient = g;
    }

    // ---------------- STROKE COMPLETION ----------------
    void OnStrokeComplete()
    {
        // Play correct sound
        if (correctClipSource != null && correctClip != null)
            correctClipSource.PlayOneShot(correctClip);

        LetterStroke s = strokes[currentStroke];

        // Hide guides
        if (s.guideLine != null) s.guideLine.gameObject.SetActive(false);
        if (s.guideSprite != null) s.guideSprite.enabled = false;

        // Destroy user line
        if (currentLine != null)
        {
            Destroy(currentLine.gameObject);
            currentLine = null;
        }

        if (drawingAudioSource != null && drawingAudioSource.isPlaying)
            drawingAudioSource.Stop();

        isDrawing = false;
        currentPoints.Clear();

        currentStroke++;

        // Letter completed?
        if (currentStroke >= strokes.Count)
            OnAllStrokesComplete();
    }

    void OnAllStrokesComplete()
    {
        Debug.Log("Letter complete!");

        // Enable next button
        if (nextButton != null)
            nextButton.interactable = true;

        // Celebration sound
        if (Celebration_SoundSource != null && Celebration_Sound != null)
        {
            Celebration_SoundSource.clip = Celebration_Sound;
            Celebration_SoundSource.loop = true;
            Celebration_SoundSource.Play();
        }

        isCompleted = true;

        // Particles
        if (completionParticles != null && !completionParticles.isPlaying)
            completionParticles.Play();

        // Animation
        if (celebrationAnimator != null)
            celebrationAnimator.SetBool("isCelebrating", true);
    }

    // ---------------- RESET ----------------
    public void ResetAll()
    {
        currentStroke = 0;

        // Stop sounds/effects
        if (Celebration_SoundSource != null && Celebration_SoundSource.isPlaying)
            Celebration_SoundSource.Stop();

        if (completionParticles != null)
            completionParticles.Stop();

        if (celebrationAnimator != null)
            celebrationAnimator.SetBool("isCelebrating", false);

        isCompleted = false;

        // Reset strokes
        foreach (var s in strokes) 
            s.ResetStroke();

        if (nextButton != null) 
            nextButton.interactable = false;
    }

    // ---------------- AUDIO ----------------
    void PlayError()
    {
        if (errorClipSource != null && errorClip != null)
            errorClipSource.PlayOneShot(errorClip);
    }
}
