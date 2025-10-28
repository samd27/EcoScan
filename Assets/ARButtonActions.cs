using UnityEngine;
using UnityEngine.SceneManagement;

public class ARButtonActions : MonoBehaviour
{
    public void VolverAlMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
