using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // <-- ¡MUY IMPORTANTE AÑADIR ESTO!

public class SmartNavBarController : MonoBehaviour
{
    [Header("Elementos de la NavBar")]
    public RectTransform pildoraActiva; // La imagen verde
    public Button botonInicio;
    public Button botonListado;

    [Header("Posiciones (Coordenadas X)")]
    public float posicionX_Inicio; 
    public float posicionX_Listado;

    // --- ¡NUEVAS VARIABLES AQUÍ! ---
    [Header("Gráficos de Botones")]
    public Image iconoInicio;
    public TextMeshProUGUI textoInicio;
    public Image iconoListado;
    public TextMeshProUGUI textoListado;

    [Header("Colores de Estado")]
    public Color colorActivo;
    public Color colorInactivo;
    // --- FIN DE NUEVAS VARIABLES ---

    [Header("Configuración de Animación")]
    public float duracionAnimacion = 0.3f;

   void Awake()
    {
        // --- ¡AÑADIMOS ESTA COMPROBACIÓN! ---
        // Si el botonInicio existe en esta escena, conéctalo.
        if (botonInicio != null)
        {
            botonInicio.onClick.AddListener(() => OnNavButtonClick("Inicio"));
        }

        // Si el botonListado existe en esta escena, conéctalo.
        if (botonListado != null)
        {
            botonListado.onClick.AddListener(() => OnNavButtonClick("Listado"));
        }
    }

    // --- START ACTUALIZADO ---
    void Start()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // --- ¡ASEGÚRATE DE QUE ESTOS NOMBRES DE ESCENA SEAN LOS TUYOS! ---
        if (currentSceneName == "MainMenu") 
        {
            MovePillTo(posicionX_Inicio);
            SetButtonState("Inicio"); // <-- Línea añadida
        }
        else if (currentSceneName == "ListadoScene") // Reemplaza "ListadoScene"
        {
            MovePillTo(posicionX_Listado);
            SetButtonState("Listado"); // <-- Línea añadida
        }
    }

    // --- ONNAVBUTTONCLICK ACTUALIZADO ---
    public void OnNavButtonClick(string buttonName)
    {
        // --- ¡ASEGÚRATE DE QUE ESTOS NOMBRES DE ESCENA SEAN LOS TUYOS! ---
        switch (buttonName)
        {
            case "Inicio":
                StartCoroutine(AnimatePillAndLoadScene(posicionX_Inicio, "MainMenu", "Inicio")); // <-- Parámetro añadido
                break;
            case "Listado":
                StartCoroutine(AnimatePillAndLoadScene(posicionX_Listado, "ListadoScene", "Listado")); // <-- Parámetro añadido
                break;
        }
    }

    // --- CORRUTINA ACTUALIZADA ---
    IEnumerator AnimatePillAndLoadScene(float targetX, string sceneName, string newState) // <-- Parámetro añadido
    {
        // Cambiamos el color AL INSTANTE de hacer clic
        SetButtonState(newState); // <-- Línea añadida

        // Espera a que la animación de la píldora termine
        yield return StartCoroutine(AnimatePill(targetX));
        
        // Carga la nueva escena
        SceneManager.LoadScene(sceneName);
    }

    // --- ¡NUEVA FUNCIÓN AQUÍ! ---
    void SetButtonState(string activeState)
    {
        if (activeState == "Inicio")
        {
            // Pone "Inicio" en Activo y "Listado" en Inactivo
            iconoInicio.color = colorActivo;
            textoInicio.color = colorActivo;
            
            iconoListado.color = colorInactivo;
            textoListado.color = colorInactivo;
        }
        else if (activeState == "Listado")
        {
            // Pone "Inicio" en Inactivo y "Listado" en Activo
            iconoInicio.color = colorInactivo;
            textoInicio.color = colorInactivo;

            iconoListado.color = colorActivo;
            textoListado.color = colorActivo;
        }
    }
    // --- FIN DE NUEVA FUNCIÓN ---

    // (El resto de funciones: AnimatePill y MovePillTo se quedan igual)
    #region Funciones de Movimiento (Sin cambios)
    
    IEnumerator AnimatePill(float targetX)
    {
        Vector2 startPosition = pildoraActiva.anchoredPosition;
        Vector2 targetPosition = new Vector2(targetX, startPosition.y); 

        float elapsedTime = 0f;

        while (elapsedTime < duracionAnimacion)
        {
            pildoraActiva.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, (elapsedTime / duracionAnimacion));
            elapsedTime += Time.deltaTime;
            yield return null; 
        }

        pildoraActiva.anchoredPosition = targetPosition;
    }

    void MovePillTo(float targetX)
    {
        Vector2 newPos = pildoraActiva.anchoredPosition;
        newPos.x = targetX;
        pildoraActiva.anchoredPosition = newPos;
    }
    
    #endregion
}