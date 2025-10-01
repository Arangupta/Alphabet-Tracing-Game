using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LetterManager : MonoBehaviour
{
    [Header("Letter Prefabs (A–Z in order)")]
    public List<GameObject> letterPrefabs;   // assign all 26 prefabs in inspector
    public Transform spawnPoint;             // where the letters should appear
    public Button nextButton;
    public Button prevButton;

    private int currentIndex = 0;
    private GameObject currentLetterInstance;

    void Start()
    {
        LoadLetter(currentIndex);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextLetter);
        if (prevButton != null)
            prevButton.onClick.AddListener(PreviousLetter);
    }

    void LoadLetter(int index)
    {
        // destroy old letter
        if (currentLetterInstance != null)
            Destroy(currentLetterInstance);

        // spawn new one at spawnPoint position without parenting
        if (index >= 0 && index < letterPrefabs.Count)
        {
            currentLetterInstance = Instantiate(letterPrefabs[index], spawnPoint.position, Quaternion.identity);
        }
    }

    public void NextLetter()
    {
        if (currentIndex < letterPrefabs.Count - 1)
        {
            currentIndex++;
            LoadLetter(currentIndex);
        }
    }

    public void PreviousLetter()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            LoadLetter(currentIndex);
        }
    }

    public void ResetToFirstLetter()
    {
        currentIndex = 0;
        LoadLetter(currentIndex);
    }
}
