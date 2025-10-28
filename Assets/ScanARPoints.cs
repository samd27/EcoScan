using UnityEngine;
using UnityEngine.UI;

public class ScanARPoints : MonoBehaviour
{
    [SerializeField] private int puntosASumar = 10;

    void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn == null)
        {
            UnityEngine.Debug.LogError("❌ No se encontró componente Button", gameObject);
            return;
        }

        btn.onClick.AddListener(() =>
        {
            if (GlobalPointsManager.Instance == null)
            {
                UnityEngine.Debug.LogError("❌ GlobalPointsManager no existe");
                return;
            }

            GlobalPointsManager.Instance.SumarPuntos(puntosASumar);

            // Feedback visual opcional
            StartCoroutine(AnimarBoton());
        });
    }

    System.Collections.IEnumerator AnimarBoton()
    {
        transform.localScale = Vector3.one * 1.2f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = Vector3.one;
    }
}