using UnityEngine;
using UnityEngine.UI; // Para Image y Toggle
using System.Collections.Generic; // Para List<>
using System.Linq; // Para filtrar y ordenar (Where, Select, Distinct, etc.)
using TMPro; // Para TextMeshProUGUI y TMP_InputField

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
    public GameObject togglePrefab; // El "Filtro_Toggle_Prefab"
    public Transform filterContentParent; // El "Content" del ScrollView de filtros

    // --- VARIABLES DE ESTADO PRIVADAS ---
    private List<Residuo> allResiduos = new List<Residuo>();
    
    // --- Variables para guardar el estado actual del filtro ---
    private string currentSearchText = "";
    private string currentCategory = "ALL";
    private bool sortAZ = false;
    private List<string> selectedMaterialFilters = new List<string>();
    private List<Toggle> instantiatedToggles = new List<Toggle>(); // Para poder limpiarlos

    // --- FUNCIONES DE UNITY ---

    void Start()
    {
        // 1. Ocultar el panel de filtros al inicio
        if (filtrosPanel != null)
            filtrosPanel.SetActive(false);

        // 2. Conectar el InputField (si se asignó)
        if (searchInputField != null)
        {
            searchInputField.onValueChanged.AddListener(SetSearchText);
        }
        
        // 3. Establecer el estado inicial por defecto
        currentCategory = "ALL"; 
        
        // 4. Cargar datos del JSON a la lista maestra
        LoadResiduosFromJson();
        
        // 5. Poblar el panel de filtros con los materiales únicos
        PopulateFilterPanel();
        
        // 6. Actualizar el visual de los botones al estado "ALL" por defecto
        UpdateCategoryButtonsVisuals(); 

        // 7. Mostrar todos los items por primera vez
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

    // Llamada por el InputField
    public void SetSearchText(string newText)
    {
        currentSearchText = newText.ToLower();
        if (searchIcon != null)
        {
            searchIcon.SetActive(string.IsNullOrEmpty(newText));
        }
        ApplyFiltersAndSort();
    }

    // Llamada por los botones Orgánico / Todos / Inorgánico
    public void SetCategoryFilter(string category)
    {
        currentCategory = category;
        UpdateCategoryButtonsVisuals();
        ApplyFiltersAndSort();
    }

    // Llamada por el botón A-Z
    public void ToggleSortAZ()
    {
        sortAZ = !sortAZ;
        ApplyFiltersAndSort();
    }

    // Llamada por el botón "Filtros" y "Boton_Cerrar"
    public void ShowFilterPanel(bool show)
    {
        if (filtrosPanel != null)
            filtrosPanel.SetActive(show);
    }

    // Llamada por el botón "Aplicar" del panel de filtros
    public void ApplyAndCloseFilters()
    {
        ApplyFiltersAndSort(); // Aplica todos los filtros
        ShowFilterPanel(false); // Cierra el panel
    }

    // Llamada por el botón "Limpiar" del panel de filtros
    public void ClearMaterialFilters()
    {
        // 1. Limpiar la lista de datos
        selectedMaterialFilters.Clear();

        // 2. Desmarcar visualmente todas las casillas
        foreach (Toggle t in instantiatedToggles)
        {
            // Ponemos .isOn = false, lo que también disparará
            // el listener OnMaterialToggleChanged y limpiará la lista
            t.isOn = false;
        }

        // 3. Aplicar los filtros (que ahora están limpios)
        ApplyFiltersAndSort();
    }


    // --- FUNCIONES INTERNAS DE ACTUALIZACIÓN ---

    void ApplyFiltersAndSort()
    {
        // 1. Empezar con la lista completa
        IEnumerable<Residuo> filteredList = allResiduos;

        // 2. Aplicar filtro de Categoría
        if (currentCategory != "ALL")
        {
            filteredList = filteredList.Where(r => r.categoria == currentCategory);
        }

        // 3. Aplicar filtro de Búsqueda (en nombre Y keywords)
        if (!string.IsNullOrEmpty(currentSearchText))
        {
            filteredList = filteredList.Where(r => 
                r.nombre.ToLower().Contains(currentSearchText) || 
                r.keywords.ToLower().Contains(currentSearchText)
            );
        }

        // 4. Aplicar filtro de Material (del panel de filtros)
        if (selectedMaterialFilters.Count > 0)
        {
            filteredList = filteredList.Where(r => 
                selectedMaterialFilters.Contains(r.material)
            );
        }

        // 5. Aplicar Ordenamiento
        if (sortAZ)
        {
            filteredList = filteredList.OrderBy(r => r.nombre);
        }
        
        // 6. ¡Finalmente, mostrar el resultado!
        UpdateDisplay(filteredList.ToList());
    }

    void UpdateDisplay(List<Residuo> listToDisplay)
    {
        // Borrar todos los items viejos
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Crear los nuevos items filtrados/ordenados
        foreach (Residuo residuo in listToDisplay)
        {
            GameObject newItem = Instantiate(residuoItemPrefab, contentParent);
            newItem.GetComponent<ResiduoItemDisplay>().Setup(residuo);
        }
    }

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

    // --- FUNCIONES DEL PANEL DE FILTROS ---

    void PopulateFilterPanel()
    {
        // Usar LINQ para encontrar todos los materiales ÚNICOS
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
                label.text = material; // Ej: "Plástico", "Metal", "Vidrio"
            }

            // Añadir un "listener" que nos avise cuando el usuario marque/desmarque
            string mat = material; 
            newToggle.onValueChanged.AddListener((isOn) => {
                OnMaterialToggleChanged(isOn, mat);
            });

            instantiatedToggles.Add(newToggle);
        }
    }

    // Esta es la función que se llama cuando un Toggle cambia
    void OnMaterialToggleChanged(bool isOn, string material)
    {
        if (isOn)
        {
            // Si se marcó, añadir a la lista
            if (!selectedMaterialFilters.Contains(material))
                selectedMaterialFilters.Add(material);
        }
        else
        {
            // Si se desmarcó, quitar de la lista
            if (selectedMaterialFilters.Contains(material))
                selectedMaterialFilters.Remove(material);
        }
    }
}