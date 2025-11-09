using UnityEngine;
using UnityEngine.UI;
using TMPro; // Asegúrate de tener TextMeshPro

public class ResiduoItemDisplay : MonoBehaviour
{
    // --- Campos para conectar en el Inspector ---
    public Image itemImage;
    public TextMeshProUGUI nombreText;
    public TextMeshProUGUI tipoText;
    // ------------------------------------------

    private Residuo currentResiduoData;

    // --- ¡FUNCIÓN SETUP SIMPLIFICADA Y CON DEFAULT! ---
    public void Setup(Residuo residuoData)
    {
        currentResiduoData = residuoData;

        // 1. Asignar los textos (esto sigue igual)
        nombreText.text = residuoData.nombre;
        tipoText.text = residuoData.categoria;

        // 2. Cargar la imagen (¡NUEVA LÓGICA!)
        
        // 2a. Definir la ruta base dentro de Resources
        string imageBasePath = "DB/img/";
            
        // 2b. Usar el ID del residuo como nombre de archivo. ¡Mucho más simple!
        string imageName = residuoData.id.ToString();
        string resourcePath = imageBasePath + imageName;
            
        // 2c. Intentar cargar el Sprite específico (ej. "DB/img/1", "DB/img/2", etc.)
        // (Resources.Load no necesita la extensión .png o .jpg)
        Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);

        // 2d. ¡LÓGICA DE IMAGEN DEFAULT!
        // Si no se encontró la imagen específica (es null)...
        if (loadedSprite == null)
        {
            // ...avisar en la consola (opcional pero útil)...
            Debug.LogWarning("No se encontró imagen: " + resourcePath + 
                             ". Se intentará cargar 'default'.");
                             
            // ...e intentar cargar la imagen "default" de la misma carpeta.
            string defaultImagePath = imageBasePath + "default";
            loadedSprite = Resources.Load<Sprite>(defaultImagePath);
        }

        // 2e. Asignar el sprite (ya sea el específico o el default)
        if (loadedSprite != null)
        {
            itemImage.sprite = loadedSprite;
        }
        else
        {
            // Esto solo pasará si falla la específica Y la default
            Debug.LogError("¡ERROR CRÍTICO! No se encontró ni la imagen " + resourcePath + 
                           " ni la imagen 'default' en Assets/Resources/DB/img/");
        }
    }

    // Esta función se queda exactamente igual
    public void OnItemClicked()
    {
        if (currentResiduoData != null)
        {
            Debug.Log("Has hecho clic en: " + currentResiduoData.nombre);
            // Aquí llamarás a tu Popup de detalles
        }
    }
}