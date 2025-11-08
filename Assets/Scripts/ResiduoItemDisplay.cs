using UnityEngine;
using UnityEngine.UI;
using TMPro; // Asegúrate de tener TextMeshPro importado

public class ResiduoItemDisplay : MonoBehaviour
{
    // --- Asigna esto en el Inspector del Prefab ---
    public Image itemImage;
    public TextMeshProUGUI nombreText;
    public TextMeshProUGUI tipoText; // El texto que antes decía "Inorgánico"
    // -----------------------------------------------

    private Residuo currentResiduoData;

    // Esta función es llamada por el script generador
    public void Setup(Residuo residuoData)
    {
        currentResiduoData = residuoData;

        // 1. Asignar los textos
        nombreText.text = residuoData.nombre;
        tipoText.text = residuoData.categoria;

        // 2. Cargar la imagen desde la carpeta Resources
        if (!string.IsNullOrEmpty(residuoData.ruta_imagen))
        {
            // Tu JSON dice "residuo_img.png". Resources.Load necesita
            // el nombre SIN la extensión ".png".
            string imageName = residuoData.ruta_imagen.Split('.')[0];

            Sprite loadedSprite = Resources.Load<Sprite>(imageName);

            if (loadedSprite != null)
            {
                itemImage.sprite = loadedSprite;
            }
            else
            {
                Debug.LogWarning("No se pudo cargar la imagen: " + imageName + " desde Resources.");
            }
        }
    }

    // 3. Configurar el clic del botón (¡ya listo!)
    public void OnItemClicked()
    {
        if (currentResiduoData != null)
        {
            Debug.Log("Has hecho clic en: " + currentResiduoData.nombre);
            // Aquí es donde, en el futuro, llamarás a la función
            // que abre el recuadro de detalles.
            // Ejemplo: GameManager.Instance.ShowDetails(currentResiduoData);
        }
    }
}