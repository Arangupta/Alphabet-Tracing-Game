using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages switching between letter prefabs (A–Z).
/// Handles spawning letters at a defined point and navigation with Next/Previous buttons.
/// </summary>
public class LetterManager : MonoBehaviour
{
    [Header("Letter Settings")]
    [Tooltip("Assign all 26 letter prefabs here in A–Z order.")]
    public List<GameObject> letterPrefabs;

    [Tooltip("Where the letter will be spawned in the scene.")]
    public Transform spawnPoint;

    [Header("Navigation Buttons")]
    public Button nextButton;   // Button to go to the next letter
    public Button prevButton;   // Button to go to the previous letter

    // Runtime state
    private int currentIndex = 0;              // Which letter we are on (0 = A)
    private GameObject currentLetterInstance; // Reference to the active letter prefab

    void Start()
    {
        // Load the very first letter (index 0 = "A")
        LoadLetter(currentIndex);

        // Hook up button listeners (if assigned in the Inspector)
        if (nextButton != null)
            nextButton.onClick.AddListener(NextLetter);

        if (prevButton != null)
            prevButton.onClick.AddListener(PreviousLetter);
    }

    /// <summary>
    /// Loads a letter prefab by index, destroys the old one if it exists.
    /// </summary>
    void LoadLetter(int index)
    {
        // Remove previously spawned letter
        if (currentLetterInstance != null)
            Destroy(currentLetterInstance);

        // Spawn new letter if the index is valid
        if (index >= 0 && index < letterPrefabs.Count)
        {
            currentLetterInstance = Instantiate(
                letterPrefabs[index],       // prefab to spawn
                spawnPoint.position,        // spawn location
                Quaternion.identity         // no rotation
            );
        }
    }

    /// <summary>
    /// Loads the next letter (if available).
    /// </summary>
    public void NextLetter()
    {
        if (currentIndex < letterPrefabs.Count - 1)
        {
            currentIndex++;
            LoadLetter(currentIndex);
        }
    }

    /// <summary>
    /// Loads the previous letter (if available).
    /// </summary>
    public void PreviousLetter()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            LoadLetter(currentIndex);
        }
    }

    /// <summary>
    /// Resets the sequence back to the first letter (A).
    /// </summary>
    public void ResetToFirstLetter()
    {
        currentIndex = 0;
        LoadLetter(currentIndex);
    }
}
