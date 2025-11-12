using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResiduoListGenerator : MonoBehaviour
{
    [Header("Prefabs y contenedores")]
    public GameObject residuoItemPrefab;
    public Transform contentParent;

    [Header("Datos")]
    public string jsonFilePath = "DB/residuos";

    private readonly List<Residuo> allResiduos = new List<Residuo>();
    private readonly List<Residuo> filteredResiduos = new List<Residuo>();
    private readonly Dictionary<string, Toggle> subcategoryToggles = new Dictionary<string, Toggle>(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> activeSubcategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private Button searchButton;
    private GameObject searchPanel;
    private TMP_InputField searchInputField;
    private Button searchCloseButton;

    private Button organicButton;
    private Button inorganicButton;
    private Image organicButtonImage;
    private Image inorganicButtonImage;
    private TextMeshProUGUI organicButtonLabel;
    private TextMeshProUGUI inorganicButtonLabel;
    private Color organicButtonActiveColor = Color.white;
    private Color inorganicButtonActiveColor = Color.white;
    private Color organicTextActiveColor = new Color(0.149f, 0.384f, 0.169f);
    private Color inorganicTextActiveColor = new Color(0.149f, 0.384f, 0.169f);

    private Button sortButton;
    private TextMeshProUGUI sortButtonLabel;

    private Button filtersButton;
    private GameObject filtersModal;
    private Transform filtersListContainer;
    private Button filtersCloseButton;

    private TMP_FontAsset mainFontAsset;

    private readonly Color categoryDisabledColor = new Color(0.78f, 0.78f, 0.78f, 1f);
    private readonly Color categoryTextDisabledColor = new Color(0.35f, 0.35f, 0.35f, 0.75f);

    private bool showOrganico = true;
    private bool showInorganico = true;
    private bool sortAscending = true;
    private string searchTerm = string.Empty;
    private bool isInitialized;

    void Start()
    {
        LoadResiduosFromJson();
        SetupUI();
        PopulateSubcategoryFilters();
        isInitialized = true;
        ApplyFilters();
    }

    private void LoadResiduosFromJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFilePath);

        if (jsonFile == null)
        {
            Debug.LogError("Error: No se encontró el JSON en Resources/" + jsonFilePath);
            return;
        }

        string jsonText = "{\"residuos\":" + jsonFile.text + "}";
        ResiduoList residuoList = JsonUtility.FromJson<ResiduoList>(jsonText);

        if (residuoList == null || residuoList.residuos == null)
        {
            Debug.LogError("Error al deserializar el JSON. Revisa el formato.");
            return;
        }

        allResiduos.Clear();
        allResiduos.AddRange(residuoList.residuos);
    }

    private void SetupUI()
    {
        Canvas rootCanvas = contentParent != null ? contentParent.GetComponentInParent<Canvas>() : FindObjectOfType<Canvas>();
        if (rootCanvas == null)
        {
            Debug.LogError("No se encontró un Canvas en la escena para construir la UI.");
            return;
        }

        searchButton = GameObject.Find("Search_Box")?.GetComponent<Button>();
        sortButton = GameObject.Find("Boton_Sort")?.GetComponent<Button>();
        organicButton = GameObject.Find("Boton_Organico")?.GetComponent<Button>();
        inorganicButton = GameObject.Find("Boton_Inorganico")?.GetComponent<Button>();
        filtersButton = GameObject.Find("Boton_Filtros")?.GetComponent<Button>();

        sortButtonLabel = sortButton != null ? sortButton.GetComponentInChildren<TextMeshProUGUI>() : null;
        organicButtonLabel = organicButton != null ? organicButton.GetComponentInChildren<TextMeshProUGUI>() : null;
        inorganicButtonLabel = inorganicButton != null ? inorganicButton.GetComponentInChildren<TextMeshProUGUI>() : null;

        mainFontAsset = (sortButtonLabel ?? organicButtonLabel ?? inorganicButtonLabel)?.font ?? TMP_Settings.defaultFontAsset;

        if (searchButton != null)
        {
            CreateSearchPanel(rootCanvas.transform);
            searchButton.onClick.AddListener(() => ToggleSearchPanel(true));
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'Search_Box' en la escena.");
        }

        if (sortButton != null)
        {
            sortButton.onClick.AddListener(ToggleSortOrder);
            UpdateSortLabel();
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'Boton_Sort' en la escena.");
        }

        if (organicButton != null)
        {
            organicButtonImage = organicButton.GetComponent<Image>();
            if (organicButtonImage != null)
            {
                organicButtonActiveColor = organicButtonImage.color;
            }
            if (organicButtonLabel != null)
            {
                organicTextActiveColor = organicButtonLabel.color;
            }
            organicButton.onClick.AddListener(() => ToggleCategoryFilter("ORGANICO"));
            UpdateCategoryVisual(organicButtonImage, organicButtonLabel, true, organicButtonActiveColor, organicTextActiveColor);
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'Boton_Organico' en la escena.");
        }

        if (inorganicButton != null)
        {
            inorganicButtonImage = inorganicButton.GetComponent<Image>();
            if (inorganicButtonImage != null)
            {
                inorganicButtonActiveColor = inorganicButtonImage.color;
            }
            if (inorganicButtonLabel != null)
            {
                inorganicTextActiveColor = inorganicButtonLabel.color;
            }
            inorganicButton.onClick.AddListener(() => ToggleCategoryFilter("INORGANICO"));
            UpdateCategoryVisual(inorganicButtonImage, inorganicButtonLabel, true, inorganicButtonActiveColor, inorganicTextActiveColor);
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'Boton_Inorganico' en la escena.");
        }

        if (filtersButton != null)
        {
            CreateFiltersModal(rootCanvas.transform);
            filtersButton.onClick.AddListener(() => ToggleFiltersModal(filtersModal != null && !filtersModal.activeSelf));
        }
        else
        {
            Debug.LogWarning("No se encontró el objeto 'Boton_Filtros' en la escena.");
        }
    }

    private void PopulateSubcategoryFilters()
    {
        if (allResiduos.Count == 0)
        {
            activeSubcategories.Clear();
            return;
        }

        List<string> subcategories = allResiduos
            .Select(r => string.IsNullOrWhiteSpace(r.subcategoria) ? string.Empty : r.subcategoria)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();

        activeSubcategories = new HashSet<string>(subcategories, StringComparer.OrdinalIgnoreCase);

        if (filtersListContainer == null)
        {
            return;
        }

        foreach (Transform child in filtersListContainer)
        {
            Destroy(child.gameObject);
        }

        subcategoryToggles.Clear();

        foreach (string subcategory in subcategories)
        {
            CreateSubcategoryToggle(subcategory);
        }
    }

    private void CreateSubcategoryToggle(string subcategory)
    {
        string toggleName = string.IsNullOrWhiteSpace(subcategory) ? "Toggle_SinSubcategoria" : "Toggle_" + SanitizeName(subcategory);
        GameObject toggleGO = new GameObject(toggleName, typeof(RectTransform), typeof(Image), typeof(Toggle));
        toggleGO.transform.SetParent(filtersListContainer, false);

        RectTransform rect = toggleGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(0f, 100f);

        Sprite defaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

        Image background = toggleGO.GetComponent<Image>();
        background.sprite = defaultSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color(0.92f, 0.96f, 0.93f, 1f);

        GameObject checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmarkGO.transform.SetParent(toggleGO.transform, false);

        RectTransform checkmarkRect = checkmarkGO.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
        checkmarkRect.pivot = new Vector2(0f, 0.5f);
        checkmarkRect.anchoredPosition = new Vector2(30f, 0f);
        checkmarkRect.sizeDelta = new Vector2(40f, 40f);

        Image checkmarkImage = checkmarkGO.GetComponent<Image>();
        checkmarkImage.sprite = defaultSprite;
        checkmarkImage.type = Image.Type.Sliced;
        checkmarkImage.color = new Color(0.298f, 0.686f, 0.314f, 1f);

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(toggleGO.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(100f, 0f);
        labelRect.offsetMax = new Vector2(-20f, 0f);

        TextMeshProUGUI labelText = labelGO.GetComponent<TextMeshProUGUI>();
        labelText.text = string.IsNullOrWhiteSpace(subcategory) ? "Sin subcategoría" : subcategory;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.fontSize = 48f;
        labelText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        if (mainFontAsset != null)
        {
            labelText.font = mainFontAsset;
        }

        Toggle toggle = toggleGO.GetComponent<Toggle>();
        toggle.isOn = true;
        toggle.targetGraphic = background;
        toggle.graphic = checkmarkImage;
        toggle.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = toggle.colors;
        colors.colorMultiplier = 1f;
        colors.normalColor = background.color;
        colors.highlightedColor = new Color(0.88f, 0.94f, 0.90f, 1f);
        colors.pressedColor = new Color(0.82f, 0.90f, 0.84f, 1f);
        colors.selectedColor = colors.normalColor;
        colors.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.5f);
        toggle.colors = colors;

        string key = subcategory;
        toggle.onValueChanged.AddListener(isOn => OnSubcategoryToggleChanged(key, isOn));

        subcategoryToggles[key] = toggle;
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

    private void ToggleCategoryFilter(string category)
    {
        if (category == "ORGANICO")
        {
            showOrganico = !showOrganico;
            UpdateCategoryVisual(organicButtonImage, organicButtonLabel, showOrganico, organicButtonActiveColor, organicTextActiveColor);
        }
        else if (category == "INORGANICO")
        {
            showInorganico = !showInorganico;
            UpdateCategoryVisual(inorganicButtonImage, inorganicButtonLabel, showInorganico, inorganicButtonActiveColor, inorganicTextActiveColor);
        }

        ApplyFilters();
    }

    private void UpdateCategoryVisual(Image image, TextMeshProUGUI label, bool isActive, Color activeColor, Color textActiveColor)
    {
        if (image != null)
        {
            image.color = isActive ? activeColor : categoryDisabledColor;
        }

        if (label != null)
        {
            label.color = isActive ? textActiveColor : categoryTextDisabledColor;
        }
    }

    private void ToggleSortOrder()
    {
        sortAscending = !sortAscending;
        UpdateSortLabel();
        ApplyFilters();
    }

    private void UpdateSortLabel()
    {
        if (sortButtonLabel != null)
        {
            sortButtonLabel.text = sortAscending ? "A-Z" : "Z-A";
        }
    }

    private void ApplyFilters()
    {
        if (!isInitialized || residuoItemPrefab == null || contentParent == null)
        {
            return;
        }

        IEnumerable<Residuo> query = allResiduos;

        if (!showOrganico || !showInorganico)
        {
            query = query.Where(r =>
                (showOrganico && string.Equals(r.categoria, "ORGANICO", StringComparison.OrdinalIgnoreCase)) ||
                (showInorganico && string.Equals(r.categoria, "INORGANICO", StringComparison.OrdinalIgnoreCase))
            );
        }

        if (activeSubcategories != null && activeSubcategories.Count > 0)
        {
            query = query.Where(r =>
            {
                string sub = string.IsNullOrWhiteSpace(r.subcategoria) ? string.Empty : r.subcategoria;
                return activeSubcategories.Contains(sub);
            });
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string lowered = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(r =>
                (!string.IsNullOrEmpty(r.nombre) && r.nombre.ToLowerInvariant().Contains(lowered)) ||
                (!string.IsNullOrEmpty(r.keywords) && r.keywords.ToLowerInvariant().Contains(lowered))
            );
        }

        query = sortAscending
            ? query.OrderBy(r => r.nombre ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            : query.OrderByDescending(r => r.nombre ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        filteredResiduos.Clear();
        filteredResiduos.AddRange(query);

        RebuildList();
    }

    private void RebuildList()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (Residuo residuo in filteredResiduos)
        {
            GameObject newItem = Instantiate(residuoItemPrefab, contentParent);
            ResiduoItemDisplay display = newItem.GetComponent<ResiduoItemDisplay>();
            if (display != null)
            {
                display.Setup(residuo);
            }
        }
    }

    private void CreateSearchPanel(Transform canvasTransform)
    {
        if (canvasTransform == null)
        {
            return;
        }

        searchPanel = new GameObject("SearchPanel", typeof(RectTransform), typeof(Image));
        searchPanel.transform.SetParent(canvasTransform, false);
        searchPanel.SetActive(false);

        RectTransform panelRect = searchPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -160f);
        panelRect.sizeDelta = new Vector2(960f, 200f);

        Image panelImage = searchPanel.GetComponent<Image>();
        panelImage.color = new Color(1f, 1f, 1f, 0.97f);

        GameObject inputRoot = new GameObject("SearchInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputRoot.transform.SetParent(searchPanel.transform, false);

        RectTransform inputRect = inputRoot.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 0.15f);
        inputRect.anchorMax = new Vector2(1f, 0.85f);
        inputRect.offsetMin = new Vector2(40f, 0f);
        inputRect.offsetMax = new Vector2(-120f, 0f);

        Image inputBackground = inputRoot.GetComponent<Image>();
        Sprite defaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/InputFieldBackground.psd");
        if (defaultSprite == null)
        {
            defaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }
        inputBackground.sprite = defaultSprite;
        inputBackground.type = Image.Type.Sliced;
        inputBackground.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        TMP_InputField inputField = inputRoot.GetComponent<TMP_InputField>();
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.caretWidth = 2;

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputRoot.transform, false);
        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.anchorMin = new Vector2(0f, 0f);
        textAreaRect.anchorMax = new Vector2(1f, 1f);
        textAreaRect.offsetMin = new Vector2(16f, 12f);
        textAreaRect.offsetMax = new Vector2(-16f, -12f);

        GameObject placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderGO.transform.SetParent(textArea.transform, false);
        RectTransform placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        TextMeshProUGUI placeholderText = placeholderGO.GetComponent<TextMeshProUGUI>();
        placeholderText.text = "Buscar residuo...";
        placeholderText.fontSize = 48f;
        placeholderText.alignment = TextAlignmentOptions.MidlineLeft;
        placeholderText.color = new Color(0.55f, 0.55f, 0.55f, 0.7f);
        if (mainFontAsset != null)
        {
            placeholderText.font = mainFontAsset;
        }

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(textArea.transform, false);
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textComponent = textGO.GetComponent<TextMeshProUGUI>();
        textComponent.text = string.Empty;
        textComponent.fontSize = 48f;
        textComponent.alignment = TextAlignmentOptions.MidlineLeft;
        textComponent.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        textComponent.enableWordWrapping = false;
        if (mainFontAsset != null)
        {
            textComponent.font = mainFontAsset;
        }

        inputField.textViewport = textAreaRect;
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderText;

        inputField.onValueChanged.AddListener(OnSearchValueChanged);
        inputField.onSubmit.AddListener(_ => ToggleSearchPanel(false));

        searchInputField = inputField;

        GameObject closeButtonGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeButtonGO.transform.SetParent(searchPanel.transform, false);
        RectTransform closeRect = closeButtonGO.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-20f, -20f);
        closeRect.sizeDelta = new Vector2(80f, 80f);

        Image closeImage = closeButtonGO.GetComponent<Image>();
        closeImage.sprite = defaultSprite;
        closeImage.type = Image.Type.Sliced;
        closeImage.color = new Color(0.94f, 0.94f, 0.94f, 1f);

        searchCloseButton = closeButtonGO.GetComponent<Button>();
        searchCloseButton.onClick.AddListener(() => ToggleSearchPanel(false));

        GameObject closeTextGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTextGO.transform.SetParent(closeButtonGO.transform, false);
        RectTransform closeTextRect = closeTextGO.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI closeLabel = closeTextGO.GetComponent<TextMeshProUGUI>();
        closeLabel.text = "X";
        closeLabel.fontSize = 54f;
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        if (mainFontAsset != null)
        {
            closeLabel.font = mainFontAsset;
        }
    }

    private void ToggleSearchPanel(bool show)
    {
        if (searchPanel == null)
        {
            return;
        }

        if (show)
        {
            searchPanel.transform.SetAsLastSibling();
        }

        searchPanel.SetActive(show);

        if (show && searchInputField != null)
        {
            searchInputField.SetTextWithoutNotify(searchTerm);
            searchInputField.ActivateInputField();
            searchInputField.MoveTextEnd(false);
        }
        else if (!show && searchInputField != null)
        {
            searchInputField.DeactivateInputField();
        }
    }

    private void OnSearchValueChanged(string value)
    {
        searchTerm = value ?? string.Empty;
        ApplyFilters();
    }

    private void CreateFiltersModal(Transform canvasTransform)
    {
        filtersModal = new GameObject("FiltersModal", typeof(RectTransform), typeof(Image));
        filtersModal.transform.SetParent(canvasTransform, false);
        filtersModal.SetActive(false);

        RectTransform modalRect = filtersModal.GetComponent<RectTransform>();
        modalRect.anchorMin = Vector2.zero;
        modalRect.anchorMax = Vector2.one;
        modalRect.offsetMin = Vector2.zero;
        modalRect.offsetMax = Vector2.zero;

        Image modalBackground = filtersModal.GetComponent<Image>();
        modalBackground.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(filtersModal.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.2f);
        panelRect.anchorMax = new Vector2(0.9f, 0.8f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(1f, 1f, 1f, 0.97f);

        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(panel.transform, false);
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(30f, -100f);
        titleRect.offsetMax = new Vector2(-100f, -20f);

        TextMeshProUGUI titleText = titleGO.GetComponent<TextMeshProUGUI>();
        titleText.text = "Filtrar por subcategoría";
        titleText.fontSize = 60f;
        titleText.alignment = TextAlignmentOptions.MidlineLeft;
        titleText.color = new Color(0.18f, 0.43f, 0.2f, 1f);
        if (mainFontAsset != null)
        {
            titleText.font = mainFontAsset;
        }

        GameObject closeButtonGO = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeButtonGO.transform.SetParent(panel.transform, false);
        RectTransform closeRect = closeButtonGO.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-20f, -20f);
        closeRect.sizeDelta = new Vector2(80f, 80f);

        Image closeImage = closeButtonGO.GetComponent<Image>();
        Sprite defaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        closeImage.sprite = defaultSprite;
        closeImage.type = Image.Type.Sliced;
        closeImage.color = new Color(0.94f, 0.94f, 0.94f, 1f);

        filtersCloseButton = closeButtonGO.GetComponent<Button>();
        filtersCloseButton.onClick.AddListener(() => ToggleFiltersModal(false));

        GameObject closeTextGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTextGO.transform.SetParent(closeButtonGO.transform, false);
        RectTransform closeTextRect = closeTextGO.GetComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.offsetMin = Vector2.zero;
        closeTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI closeLabel = closeTextGO.GetComponent<TextMeshProUGUI>();
        closeLabel.text = "X";
        closeLabel.fontSize = 54f;
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        if (mainFontAsset != null)
        {
            closeLabel.font = mainFontAsset;
        }

        GameObject scrollViewGO = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
        scrollViewGO.transform.SetParent(panel.transform, false);
        RectTransform scrollRectTransform = scrollViewGO.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(30f, 40f);
        scrollRectTransform.offsetMax = new Vector2(-30f, -140f);

        Image scrollImage = scrollViewGO.GetComponent<Image>();
        scrollImage.sprite = defaultSprite;
        scrollImage.type = Image.Type.Sliced;
        scrollImage.color = new Color(0.96f, 0.98f, 0.97f, 1f);

        ScrollRect scrollRect = scrollViewGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(scrollViewGO.transform, false);

        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(0f, 0f);
        contentRect.offsetMax = new Vector2(0f, 0f);

        VerticalLayoutGroup layoutGroup = contentGO.GetComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.spacing = 10f;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;

        ContentSizeFitter fitter = contentGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRect;
        scrollRect.viewport = scrollRectTransform;

        filtersListContainer = contentGO.transform;
    }

    private void ToggleFiltersModal(bool show)
    {
        if (filtersModal == null)
        {
            return;
        }

        filtersModal.SetActive(show);

        if (show)
        {
            filtersModal.transform.SetAsLastSibling();

            foreach (KeyValuePair<string, Toggle> pair in subcategoryToggles)
            {
                bool shouldBeOn = activeSubcategories.Contains(pair.Key);
                pair.Value.SetIsOnWithoutNotify(shouldBeOn);
            }
        }
    }

    private string SanitizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "Item";
        }

        char[] safeChars = value.Where(char.IsLetterOrDigit).ToArray();
        string sanitized = new string(safeChars);
        return string.IsNullOrEmpty(sanitized) ? "Item" : sanitized;
    }
}
