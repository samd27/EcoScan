using UnityEngine;
using UnityEngine.SceneManagement; // ¡Esta línea es ESENCIAL para cargar escenas!

public class SceneLoader : MonoBehaviour
{
    // Esta es la función que llamará nuestro botón
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Función pública para cerrar la aplicación
    public void SalirDeLaAplicacion()
    {
        // Esto imprime en la consola de Unity, para que sepas que funciona
        Debug.Log("¡CERRANDO LA APLICACIÓN!");
        
        // Esta es la línea que cierra la app
        Application.Quit();
    }
}