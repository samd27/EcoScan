using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResiduoListGenerator : MonoBehaviour
{
    [Header("Prefabs y contenedor")]
    [Tooltip("Prefab que representa cada tarjeta de residuo.")]
    public GameObject residuoItemPrefab;

    [Tooltip("Contenedor (generalmente el Content del ScrollView) donde se instanciarán los ítems.")]
    public Transform contentParent;

    [Header("Datos")]
    [Tooltip("Ruta dentro de Resources donde vive el JSON. No incluyas la extensión .json.")]
    public string jsonFilePath = "DB/residuos";

    [Header("Referencias UI (opcionales)")]
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private Button organicButton;
    [SerializeField] private Button inorganicButton;
    [SerializeField] private Button sortButton;
    [SerializeField] private Button filterButton;

    private readonly List<GameObject> spawnedResiduoItems = new List<GameObject>();
    private readonly Dictionary<string, Toggle> subcategoryToggles = new Dictionary<string, Toggle>();
    private readonly HashSet<string> activeSubcategories = new HashSet<string>();

    private List<Residuo> allResiduos = new List<Residuo>();
    private List<string> orderedSubcategories = new List<string>();

    private bool organicFilterActive;
    private bool inorganicFilterActive;
    private bool sortAscending = true;
    private string currentSearchQuery = string.Empty;

    private GameObject filterPopupOverlay;
    private RectTransform filterToggleContent;
    private TextMeshProUGUI sortButtonLabel;
    private Image organicButtonImage;
    private Image inorganicButtonImage;
    private Color organicButtonDefaultColor;
    private Color inorganicButtonDefaultColor;

    private void Start()
    {
        if (residuoItemPrefab == null || contentParent == null)
        {
            Debug.LogError("ResiduoListGenerator necesita un prefab y un contentParent configurados en el inspector.");
            return;
        }

        if (!LoadResiduosFromJson())
        {
            return;
        }

        CacheButtonReferences();
        EnsureSearchField();
        BuildFilterPopup();
        ApplyFilters();
    }

    private bool LoadResiduosFromJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFilePath);

        if (jsonFile == null)
        {
            Debug.LogError("Error: No se encontró el JSON en Resources/" + jsonFilePath);
            return false;
        }

        string jsonText = "{\"residuos\":" + jsonFile.text + "}";
        ResiduoList residuoList = JsonUtility.FromJson<ResiduoList>(jsonText);

        if (residuoList == null || residuoList.residuos == null)
        {
            Debug.LogError("Error al deserializar el JSON. Revisa el formato.");
            return false;
        }

        allResiduos = residuoList.residuos.ToList();
        orderedSubcategories = allResiduos
            .Select(r => r.subcategoria)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList();

        activeSubcategories.Clear();
        foreach (string subcategory in orderedSubcategories)
        {
            activeSubcategories.Add(subcategory);
        }

        return true;
    }

    private void CacheButtonReferences()
    {
        if (organicButton == null)
        {
            organicButton = FindButtonByName("Boton_Organico");
        }

        if (inorganicButton == null)
        {
            inorganicButton = FindButtonByName("Boton_Inorganico");
        }

        if (sortButton == null)
        {
            sortButton = FindButtonByName("Boton_Sort");
        }

        if (filterButton == null)
        {
            filterButton = FindButtonByName("Boton_Filtros");
        }

        if (organicButton != null)
        {
            organicButton.onClick.AddListener(OnOrganicButtonClicked);
            organicButtonImage = organicButton.GetComponent<Image>();
            if (organicButtonImage != null)
            {
                organicButtonDefaultColor = organicButtonImage.color;
            }
        }

        if (inorganicButton != null)
        {
            inorganicButton.onClick.AddListener(OnInorganicButtonClicked);
            inorganicButtonImage = inorganicButton.GetComponent<Image>();
            if (inorganicButtonImage != null)
            {
                inorganicButtonDefaultColor = inorganicButtonImage.color;
            }
        }

        if (sortButton != null)
        {
            sortButton.onClick.AddListener(OnSortButtonClicked);
            sortButtonLabel = sortButton.GetComponentInChildren<TextMeshProUGUI>();
            UpdateSortButtonLabel();
        }

        if (filterButton != null)
        {
            filterButton.onClick.AddListener(ToggleFilterPopup);
        }
    }

    private Button FindButtonByName(string objectName)
    {
        GameObject buttonObject = GameObject.Find(objectName);
        if (buttonObject == null)
        {
            return null;
        }

        Button buttonComponent = buttonObject.GetComponent<Button>();
        if (buttonComponent == null)
        {
            Debug.LogWarning($"El objeto '{objectName}' no tiene un componente Button.");
        }

        return buttonComponent;
    }

    private void EnsureSearchField()
    {
        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(OnSearchValueChanged);
            return;
        }

        TMP_InputField existingInput = FindObjectOfType<TMP_InputField>();
        if (existingInput != null && existingInput != searchInput)
        {
            searchInput = existingInput;
            searchInput.onValueChanged.AddListener(OnSearchValueChanged);
            return;
        }

        GameObject searchBox = GameObject.Find("Search_Box");
        if (searchBox == null)
        {
            Debug.LogWarning("No se encontró el objeto Search_Box para crear un campo de búsqueda.");
            return;
        }

        searchInput = searchBox.GetComponent<TMP_InputField>();
        if (searchInput == null)
        {
            searchInput = searchBox.GetComponentInChildren<TMP_InputField>();
        }

        if (searchInput == null)
        {
            searchInput = CreateRuntimeSearchField(searchBox);
        }

        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(OnSearchValueChanged);
        }
    }

    private TMP_InputField CreateRuntimeSearchField(GameObject searchBox)
    {
        RectTransform searchBoxRect = searchBox.GetComponent<RectTransform>();
        if (searchBoxRect == null)
        {
            Debug.LogWarning("Search_Box no tiene RectTransform, no se puede generar un campo de búsqueda.");
            return null;
        }

        Button buttonComponent = searchBox.GetComponent<Button>();
        if (buttonComponent != null)
        {
            buttonComponent.onClick.RemoveAllListeners();
            buttonComponent.enabled = false;
        }

        RectTransform textArea = new GameObject("TextArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectMask2D)).GetComponent<RectTransform>();
        textArea.SetParent(searchBox.transform, false);
        textArea.anchorMin = Vector2.zero;
        textArea.anchorMax = Vector2.one;
        textArea.offsetMin = new Vector2(80f, 10f);
        textArea.offsetMax = new Vector2(-20f, -10f);

        TextMeshProUGUI placeholder = CreateTextMeshProChild(textArea, "Placeholder", "Buscar...", new Color(0.4f, 0.4f, 0.4f, 0.5f));
        TextMeshProUGUI textComponent = CreateTextMeshProChild(textArea, "Text", string.Empty, new Color(0.2f, 0.2f, 0.2f, 1f));

        TMP_InputField inputField = searchBox.AddComponent<TMP_InputField>();
        inputField.textViewport = textArea;
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholder;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.characterValidation = TMP_InputField.CharacterValidation.None;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.caretColor = new Color(0.2f, 0.6f, 0.3f, 1f);
        inputField.selectionColor = new Color(0.2f, 0.6f, 0.3f, 0.4f);
        inputField.pointSize = 36f;

        return inputField;
    }

    private TextMeshProUGUI CreateTextMeshProChild(RectTransform parent, string name, string initialText, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = 36f;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;

        return text;
    }

    private void BuildFilterPopup()
    {
        if (filterPopupOverlay != null || orderedSubcategories.Count == 0)
        {
            return;
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            parentCanvas = FindObjectOfType<Canvas>();
        }

        if (parentCanvas == null)
        {
            Debug.LogWarning("No se encontró Canvas para construir la ventana de filtros.");
            return;
        }

        filterPopupOverlay = new GameObject("FilterPopupOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        filterPopupOverlay.transform.SetParent(parentCanvas.transform, false);

        RectTransform overlayRect = filterPopupOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = filterPopupOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject panel = new GameObject("FilterPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(filterPopupOverlay.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.2f);
        panelRect.anchorMax = new Vector2(0.9f, 0.8f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = Color.white;
        panelImage.raycastTarget = true;

        TextMeshProUGUI title = CreateTextMeshProChild(panelRect, "Title", "Filtrar por subcategoría", new Color(0.15f, 0.15f, 0.15f, 1f));
        title.fontSize = 42f;
        title.fontStyle = FontStyles.Bold;
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(40f, -100f);
        titleRect.offsetMax = new Vector2(-120f, -20f);

        GameObject closeButtonGO = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeButtonGO.transform.SetParent(panel.transform, false);
        RectTransform closeRect = closeButtonGO.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(70f, 70f);
        closeRect.anchoredPosition = new Vector2(-20f, -20f);

        Image closeImage = closeButtonGO.GetComponent<Image>();
        closeImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        closeImage.type = Image.Type.Sliced;
        closeImage.color = new Color(0.93f, 0.93f, 0.93f, 1f);

        Button closeButton = closeButtonGO.GetComponent<Button>();
        closeButton.onClick.AddListener(() => SetFilterPopupVisible(false));

        TextMeshProUGUI closeLabel = CreateTextMeshProChild(closeRect, "Label", "✕", new Color(0.2f, 0.2f, 0.2f, 1f));
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.fontSize = 48f;

        GameObject scrollViewGO = new GameObject("ScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
        scrollViewGO.transform.SetParent(panel.transform, false);
        RectTransform scrollRectTransform = scrollViewGO.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(30f, 40f);
        scrollRectTransform.offsetMax = new Vector2(-30f, -140f);

        Image scrollViewImage = scrollViewGO.GetComponent<Image>();
        scrollViewImage.color = new Color(0.96f, 0.96f, 0.96f, 1f);

        ScrollRect scrollRect = scrollViewGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(scrollViewGO.transform, false);
        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layoutGroup = contentGO.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.spacing = 24f;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;

        ContentSizeFitter fitter = contentGO.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;

        filterToggleContent = contentRect;

        foreach (string subcategory in orderedSubcategories)
        {
            CreateSubcategoryToggle(subcategory);
        }

        filterPopupOverlay.SetActive(false);
    }

    private void CreateSubcategoryToggle(string subcategory)
    {
        if (filterToggleContent == null || subcategoryToggles.ContainsKey(subcategory))
        {
            return;
        }

        GameObject row = new GameObject($"ToggleRow_{subcategory}", typeof(RectTransform));
        row.transform.SetParent(filterToggleContent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);

        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 24f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childForceExpandWidth = false;

        LayoutElement rowLayoutElement = row.AddComponent<LayoutElement>();
        rowLayoutElement.minHeight = 70f;
        rowLayoutElement.preferredHeight = 80f;

        GameObject toggleGO = new GameObject("Toggle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
        toggleGO.transform.SetParent(row.transform, false);
        RectTransform toggleRect = toggleGO.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(48f, 48f);

        Image toggleBackground = toggleGO.GetComponent<Image>();
        toggleBackground.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        toggleBackground.type = Image.Type.Sliced;
        toggleBackground.color = new Color(0.88f, 0.88f, 0.88f, 1f);

        GameObject checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        checkmarkGO.transform.SetParent(toggleGO.transform, false);
        RectTransform checkmarkRect = checkmarkGO.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.2f, 0.2f);
        checkmarkRect.anchorMax = new Vector2(0.8f, 0.8f);
        checkmarkRect.offsetMin = Vector2.zero;
        checkmarkRect.offsetMax = Vector2.zero;

        Image checkmarkImage = checkmarkGO.GetComponent<Image>();
        checkmarkImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Checkmark.psd");
        checkmarkImage.color = new Color(0.2f, 0.6f, 0.3f, 1f);

        Toggle toggle = toggleGO.GetComponent<Toggle>();
        toggle.targetGraphic = toggleBackground;
        toggle.graphic = checkmarkImage;
        toggle.isOn = true;
        toggle.onValueChanged.AddListener(isOn => OnSubcategoryToggleChanged(subcategory, isOn));

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelGO.transform.SetParent(row.transform, false);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = subcategory;
        label.fontSize = 34f;
        label.color = new Color(0.16f, 0.16f, 0.16f, 1f);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableWordWrapping = true;

        LayoutElement labelLayoutElement = labelGO.AddComponent<LayoutElement>();
        labelLayoutElement.flexibleWidth = 1f;

        subcategoryToggles[subcategory] = toggle;
    }

    private void OnOrganicButtonClicked()
    {
        organicFilterActive = !organicFilterActive;
        UpdateCategoryButtonVisuals();
        ApplyFilters();
    }

    private void OnInorganicButtonClicked()
    {
        inorganicFilterActive = !inorganicFilterActive;
        UpdateCategoryButtonVisuals();
        ApplyFilters();
    }

    private void UpdateCategoryButtonVisuals()
    {
        if (organicButtonImage != null)
        {
            organicButtonImage.color = organicFilterActive
                ? new Color(0.77f, 0.93f, 0.78f, 1f)
                : organicButtonDefaultColor;
        }

        if (inorganicButtonImage != null)
        {
            inorganicButtonImage.color = inorganicFilterActive
                ? new Color(0.78f, 0.87f, 0.96f, 1f)
                : inorganicButtonDefaultColor;
        }
    }

    private void OnSortButtonClicked()
    {
        sortAscending = !sortAscending;
        UpdateSortButtonLabel();
        ApplyFilters();
    }

    private void UpdateSortButtonLabel()
    {
        if (sortButtonLabel != null)
        {
            sortButtonLabel.text = sortAscending ? "A-Z" : "Z-A";
        }
    }

    private void ToggleFilterPopup()
    {
        if (filterPopupOverlay == null)
        {
            return;
        }

        bool shouldShow = !filterPopupOverlay.activeSelf;
        SetFilterPopupVisible(shouldShow);
    }

    private void SetFilterPopupVisible(bool visible)
    {
        if (filterPopupOverlay == null)
        {
            return;
        }

        filterPopupOverlay.SetActive(visible);

        if (visible)
        {
            foreach (KeyValuePair<string, Toggle> pair in subcategoryToggles)
            {
                if (pair.Value != null)
                {
                    bool isActive = activeSubcategories.Contains(pair.Key);
                    pair.Value.SetIsOnWithoutNotify(isActive);
                }
            }
        }
    }

    private void OnSubcategoryToggleChanged(string subcategory, bool isOn)
    {
        if (isOn)
        {
            activeSubcategories.Add(subcategory);
        }
        else
        {
            activeSubcategories.Remove(subcategory);
        }

        ApplyFilters();
    }

    private void OnSearchValueChanged(string newValue)
    {
        currentSearchQuery = newValue ?? string.Empty;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<Residuo> query = allResiduos;

        if (!string.IsNullOrWhiteSpace(currentSearchQuery))
        {
            string lowercaseSearch = currentSearchQuery.ToLowerInvariant();
            query = query.Where(residuo =>
                (!string.IsNullOrEmpty(residuo.nombre) && residuo.nombre.ToLowerInvariant().Contains(lowercaseSearch)) ||
                (!string.IsNullOrEmpty(residuo.keywords) && residuo.keywords.ToLowerInvariant().Contains(lowercaseSearch)));
        }

        List<string> activeCategories = new List<string>();
        if (organicFilterActive)
        {
            activeCategories.Add("ORGANICO");
        }

        if (inorganicFilterActive)
        {
            activeCategories.Add("INORGANICO");
        }

        if (activeCategories.Count > 0)
        {
            query = query.Where(residuo => activeCategories.Contains(residuo.categoria?.ToUpperInvariant()));
        }

        if (activeSubcategories.Count > 0 && activeSubcategories.Count != orderedSubcategories.Count)
        {
            query = query.Where(residuo => !string.IsNullOrEmpty(residuo.subcategoria) && activeSubcategories.Contains(residuo.subcategoria));
        }

        query = sortAscending
            ? query.OrderBy(residuo => residuo.nombre)
            : query.OrderByDescending(residuo => residuo.nombre);

        RefreshResiduoList(query);
    }

    private void RefreshResiduoList(IEnumerable<Residuo> residuos)
    {
        foreach (GameObject item in spawnedResiduoItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }

        spawnedResiduoItems.Clear();

        foreach (Residuo residuo in residuos)
        {
            GameObject newItem = Instantiate(residuoItemPrefab, contentParent);
            ResiduoItemDisplay display = newItem.GetComponent<ResiduoItemDisplay>();
            if (display != null)
            {
                display.Setup(residuo);
            }

            spawnedResiduoItems.Add(newItem);
        }
    }
}
