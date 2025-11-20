using UnityEngine;
using System.Collections; // ¡Necesario para la Corutina!
using TMPro; // (Probablemente ya lo tienes)
using UnityEngine.UI; // (Probablemente ya lo tienes)

public class GameManager : MonoBehaviour
{
    // ... (Aquí va todo tu código existente) ...


    // --- ¡AÑADE ESTAS NUEVAS LÍNEAS! ---

    [Header("UI del Panel de Información")]
    public CanvasGroup infoPanelCanvasGroup; // Arrastra tu "Panel_Informacion" aquí
    private Coroutine activeInfoFadeCoroutine;

    [Header("UI Panel de Bienvenida")]
    public CanvasGroup bienvenidaPanelCanvasGroup;
    private Coroutine activeBienvenidaFadeCoroutine;

    // Esta es la "llave" que guardaremos en la memoria del teléfono
    private const string HA_ABIERTO_ANTES_KEY = "haAbiertoLaAppPorPrimeraVez";
    // ------------------------------------

    void Start()
    {
        // ... (Tu código Start existente) ...

        // Asegurarse de que el panel de info esté oculto al iniciar
        if (infoPanelCanvasGroup != null)
        {
            infoPanelCanvasGroup.alpha = 0f;
            infoPanelCanvasGroup.interactable = false;
            infoPanelCanvasGroup.blocksRaycasts = false;
        }

        if (PlayerPrefs.GetInt(HA_ABIERTO_ANTES_KEY, 0) == 0)
        {
            // --- Es la Primera Vez ---
            // 1. Mostrar el panel
            ShowBienvenidaPanel(true);
            
            // 2. "Marcar" que ya lo abrimos
            PlayerPrefs.SetInt(HA_ABIERTO_ANTES_KEY, 1);
            PlayerPrefs.Save(); // Guardar los cambios en la memoria
        }
        else
        {
            // --- Ya NO es la Primera Vez ---
            // Ocultar el panel inmediatamente, sin animación
            bienvenidaPanelCanvasGroup.alpha = 0f;
            bienvenidaPanelCanvasGroup.interactable = false;
            bienvenidaPanelCanvasGroup.blocksRaycasts = false;
        }
    }

    // --- ¡AÑADE ESTAS DOS NUEVAS FUNCIONES! ---

    // Función pública que llamarán tus botones
    public void ShowInfoPanel(bool show)
    {
        // Detener animación anterior si la hay
        if (activeInfoFadeCoroutine != null)
        {
            StopCoroutine(activeInfoFadeCoroutine);
        }

        // Empezar la nueva animación
        float duration = 0.2f; // 0.2 segundos de fade
        activeInfoFadeCoroutine = StartCoroutine(FadeInfoPanel(show, duration));
    }

    // La Corutina que hace la animación
    private IEnumerator FadeInfoPanel(bool show, float duration)
    {
        float startTime = Time.time;
        float startAlpha = infoPanelCanvasGroup.alpha;
        float targetAlpha = show ? 1.0f : 0.0f;

        // Activar interacción al MOSTRAR
        if (show)
        {
            infoPanelCanvasGroup.interactable = true;
            infoPanelCanvasGroup.blocksRaycasts = true;
        }

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float newAlpha = Mathf.SmoothStep(startAlpha, targetAlpha, t);
            infoPanelCanvasGroup.alpha = newAlpha;
            yield return null; // Espera al siguiente frame
        }

        // Asegurarse de que el valor final sea exacto
        infoPanelCanvasGroup.alpha = targetAlpha;

        // Desactivar interacción al OCULTAR
        if (!show)
        {
            infoPanelCanvasGroup.interactable = false;
            infoPanelCanvasGroup.blocksRaycasts = false;
        }

        activeInfoFadeCoroutine = null;
    }

    public void ShowBienvenidaPanel(bool show)
    {
        if (activeBienvenidaFadeCoroutine != null)
        {
            StopCoroutine(activeBienvenidaFadeCoroutine);
        }
        activeBienvenidaFadeCoroutine = StartCoroutine(FadeBienvenidaPanel(show, 0.2f));
    }

    // La Corutina de animación (es un clon de las otras)
    private IEnumerator FadeBienvenidaPanel(bool show, float duration)
    {
        float startTime = Time.time;
        float startAlpha = bienvenidaPanelCanvasGroup.alpha;
        float targetAlpha = show ? 1.0f : 0.0f;

        if (show)
        {
            bienvenidaPanelCanvasGroup.interactable = true;
            bienvenidaPanelCanvasGroup.blocksRaycasts = true;
        }

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float newAlpha = Mathf.SmoothStep(startAlpha, targetAlpha, t);
            bienvenidaPanelCanvasGroup.alpha = newAlpha;
            yield return null;
        }

        bienvenidaPanelCanvasGroup.alpha = targetAlpha;

        if (!show)
        {
            bienvenidaPanelCanvasGroup.interactable = false;
            bienvenidaPanelCanvasGroup.blocksRaycasts = false;
        }
        activeBienvenidaFadeCoroutine = null;
    }

    public void AbrirPaginaWeb(string url)
    {
        // Esta es la línea mágica que abre el navegador del celular
        Application.OpenURL(url);
    }
}