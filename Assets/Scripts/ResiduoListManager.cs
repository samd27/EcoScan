using UnityEngine;
using UnityEngine.UI;
using System.Collections; // ¡NUEVO! Para la Corutina de animación
using System.Collections.Generic;
using System.Linq;
using TMPro;
using PolyAndCode.UI; // Para ICell y RecyclableScrollRect

public class ResiduoListManager : MonoBehaviour, IRecyclableScrollRectDataSource
{
    // --- CONEXIONES DEL MANAGER ---
    public string jsonFilePath = "DB/residuos";

    // --- CONEXIÓN AL ASSET ---
    [Header("Conexión al Asset")]
    public RecyclableScrollRect recyclableScrollRect;

    // --- CONEXIONES DE UI PRINCIPAL ---
    [Header("UI Filtros Principales")]
    public TMP_InputField searchInputField;
    public GameObject searchIcon;
    public CanvasGroup filtrosPanelCanvasGroup; // ¡MODIFICADO! De GameObject a CanvasGroup

    // --- CONEXIONES DE BOTONES DE CATEGORÍA ---
    [Header("Botones de Categoría")]
    public Image botonOrganicoImage;
    public TextMeshProUGUI botonOrganicoText;
    public Image botonTodosImage;
    public TextMeshProUGUI botonTodosText;
    public Image botonInorganicoImage;
    public TextMeshProUGUI botonInorganicoText;

    // --- RECURSOS DE ESTILO DE BOTONES ---
    [Header("Estilos de Botones")]
    public Sprite spriteBotonNormal;
    public Sprite spriteBotonSolido;
    public Color colorActivo;
    public Color colorInactivo;

    // --- CONEXIONES DEL PANEL DE FILTROS ---
    [Header("Conexiones del Panel de Filtros")]
    public GameObject togglePrefab;
    public Transform filterContentParent;

    // --- VARIABLES DE ESTADO PRIVADAS ---
    private List<Residuo> allResiduos = new List<Residuo>();
    private List<Residuo> _filteredResiduoList = new List<Residuo>();
    private string currentSearchText = "";
    private string currentCategory = "ALL";
    private bool sortAZ = false;
    private List<string> selectedMaterialFilters = new List<string>();
    private List<Toggle> instantiatedToggles = new List<Toggle>();
    
    // Para la animación del panel
    private Coroutine activeFadeCoroutine;

    
    // --- FUNCIONES DE UNITY ---
    void Awake()
    {
        if (recyclableScrollRect != null)
        {
            recyclableScrollRect.DataSource = this;
        }
        else
        {
            Debug.LogError("¡No has asignado el RecyclableScrollRect en el Inspector!");
        }

        LoadResiduosFromJson();
        PopulateFilterPanel();
    }

    void Start()
    {
        // --- ¡MODIFICADO! Ocultar panel con CanvasGroup ---
        if (filtrosPanelCanvasGroup != null)
        {
            filtrosPanelCanvasGroup.alpha = 0f; // Invisible
            filtrosPanelCanvasGroup.interactable = false; // No se puede cliquear
            filtrosPanelCanvasGroup.blocksRaycasts = false; // No bloquea clics
        }

        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(SetSearchText);
        }
        
        currentCategory = "ALL"; 
        UpdateCategoryButtonsVisuals(); 
        
        // Aplicar el filtro inicial
        ApplyFiltersAndSort();
    }

    // --- FUNCIONES OBLIGATORIAS DEL ASSET ---

    public int GetItemCount()
    {
        return _filteredResiduoList.Count;
    }

    public void SetCell(ICell cell, int index)
    {
        var item = cell as ResiduoItemDisplay;
        if (item != null && index < _filteredResiduoList.Count)
        {
            item.Setup(_filteredResiduoList[index]);
        }
    }
    
    // --- LÓGICA DE FILTROS Y DATOS (Sin cambios) ---

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

    void ApplyFiltersAndSort()
    {
        IEnumerable<Residuo> filteredListQuery = allResiduos;

        // Aplicar filtros (Categoría, Búsqueda, Material)
        if (currentCategory != "ALL")
        {
            filteredListQuery = filteredListQuery.Where(r => r.categoria == currentCategory);
        }
        if (!string.IsNullOrEmpty(currentSearchText))
        {
            filteredListQuery = filteredListQuery.Where(r => 
                r.nombre.ToLower().Contains(currentSearchText) || 
                r.keywords.ToLower().Contains(currentSearchText)
            );
        }
        if (selectedMaterialFilters.Count > 0)
        {
            filteredListQuery = filteredListQuery.Where(r => 
                selectedMaterialFilters.Contains(r.material)
            );
        }
        if (sortAZ)
        {
            filteredListQuery = filteredListQuery.OrderBy(r => r.nombre);
        }
        
        // Guardar resultado y avisar al asset
        _filteredResiduoList = filteredListQuery.ToList();
        if (recyclableScrollRect != null)
        {
            recyclableScrollRect.ReloadData();
        }
    }
    
    // --- FUNCIONES PÚBLICAS (Llamadas por botones) ---

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

    public void ApplyAndCloseFilters()
    {
        ApplyFiltersAndSort();
        ShowFilterPanel(false); // Llamará a la nueva función de animación
    }

    public void ClearMaterialFilters()
    {
        selectedMaterialFilters.Clear();
        foreach (Toggle t in instantiatedToggles)
        {
            t.isOn = false;
        }
        ApplyFiltersAndSort();
    }

    // --- LÓGICA DEL PANEL DE FILTROS (Poblado) ---

    void PopulateFilterPanel()
    {
        var uniqueMaterials = allResiduos
                                .Where(r => !string.IsNullOrEmpty(r.material))
                                .Select(r => r.material)
                                .Distinct()
                                .OrderBy(m => m);
        instantiatedToggles.Clear();
        foreach (string material in uniqueMaterials)
        {
            GameObject newToggleObj = Instantiate(togglePrefab, filterContentParent);
            Toggle newToggle = newToggleObj.GetComponent<Toggle>();
            TextMeshProUGUI label = newToggleObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = material;
            }
            string mat = material; 
            newToggle.onValueChanged.AddListener((isOn) => {
                OnMaterialToggleChanged(isOn, mat);
            });
            instantiatedToggles.Add(newToggle);
        }
    }

    void OnMaterialToggleChanged(bool isOn, string material)
    {
        if (isOn)
        {
            if (!selectedMaterialFilters.Contains(material))
                selectedMaterialFilters.Add(material);
        }
        else
        {
            if (selectedMaterialFilters.Contains(material))
                selectedMaterialFilters.Remove(material);
        }
    }

    // --- LÓGICA VISUAL DE BOTONES (Sin cambios) ---
    
    void UpdateCategoryButtonsVisuals()
    {
        if (botonOrganicoImage != null) {
            botonOrganicoImage.sprite = spriteBotonNormal;
            botonOrganicoImage.color = colorInactivo;
        }
        if (botonTodosImage != null) {
            botonTodosImage.sprite = spriteBotonNormal;
            botonTodosImage.color = colorInactivo;
        }
        if (botonInorganicoImage != null) {
            botonInorganicoImage.sprite = spriteBotonNormal;
            botonInorganicoImage.color = colorInactivo;
        }

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

    // --- ¡NUEVAS FUNCIONES DE ANIMACIÓN! ---

    // Función pública que llaman los botones "Filtros" y "Cerrar"
    public void ShowFilterPanel(bool show)
    {
        // Detener cualquier animación anterior
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
        }
        
        // Empezar la nueva animación
        float duration = 0.2f; // Duración en segundos
        activeFadeCoroutine = StartCoroutine(FadePanel(show, duration));
    }

    // La Corutina que hace la animación
    private IEnumerator FadePanel(bool show, float duration)
    {
        float startTime = Time.time;
        float startAlpha = filtrosPanelCanvasGroup.alpha;
        float targetAlpha = show ? 1.0f : 0.0f;

        // Activar interacción al MOSTRAR
        if (show)
        {
            filtrosPanelCanvasGroup.interactable = true;
            filtrosPanelCanvasGroup.blocksRaycasts = true;
        }

        // Bucle de animación
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float newAlpha = Mathf.SmoothStep(startAlpha, targetAlpha, t);
            filtrosPanelCanvasGroup.alpha = newAlpha;
            yield return null; // Espera al siguiente frame
        }

        // Asegurarse de que el valor final sea exacto
        filtrosPanelCanvasGroup.alpha = targetAlpha;

        // Desactivar interacción al OCULTAR
        if (!show)
        {
            filtrosPanelCanvasGroup.interactable = false;
            filtrosPanelCanvasGroup.blocksRaycasts = false;
        }

        activeFadeCoroutine = null; // Limpiar la corutina
    }
}