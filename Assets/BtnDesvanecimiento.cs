using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BotonDesvanecer : MonoBehaviour
{
    public float duracionDesvanecimiento = 0.5f;
    private Button boton;
    private CanvasGroup canvasGroup;

    void Start()
    {
        boton = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        boton.onClick.AddListener(() => StartCoroutine(Desvanecer()));
    }

    IEnumerator Desvanecer()
    {
        boton.interactable = false;
        float tiempo = 0f;

        while (tiempo < duracionDesvanecimiento)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, tiempo / duracionDesvanecimiento);
            tiempo += Time.deltaTime;
            yield return null;
        }

        gameObject.SetActive(false);
    }
}