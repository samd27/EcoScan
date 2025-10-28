using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AjustarPantalla : MonoBehaviour
{
    [Header("Opciones")]
    public Camera camaraPrincipal;
    public bool mantenerProporcion = true;
    public float margenExtra = 1.1f;

    void Start()
    {
        if (camaraPrincipal == null)
            camaraPrincipal = Camera.main;

        AjustarFondo();
    }

    void AjustarFondo()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            // Solución: Usar UnityEngine.Debug explícitamente
            UnityEngine.Debug.LogError("Error: El objeto necesita un SpriteRenderer con sprite asignado");
            return;
        }

        float anchoSprite = sr.sprite.bounds.size.x;
        float altoSprite = sr.sprite.bounds.size.y;
        float altoPantalla = camaraPrincipal.orthographicSize * 2 * margenExtra;
        float anchoPantalla = altoPantalla * camaraPrincipal.aspect;

        Vector3 nuevaEscala = transform.localScale;
        nuevaEscala.x = anchoPantalla / anchoSprite;
        nuevaEscala.y = altoPantalla / altoSprite;

        if (mantenerProporcion)
        {
            float escalaMinima = Mathf.Min(nuevaEscala.x, nuevaEscala.y);
            nuevaEscala = Vector3.one * escalaMinima;
        }

        transform.localScale = nuevaEscala;

        UnityEngine.Debug.Log($"Fondo ajustado. Escala: {nuevaEscala}");
    }
}