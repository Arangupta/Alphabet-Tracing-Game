using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TracingManager : MonoBehaviour
{
    [Header("Strokes (in order)")]
    public List<LetterStroke> strokes = new List<LetterStroke>();
    int currentStroke = 0;

    [Header("User drawing prefab")]
    public GameObject userLinePrefab;
    public float minPointDistance = 0.03f;
    public float startTolerance = 0.35f;

    [Header("Visual/Feedback")]
    public Color onPathColor = Color.green;
    public Color offPathColor = Color.red;
 

    [Header("UI")]
    public LetterManager letterManager;
    private Button nextButton;

    [Header("Effects")]
public ParticleSystem completionParticles; // Assign in inspector
public Animator celebrationAnimator;       // Animator for celebration animation

    // Runtime
    private bool isCompleted = false;          // Flag when all strokes are completed
[Header("Sounds")]
public AudioSource drawingAudioSource;  // AudioSource that will play drawing sound
    public AudioClip drawingClip; 
   public AudioSource correctClipSource;  // AudioSource that will play correct sound
    public AudioClip correctClip;
    public AudioSource errorClipSource;         // General AudioSource for other sounds
    public AudioClip errorClip;
    public AudioSource Celebration_SoundSource;         // General AudioSource for other sounds
    public AudioClip Celebration_Sound;         




    // Runtime
    private LineRenderer currentLine;
    private List<Vector3> currentPoints = new List<Vector3>();
    private bool isDrawing = false;

    void Start()
    {

    if (nextButton == null)
        nextButton = GameObject.Find("NextButton").GetComponent<Button>();


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

    #region Input
    Vector3 GetWorldPoint(Vector3 screenPos)
    {
        float z = -Camera.main.transform.position.z;
        Vector3 w = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
        w.z = 0f;
        return w;
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0)) BeginDraw(GetWorldPoint(Input.mousePosition));
        else if (Input.GetMouseButton(0) && isDrawing) ProcessSample(GetWorldPoint(Input.mousePosition));
        else if (Input.GetMouseButtonUp(0) && isDrawing) EndDraw();
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
    #endregion

    #region Drawing lifecycle
   void BeginDraw(Vector3 start)
{
    if (currentStroke >= strokes.Count) return;
    LetterStroke stroke = strokes[currentStroke];

    //Allow starting near any point within tolerance of the first segment, not just exact P0
    Vector3 startPoint = stroke.GetStartPoint();
    if (Vector3.Distance(start, startPoint) > stroke.tolerance + startTolerance)
    {
        PlayError();
        return;
    }

    GameObject go = Instantiate(userLinePrefab, Vector3.zero, Quaternion.identity);
    go.SetActive(true);
    currentLine = go.GetComponent<LineRenderer>();
    if (currentLine == null) { Destroy(go); return; }

    currentPoints.Clear();
    isDrawing = true;
    if (drawingAudioSource != null && drawingClip != null)
{
    drawingAudioSource.clip = drawingClip;
    drawingAudioSource.loop = true;      // So it continues while drawing
    drawingAudioSource.Play();
}
    AddPointToLine(start);
    stroke.SamplePoint(start);
}

void ProcessSample(Vector3 sample)
{
    if (!isDrawing || currentStroke >= strokes.Count) return;
    LetterStroke stroke = strokes[currentStroke];

    bool onPath = stroke.IsSampleOnPath(sample, out _, out _); // use segment-based check

    if (onPath)
    {
        SetLineColor(onPathColor);

        if (currentPoints.Count == 0 ||
            Vector3.Distance(currentPoints[currentPoints.Count - 1], sample) >= minPointDistance)
        {
            AddPointToLine(sample);
        }

        stroke.SamplePoint(sample);

        if (stroke.IsComplete())
            OnStrokeComplete();
    }
    else
    {
        CancelCurrentStroke(); //cancel immediately when outside tolerance
    }
}


    void EndDraw()
    {
      if (!isDrawing) return;

    // If stroke is incomplete when player releases -> cancel it
    if (currentStroke < strokes.Count && !strokes[currentStroke].IsComplete())
    {
        CancelCurrentStroke();
    }
        isDrawing = false;
              if (drawingAudioSource != null && drawingAudioSource.isPlaying)
                  drawingAudioSource.Stop();
        currentLine = null;
        currentPoints.Clear();
    }

    void CancelCurrentStroke()
    {
        // 🔥 Erase messy line
        if (currentLine != null)
            Destroy(currentLine.gameObject);

        // Reset stroke progress & re-enable guide
        strokes[currentStroke].ResetStroke();

        currentLine = null;
        currentPoints.Clear();
        isDrawing = false;
        if (drawingAudioSource != null && drawingAudioSource.isPlaying)
            drawingAudioSource.Stop();


        PlayError();
    }
    #endregion

    #region Helpers
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
    #endregion

    void OnStrokeComplete()
    {
        if (correctClipSource != null && correctClip != null)
            correctClipSource.PlayOneShot(correctClip);

        LetterStroke s = strokes[currentStroke];

        // Hide guide for this stroke
        if (s.guideLine != null) s.guideLine.gameObject.SetActive(false);
        if (s.guideSprite != null) s.guideSprite.enabled = false;
  

        // Destroy messy user line
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
        if (currentStroke >= strokes.Count)
            OnAllStrokesComplete();
    }

    void OnAllStrokesComplete()
    {

    Debug.Log("Letter complete!");
    
    if (nextButton != null)
        nextButton.interactable = true;
       if (Celebration_SoundSource != null && Celebration_Sound != null)
{
    Celebration_SoundSource.clip = Celebration_Sound;
    Celebration_SoundSource.loop = true;      // So it continues while drawing
    Celebration_SoundSource.Play();
}

    isCompleted = true;

    // Start particle system and celebration
    if (completionParticles != null && !completionParticles.isPlaying)
        completionParticles.Play();

    if (celebrationAnimator != null)
        celebrationAnimator.SetBool("isCelebrating", true); // Make sure your Animator has a bool parameter called "isCelebrating"


    }

    public void ResetAll()
    {
        currentStroke = 0;
        if (Celebration_SoundSource != null && Celebration_SoundSource.isPlaying)
                  Celebration_SoundSource.Stop();
            if (completionParticles != null)
            completionParticles.Stop();

    if (celebrationAnimator != null)
        celebrationAnimator.SetBool("isCelebrating", false);

    isCompleted = false;
        foreach (var s in strokes) s.ResetStroke();
        if (nextButton != null) nextButton.interactable = false;
    }

    void PlayError()
    {
        if (errorClipSource != null && errorClip != null)
            errorClipSource.PlayOneShot(errorClip);
    }
}
