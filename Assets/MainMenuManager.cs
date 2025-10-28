using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void IrAEscenaAR()
    {
        SceneManager.LoadScene("ARScene");
    }

    public void IrAScanAR()
    {
        SceneManager.LoadScene("ScanAR");
    }

   

    public void IrARecompensas()
    {
        SceneManager.LoadScene("Recompensas");
    }

    public void SalirApp()
    {
        Application.Quit();
        Debug.Log("Aplicación cerrada");
    }

    public void VolverAlMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
