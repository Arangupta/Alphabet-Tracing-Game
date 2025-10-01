using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles letter stroke validation by checking user-drawn input 
/// against a predefined path made of points.
/// 
/// - Can automatically collect child transforms as stroke points
/// - Tracks user progress along the path
/// - Provides optional guide visualization with a LineRenderer/Sprite
/// </summary>
[ExecuteInEditMode]
public class LetterStroke : MonoBehaviour
{
    [Tooltip("If left empty, the script will auto-collect child transforms (in hierarchy order).")]
    public List<Transform> pointTransforms = new List<Transform>();

    [Header("Validation")]
    [Tooltip("How far (world units) the user may be from the path to count as 'on path'.")]
    public float tolerance = 0.30f;

    [Range(0.5f, 1f)]
    [Tooltip("Fraction of stroke that must be covered to consider stroke complete.")]
    public float requiredProgress = 0.85f;

    [Header("Guide (optional)")]
    public LineRenderer guideLine;   // Optional visual guide line
    public SpriteRenderer guideSprite; // Optional guide sprite

    // Internal stroke data
    private Vector3[] points;       // World-space stroke points
    private float[] cumLengths;     // Cumulative distance along stroke
    private float totalLength;      // Total stroke length
    private float maxProgress = 0f; // Max distance achieved by user

    /// <summary>
    /// Unity Awake lifecycle method.
    /// Initializes points, builds length data, resets stroke, and draws guides.
    /// </summary>
    void Awake()
    {
        CollectPoints();
        BuildLengths();
        ResetStroke();

        if (guideLine != null)
            DrawGuide();
    }

    /// <summary>
    /// Collects stroke points from assigned transforms or automatically 
    /// from child transforms in hierarchy order.
    /// </summary>
    public void CollectPoints()
    {
        if (pointTransforms == null)
            pointTransforms = new List<Transform>();

        // Auto-collect children if no points are assigned
        if (pointTransforms.Count == 0)
        {
            List<Transform> children = new List<Transform>();

            foreach (Transform t in transform)
                children.Add(t);

            // Sort by sibling index to keep hierarchy order
            children.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));
            pointTransforms = children;
        }

        // Store world positions
        points = new Vector3[pointTransforms.Count];
        for (int i = 0; i < pointTransforms.Count; i++)
            points[i] = pointTransforms[i].position;
    }

    /// <summary>
    /// Builds cumulative length data along the stroke.
    /// </summary>
    void BuildLengths()
    {
        if (points == null || points.Length < 2)
        {
            totalLength = 0f;
            cumLengths = new float[0];
            return;
        }

        cumLengths = new float[points.Length];
        totalLength = 0f;
        cumLengths[0] = 0f;

        for (int i = 1; i < points.Length; i++)
        {
            totalLength += Vector3.Distance(points[i - 1], points[i]);
            cumLengths[i] = totalLength;
        }
    }

    /// <summary>
    /// Draws a guide line using LineRenderer if assigned.
    /// </summary>
    void DrawGuide()
    {
        if (guideLine == null || points == null) return;

        guideLine.positionCount = points.Length;
        guideLine.SetPositions(points);
    }

    /// <summary>
    /// Returns all stroke points.
    /// </summary>
    public Vector3[] GetPoints() => points;

    /// <summary>
    /// Returns the starting point of the stroke.
    /// </summary>
    public Vector3 GetStartPoint()
    {
        return (points != null && points.Length > 0) ? points[0] : Vector3.zero;
    }

    /// <summary>
    /// Samples a user-drawn point, updates max progress if it's on path.
    /// </summary>
    public void SamplePoint(Vector3 sample)
    {
        if (points == null || points.Length < 2) return;

        if (IsSampleOnPath(sample, out float accum, out float dist))
        {
            if (accum > maxProgress)
                maxProgress = accum;
        }
    }

    /// <summary>
    /// Checks if the sampled point is within tolerance of the path.
    /// Outputs cumulative distance and nearest distance.
    /// </summary>
    public bool IsSampleOnPath(Vector3 sample, out float accumDist, out float outDist)
    {
        accumDist = 0f;
        outDist = float.MaxValue;

        if (points == null || points.Length < 2) return false;

        float bestDist = float.MaxValue;
        float bestAccum = 0f;

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];

            // Find closest point on this segment
            Vector3 proj = ClosestPointOnSegment(a, b, sample, out float t);
            float d = Vector3.Distance(sample, proj);

            if (d < bestDist)
            {
                bestDist = d;
                float accumStart = cumLengths[i];
                float distAlongSegment = Vector3.Distance(a, proj);
                bestAccum = accumStart + distAlongSegment;
            }
        }

        accumDist = bestAccum;
        outDist = bestDist;
        return bestDist <= tolerance;
    }

    /// <summary>
    /// Returns the ratio of user progress (0..1).
    /// </summary>
    public float GetProgressRatio()
    {
        if (totalLength <= Mathf.Epsilon) return 0f;
        return Mathf.Clamp01(maxProgress / totalLength);
    }

    /// <summary>
    /// Returns true if user completed the stroke (based on required progress).
    /// </summary>
    public bool IsComplete()
    {
        return GetProgressRatio() >= requiredProgress;
    }

    /// <summary>
    /// Resets stroke progress and re-enables guides.
    /// </summary>
    public void ResetStroke()
    {
        maxProgress = 0f;

        if (guideLine != null) 
            guideLine.gameObject.SetActive(true);

        if (guideSprite != null) 
            guideSprite.enabled = true;
    }

    /// <summary>
    /// Returns the closest point on a segment to given point p.
    /// Outputs t (0=start, 1=end).
    /// </summary>
    Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p, out float t)
    {
        Vector3 ab = b - a;
        float ab2 = Vector3.Dot(ab, ab);

        if (ab2 == 0f)
        {
            t = 0f;
            return a; // Degenerate segment
        }

        t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab2);
        return a + ab * t;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Unity Editor-only: auto-update when values change in Inspector.
    /// </summary>
    void OnValidate()
    {
        CollectPoints();
        BuildLengths();

        if (guideLine != null) 
            DrawGuide();
    }
#endif
}
