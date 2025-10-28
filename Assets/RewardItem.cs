using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

public class RewardItem : MonoBehaviour
{
    [Header("Configuración")]
    public int Costo = 10;
    public Button Button;
    public float duracionDesvanecimiento = 0.5f;

    [Header("Estado")]
    [SerializeField] private bool puedeInteractuar = true;

    private CanvasGroup canvasGroup;
    private UnityEngine.UI.Image buttonImage;

    void Start()
    {
        // Auto-asignar componentes si no están configurados
        if (Button == null) Button = GetComponent<Button>();
        if (Button == null)
        {
            UnityEngine.Debug.LogError("No se encontró Button en " + gameObject.name);
            return;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        buttonImage = Button.GetComponent<UnityEngine.UI.Image>();
        ActualizarEstado(); // Estado inicial
    }

    public void Setup(UnityAction<int> onRedeemAction)
    {
        if (Button == null) return;

        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(() =>
        {
            if (GlobalPointsManager.Instance.puntos >= Costo)
            {
                onRedeemAction?.Invoke(Costo);
                StartCoroutine(DesvanecerBoton());
            }
        });
    }

    private void ActualizarEstado()
    {
        if (GlobalPointsManager.Instance == null || Button == null) return;

        puedeInteractuar = GlobalPointsManager.Instance.puntos >= Costo;
        Button.interactable = puedeInteractuar;
        canvasGroup.alpha = puedeInteractuar ? 1f : 0.5f; // Efecto visual

        UnityEngine.Debug.Log($"{name} - Interactuable: {puedeInteractuar} (Puntos: {GlobalPointsManager.Instance.puntos}, Costo: {Costo})");
    }

    IEnumerator DesvanecerBoton()
    {
        if (buttonImage == null) yield break;

        Button.interactable = false;
        float tiempo = 0f;

        while (tiempo < duracionDesvanecimiento)
        {
            float alpha = Mathf.Lerp(1f, 0f, tiempo / duracionDesvanecimiento);
            buttonImage.color = new Color(1, 1, 1, alpha);
            tiempo += Time.deltaTime;
            yield return null;
        }

        Button.gameObject.SetActive(false);
    }

    public void Revalidar()
    {
        ActualizarEstado();
    }
}