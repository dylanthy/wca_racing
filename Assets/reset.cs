using UnityEngine;
using UnityEngine.SceneManagement;

public class reset : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // R: reload scene, best time is KEPT
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Escape: reload scene, best time is KEPT
            // If you want Escape to wipe the record instead, call RaceSessionData.Clear() here
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
