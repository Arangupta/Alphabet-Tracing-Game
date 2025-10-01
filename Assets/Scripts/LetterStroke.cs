using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class LetterStroke : MonoBehaviour
{
    [Tooltip("If left empty, the script will auto-collect child transforms (in hierarchy order).")]
    public List<Transform> pointTransforms = new List<Transform>();

    [Header("Validation")]
    [Tooltip("How far (world units) the user may be from the path to count as 'on path'")]
    public float tolerance = 0.30f;

    [Range(0.5f, 1f)]
    public float requiredProgress = 0.85f; // how much of the stroke must be covered

    [Header("Guide (optional)")]
    public LineRenderer guideLine;
    public SpriteRenderer guideSprite;

  

    // Internal
    private Vector3[] points;
    private float[] cumLengths;
    private float totalLength;
    private float maxProgress = 0f;

    void Awake()
    {
        CollectPoints();
        BuildLengths();
        ResetStroke();
        if (guideLine != null) DrawGuide();
    }

    public void CollectPoints()
    {
        if (pointTransforms == null) pointTransforms = new List<Transform>();

        if (pointTransforms.Count == 0)
        {
            List<Transform> children = new List<Transform>();
            foreach (Transform t in transform) children.Add(t);
            children.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));
            pointTransforms = children;
        }

        points = new Vector3[pointTransforms.Count];
        for (int i = 0; i < pointTransforms.Count; i++)
            points[i] = pointTransforms[i].position;
    }

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

    void DrawGuide()
    {
        if (guideLine == null || points == null) return;
        guideLine.positionCount = points.Length;
        guideLine.SetPositions(points);
    }

    public Vector3[] GetPoints() => points;

    public Vector3 GetStartPoint()
    {
        return (points != null && points.Length > 0) ? points[0] : Vector3.zero;
    }

    // Check progress along stroke
    public void SamplePoint(Vector3 sample)
    {
        if (points == null || points.Length < 2) return;

        if (IsSampleOnPath(sample, out float accum, out float dist))
        {
            if (accum > maxProgress)
                maxProgress = accum;
        }
    }

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

    public float GetProgressRatio()
    {
        if (totalLength <= Mathf.Epsilon) return 0f;
        return Mathf.Clamp01(maxProgress / totalLength);
    }

    public bool IsComplete()
    {
        return GetProgressRatio() >= requiredProgress;
    }

    public void ResetStroke()
    {
        maxProgress = 0f;
        if (guideLine != null) guideLine.gameObject.SetActive(true);
        if (guideSprite != null) guideSprite.enabled = true;
    }

    // Helper
    Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p, out float t)
    {
        Vector3 ab = b - a;
        float ab2 = Vector3.Dot(ab, ab);
        if (ab2 == 0f)
        {
            t = 0f;
            return a;
        }
        t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab2);
        return a + ab * t;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        CollectPoints();
        BuildLengths();
        if (guideLine != null) DrawGuide();
    }
#endif
}
