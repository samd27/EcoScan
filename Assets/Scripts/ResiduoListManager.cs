using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Para Corutinas
using System.Collections.Generic;
using System.Linq;
using TMPro;
using PolyAndCode.UI; // Para el Recyclable Scroll Rect

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
    public CanvasGroup filtrosPanelCanvasGroup; // Panel de filtros

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

    // --- ¡NUEVO! CONEXIONES PANEL DE DETALLES ---
    [Header("UI Panel de Detalles")]
    public CanvasGroup detallesPanelCanvasGroup;
    public Image detallesImage;
    public TextMeshProUGUI detallesNombreText;
    public TextMeshProUGUI detallesCategoriaText;
    public TextMeshProUGUI detallesMaterialText;
    public TextMeshProUGUI detallesDescripcionText;

    // --- VARIABLES DE ESTADO PRIVADAS ---
    private List<Residuo> allResiduos = new List<Residuo>();
    private List<Residuo> _filteredResiduoList = new List<Residuo>();
    private string currentSearchText = "";
    private string currentCategory = "ALL";
    private bool sortAZ = false;
    private List<string> selectedMaterialFilters = new List<string>();
    private List<Toggle> instantiatedToggles = new List<Toggle>();
    
    // Corutinas de animación
    private Coroutine activeFadeCoroutine;
    private Coroutine activeDetallesFadeCoroutine;

    
    // --- FUNCIONES DE UNITY ---
    void Awake()
    {
        // Asignar el "proveedor de datos" al asset
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
        // Ocultar panel de filtros al inicio
        if (filtrosPanelCanvasGroup != null)
        {
            filtrosPanelCanvasGroup.alpha = 0f;
            filtrosPanelCanvasGroup.interactable = false;
            filtrosPanelCanvasGroup.blocksRaycasts = false;
        }

        // Ocultar panel de detalles al inicio
        if (detallesPanelCanvasGroup != null)
        {
            detallesPanelCanvasGroup.alpha = 0f;
            detallesPanelCanvasGroup.interactable = false;
            detallesPanelCanvasGroup.blocksRaycasts = false;
        }

        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(SetSearchText);
        }
        
        currentCategory = "ALL"; 
        UpdateCategoryButtonsVisuals(); 
        ApplyFiltersAndSort(); // Aplicar filtro inicial
    }

    // --- FUNCIONES OBLIGATORIAS DEL ASSET ---

    public int GetItemCount()
    {
        return _filteredResiduoList.Count;
    }

    // ¡FUNCIÓN CRÍTICA MODIFICADA!
    // Aquí es donde se asigna el clic dinámicamente
    public void SetCell(ICell cell, int index)
    {
        // 1. Convertir el 'ICell' genérico a tu script específico
        var item = cell as ResiduoItemDisplay;
        if (item == null || index < 0 || index >= _filteredResiduoList.Count) return;

        // 2. Obtener los datos para este índice
        Residuo dataParaEstaCelda = _filteredResiduoList[index];

        // 3. Llamar a tu función 'Setup' de siempre
        item.Setup(dataParaEstaCelda);

        // 4. --- ¡LA NUEVA LÓGICA DE CLIC! ---
        
        // Obtenemos el componente Button del prefab
        Button itemButton = item.GetComponent<Button>();
        
        // Limpiamos CUALQUIER listener anterior (¡CRÍTICO para el reciclaje!)
        itemButton.onClick.RemoveAllListeners();

        // Añadimos el NUEVO listener que llama a nuestra función
        // usando los datos de ESTA celda.
        itemButton.onClick.AddListener(() => {
            ShowDetallesPanel(dataParaEstaCelda); 
        });
    }
    
    // --- LÓGICA DE FILTROS Y DATOS ---

    void LoadResiduosFromJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFilePath);
        if (jsonFile == null) { Debug.LogError("Error: No se encontró el JSON..."); return; }
        string jsonText = "{\"residuos\":" + jsonFile.text + "}";
        ResiduoList residuoList = JsonUtility.FromJson<ResiduoList>(jsonText);
        if (residuoList == null || residuoList.residuos == null) { Debug.LogError("Error al deserializar el JSON."); return; }
        allResiduos = new List<Residuo>(residuoList.residuos);
    }

    void ApplyFiltersAndSort()
    {
        IEnumerable<Residuo> filteredListQuery = allResiduos;

        // Aplicar filtros (Categoría, Búsqueda, Material)
        if (currentCategory != "ALL")
            filteredListQuery = filteredListQuery.Where(r => r.categoria == currentCategory);
        
        if (!string.IsNullOrEmpty(currentSearchText))
            filteredListQuery = filteredListQuery.Where(r => 
                r.nombre.ToLower().Contains(currentSearchText) || 
                r.keywords.ToLower().Contains(currentSearchText));
        
        if (selectedMaterialFilters.Count > 0)
            filteredListQuery = filteredListQuery.Where(r => 
                selectedMaterialFilters.Contains(r.material));
        
        if (sortAZ)
            filteredListQuery = filteredListQuery.OrderBy(r => r.nombre);
        
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
        if (searchIcon != null) searchIcon.SetActive(string.IsNullOrEmpty(newText));
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
        ShowFilterPanel(false); // Llamará a la función de animación
    }

    public void ClearMaterialFilters()
    {
        selectedMaterialFilters.Clear();
        foreach (Toggle t in instantiatedToggles) t.isOn = false;
        ApplyFiltersAndSort();
    }

    // --- LÓGICA PANEL DE FILTROS (Poblado y Animación) ---

    void PopulateFilterPanel()
    {
        var uniqueMaterials = allResiduos
                                .Where(r => !string.IsNullOrEmpty(r.material))
                                .Select(r => r.material).Distinct().OrderBy(m => m);
        instantiatedToggles.Clear();
        foreach (string material in uniqueMaterials)
        {
            GameObject newToggleObj = Instantiate(togglePrefab, filterContentParent);
            Toggle newToggle = newToggleObj.GetComponent<Toggle>();
            TextMeshProUGUI label = newToggleObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = material;
            string mat = material; 
            newToggle.onValueChanged.AddListener((isOn) => OnMaterialToggleChanged(isOn, mat));
            instantiatedToggles.Add(newToggle);
        }
    }

    void OnMaterialToggleChanged(bool isOn, string material)
    {
        if (isOn) { if (!selectedMaterialFilters.Contains(material)) selectedMaterialFilters.Add(material); }
        else { if (selectedMaterialFilters.Contains(material)) selectedMaterialFilters.Remove(material); }
    }

    public void ShowFilterPanel(bool show) // Llamada por botones
    {
        if (activeFadeCoroutine != null) StopCoroutine(activeFadeCoroutine);
        activeFadeCoroutine = StartCoroutine(FadePanel(show, 0.2f));
    }

    private IEnumerator FadePanel(bool show, float duration) // Corutina
    {
        float startTime = Time.time;
        float startAlpha = filtrosPanelCanvasGroup.alpha;
        float targetAlpha = show ? 1.0f : 0.0f;

        if (show) {
            filtrosPanelCanvasGroup.interactable = true;
            filtrosPanelCanvasGroup.blocksRaycasts = true;
        }

        while (Time.time < startTime + duration) {
            float t = (Time.time - startTime) / duration;
            filtrosPanelCanvasGroup.alpha = Mathf.SmoothStep(startAlpha, targetAlpha, t);
            yield return null;
        }

        filtrosPanelCanvasGroup.alpha = targetAlpha;

        if (!show) {
            filtrosPanelCanvasGroup.interactable = false;
            filtrosPanelCanvasGroup.blocksRaycasts = false;
        }
        activeFadeCoroutine = null;
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
        switch (currentCategory) {
            case "ORGANICO":
                if (botonOrganicoImage != null) {
                    botonOrganicoImage.sprite = spriteBotonSolido;
                    botonOrganicoImage.color = colorActivo;
                } break;
            case "INORGANICO":
                if (botonInorganicoImage != null) {
                    botonInorganicoImage.sprite = spriteBotonSolido;
                    botonInorganicoImage.color = colorActivo;
                } break;
            case "ALL": default:
                if (botonTodosImage != null) {
                    botonTodosImage.sprite = spriteBotonSolido;
                    botonTodosImage.color = colorActivo;
                } break;
        }
    }

    // --- ¡NUEVAS FUNCIONES PARA EL PANEL DE DETALLES! ---

    // 1. Esta función PÚBLICA es llamada por el botón "Cerrar" del panel de detalles
    public void HideDetallesPanel()
    {
        if (activeDetallesFadeCoroutine != null) StopCoroutine(activeDetallesFadeCoroutine);
        activeDetallesFadeCoroutine = StartCoroutine(FadeDetallesPanel(false, 0.2f));
    }

    // 2. Esta función PRIVADA la llama el listener del botón en SetCell
    private void ShowDetallesPanel(Residuo residuoData)
    {
        // Poblar todos los campos de texto
        detallesNombreText.text = residuoData.nombre;
        detallesCategoriaText.text = residuoData.categoria;
        detallesMaterialText.text = residuoData.material;
        detallesDescripcionText.text = residuoData.descripcion;

        // Poblar la imagen (re-usando la misma lógica)
        string imageBasePath = "DB/img/";
        string imageName = residuoData.id.ToString();
        Sprite loadedSprite = Resources.Load<Sprite>(imageBasePath + imageName);

        if (loadedSprite == null)
        {
            loadedSprite = Resources.Load<Sprite>(imageBasePath + "default");
        }
        detallesImage.sprite = loadedSprite;

        // Iniciar la animación de "fade in"
        if (activeDetallesFadeCoroutine != null) StopCoroutine(activeDetallesFadeCoroutine);
        activeDetallesFadeCoroutine = StartCoroutine(FadeDetallesPanel(true, 0.2f));
    }

    // 3. La Corutina de animación para el panel de detalles
    private IEnumerator FadeDetallesPanel(bool show, float duration)
    {
        float startTime = Time.time;
        float startAlpha = detallesPanelCanvasGroup.alpha;
        float targetAlpha = show ? 1.0f : 0.0f;

        if (show)
        {
            detallesPanelCanvasGroup.interactable = true;
            detallesPanelCanvasGroup.blocksRaycasts = true;
        }

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            detallesPanelCanvasGroup.alpha = Mathf.SmoothStep(startAlpha, targetAlpha, t);
            yield return null;
        }

        detallesPanelCanvasGroup.alpha = targetAlpha;

        if (!show)
        {
            detallesPanelCanvasGroup.interactable = false;
            detallesPanelCanvasGroup.blocksRaycasts = false;
        }
        activeDetallesFadeCoroutine = null;
    }
}