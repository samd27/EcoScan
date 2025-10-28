using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;

public class RecompensasManager : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private TMP_Text puntosText;
    [SerializeField] private GameObject qrPanel;

    [Header("Recompensas")]
    [SerializeField] private List<RewardItem> rewardItems;

    void Start()
    {
        StartCoroutine(InicializarConRetraso());
    }

    IEnumerator InicializarConRetraso()
    {
        yield return null; // Espera 1 frame

        // Verificar que GlobalPointsManager esté listo
        if (GlobalPointsManager.Instance == null)
        {
            UnityEngine.Debug.LogError("GlobalPointsManager no encontrado!");
            yield break;
        }

        ActualizarUI();

        if (qrPanel != null)
            qrPanel.SetActive(false);

        // Configurar todos los botones
        foreach (var item in rewardItems)
        {
            if (item != null)
            {
                item.Setup(OnCanjear);
                item.Revalidar(); // Actualización inmediata
            }
        }

        // Suscribirse a cambios de puntos
        GlobalPointsManager.Instance.OnPuntosCambiados.AddListener(ActualizarTodosLosBotones);
    }

    private void OnCanjear(int costo)
    {
        if (GlobalPointsManager.Instance.puntos < costo) return;

        GlobalPointsManager.Instance.puntos -= costo;
        ActualizarUI();
    }

    private void ActualizarTodosLosBotones()
    {
        foreach (var item in rewardItems)
        {
            if (item != null) item.Revalidar();
        }
    }

    private void ActualizarUI()
    {
        if (puntosText != null)
            puntosText.text = $"Puntos: {GlobalPointsManager.Instance.puntos}";
    }

    void OnDestroy()
    {
        if (GlobalPointsManager.Instance != null)
            GlobalPointsManager.Instance.OnPuntosCambiados.RemoveListener(ActualizarTodosLosBotones);
    }
}