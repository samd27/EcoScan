using UnityEngine;
using UnityEngine.UI; // ¡Importante! Para que funcione "Image"
using System.Collections.Generic; // Para usar List<>
using System.Linq; // ¡El más importante! Para filtrar y ordenar
using TMPro; // Para el InputField y los textos

public class ResiduoListManager : MonoBehaviour
{
    // --- CONEXIONES DEL MANAGER ---
    [Header("Conexiones de Datos")]
    public GameObject residuoItemPrefab;
    public Transform contentParent;
    public string jsonFilePath = "DB/residuos";

    // --- CONEXIONES DE UI PRINCIPAL ---
    [Header("UI Filtros Principales")]
    public TMP_InputField searchInputField;
    public GameObject searchIcon;
    public GameObject filtrosPanel;

    // --- CONEXIONES DE BOTONES DE CATEGORÍA ---
    [Header("Botones de Categoría")]
    public Image botonOrganicoImage;
    public TextMeshProUGUI botonOrganicoText; // El campo sigue existiendo (no da error)
    public Image botonTodosImage;
    public TextMeshProUGUI botonTodosText; // El campo sigue existiendo (no da error)
    public Image botonInorganicoImage;
    public TextMeshProUGUI botonInorganicoText; // El campo sigue existiendo (no da error)

    // --- RECURSOS DE ESTILO DE BOTONES ---
    [Header("Estilos de Botones")]
    public Sprite spriteBotonNormal;
    public Sprite spriteBotonSolido;
    public Color colorActivo;
    public Color colorInactivo;

    // --- VARIABLES DE ESTADO PRIVADAS ---
    private List<Residuo> allResiduos = new List<Residuo>();
    private string currentSearchText = "";
    private string currentCategory = "ALL";
    private bool sortAZ = false;

    // --- FUNCIONES DE UNITY ---

    void Start()
    {
        if (filtrosPanel != null)
            filtrosPanel.SetActive(false);

        if (searchInputField != null)
            searchInputField.onValueChanged.AddListener(SetSearchText);
        
        currentCategory = "ALL"; 
        LoadResiduosFromJson();
        UpdateCategoryButtonsVisuals(); 
        ApplyFiltersAndSort();
    }

    // --- FUNCIONES DE LÓGICA DE DATOS ---

    void LoadResiduosFromJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFilePath);
        if (jsonFile == null) {
            Debug.LogError("Error: No se encontró el JSON en Resources/" + jsonFilePath);
            return;
        }

        string jsonText = "{\"residuos\":" + jsonFile.text + "}";
        ResiduoList residuoList = JsonUtility.FromJson<ResiduoList>(jsonText);

        if (residuoList == null || residuoList.residuos == null) {
             Debug.LogError("Error al deserializar el JSON.");
             return;
        }
        allResiduos = new List<Residuo>(residuoList.residuos);
    }

    // --- FUNCIONES PÚBLICAS (Llamadas por los botones) ---

    public void SetSearchText(string newText)
    {
        currentSearchText = newText.ToLower();
        if (searchIcon != null)
        {
            searchIcon.SetActive(string.IsNullOrEmpty(newText));
        }
        ApplyFiltersAndSort();
    }

    public void SetCategoryFilter(string category)
    {
        currentCategory = category;
        UpdateCategoryButtonsVisuals();
        ApplyFiltersAndSort();
    }

    public void ToggleSortAZ()
    {
        sortAZ = !sortAZ;
        ApplyFiltersAndSort();
    }

    public void ShowFilterPanel(bool show)
    {
        if (filtrosPanel != null)
            filtrosPanel.SetActive(show);
    }

    // --- FUNCIONES INTERNAS DE ACTUALIZACIÓN ---

    void ApplyFiltersAndSort()
    {
        IEnumerable<Residuo> filteredList = allResiduos;

        if (currentCategory != "ALL")
        {
            filteredList = filteredList.Where(r => r.categoria == currentCategory);
        }

        if (!string.IsNullOrEmpty(currentSearchText))
        {
            filteredList = filteredList.Where(r => 
                r.nombre.ToLower().Contains(currentSearchText) || 
                r.keywords.ToLower().Contains(currentSearchText)
            );
        }

        if (sortAZ)
        {
            filteredList = filteredList.OrderBy(r => r.nombre);
        }
        
        UpdateDisplay(filteredList.ToList());
    }

    void UpdateDisplay(List<Residuo> listToDisplay)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Residuo residuo in listToDisplay)
        {
            GameObject newItem = Instantiate(residuoItemPrefab, contentParent);
            newItem.GetComponent<ResiduoItemDisplay>().Setup(residuo);
        }
    }

    // --- ¡FUNCIÓN MODIFICADA SIN CAMBIO DE COLOR DE TEXTO! ---
    void UpdateCategoryButtonsVisuals()
    {
        // 1. Poner TODOS en estado "Inactivo"
        if (botonOrganicoImage != null) {
            botonOrganicoImage.sprite = spriteBotonNormal;
            botonOrganicoImage.color = colorInactivo;
        }
        // No se cambia el color del texto

        if (botonTodosImage != null) {
            botonTodosImage.sprite = spriteBotonNormal;
            botonTodosImage.color = colorInactivo;
        }
        // No se cambia el color del texto

        if (botonInorganicoImage != null) {
            botonInorganicoImage.sprite = spriteBotonNormal;
            botonInorganicoImage.color = colorInactivo;
        }
        // No se cambia el color del texto

        // 2. Poner el estado "Activo" SÓLO al botón correcto
        switch (currentCategory)
        {
            case "ORGANICO":
                if (botonOrganicoImage != null) {
                    botonOrganicoImage.sprite = spriteBotonSolido;
                    botonOrganicoImage.color = colorActivo;
                }
                break;
            case "INORGANICO":
                if (botonInorganicoImage != null) {
                    botonInorganicoImage.sprite = spriteBotonSolido;
                    botonInorganicoImage.color = colorActivo;
                }
                break;
            case "ALL":
            default:
                if (botonTodosImage != null) {
                    botonTodosImage.sprite = spriteBotonSolido;
                    botonTodosImage.color = colorActivo;
                }
                break;
        }
    }
}