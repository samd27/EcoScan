using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PolyAndCode.UI; // ¡LA LÍNEA CLAVE! (Para ICell)

// Asegúrate de que implementa ICell
public class ResiduoItemDisplay : MonoBehaviour, ICell
{
    // ... (todo tu código de Setup, OnItemClicked, etc. va aquí) ...
    
    public Image itemImage;
    public TextMeshProUGUI nombreText;
    public TextMeshProUGUI tipoText;
    
    private Residuo currentResiduoData;

    public void Setup(Residuo residuoData)
    {
        currentResiduoData = residuoData;

        nombreText.text = residuoData.nombre;
        tipoText.text = residuoData.categoria;

        // --- INICIO DE LÓGICA DE IMAGEN ---
        string imageBasePath = "DB/img/";
        string imageName = residuoData.id.ToString();
        string resourcePath = imageBasePath + imageName;
            
        Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);

        if (loadedSprite == null)
        {
            string defaultImagePath = imageBasePath + "default";
            loadedSprite = Resources.Load<Sprite>(defaultImagePath);
        }

        if (loadedSprite != null)
        {
            itemImage.sprite = loadedSprite;
        }
        // --- FIN DE LÓGICA DE IMAGEN ---
    }

    public void OnItemClicked()
    {
        if (currentResiduoData != null)
        {
            Debug.Log("Has hecho clic en: " + currentResiduoData.nombre);
        }
    }
}