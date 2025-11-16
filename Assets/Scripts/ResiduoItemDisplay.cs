using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PolyAndCode.UI; // ¡Importante para ICell!

// Implementa ICell para el Recyclable Scroll Rect
public class ResiduoItemDisplay : MonoBehaviour, ICell
{
    // --- Campos para conectar en el Inspector del Prefab ---
    public Image itemImage;
    public TextMeshProUGUI nombreText;
    public TextMeshProUGUI tipoText;
    
    private Residuo currentResiduoData;

    // Esta función es llamada por el ResiduoListManager (en SetCell)
    public void Setup(Residuo residuoData)
    {
        currentResiduoData = residuoData;

        // 1. Asignar los textos
        nombreText.text = residuoData.nombre;
        tipoText.text = residuoData.categoria;

        // 2. Cargar la imagen (con lógica de default)
        string imageBasePath = "DB/img/";
        string imageName = residuoData.id.ToString();
        string resourcePath = imageBasePath + imageName;
            
        Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);

        if (loadedSprite == null)
        {
            // Si no se encontró la específica, cargar "default"
            string defaultImagePath = imageBasePath + "default";
            loadedSprite = Resources.Load<Sprite>(defaultImagePath);
        }

        if (loadedSprite != null)
        {
            itemImage.sprite = loadedSprite;
        }
        else
        {
            Debug.LogWarning("¡No se encontró ni el sprite " + imageName + " ni el sprite 'default'!");
        }
    }
}