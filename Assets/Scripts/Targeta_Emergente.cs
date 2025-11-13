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

    // ... (Aquí sigue el resto de tu código) ...
}