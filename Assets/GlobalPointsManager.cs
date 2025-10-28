using UnityEngine;
using UnityEngine.Events;

public class GlobalPointsManager : MonoBehaviour
{
    public static GlobalPointsManager Instance;
    public int puntos = 250; // Puntos iniciales

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            UnityEngine.Debug.Log("✅ GlobalPointsManager inicializado");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public UnityEvent OnPuntosCambiados;

    public void SumarPuntos(int cantidad)
    {
        puntos += cantidad;
        OnPuntosCambiados.Invoke(); // Notifica a los suscriptores
    }
}