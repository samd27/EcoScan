using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResiduoListController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private string catalogResourcePath = "Data/residuos_catalog";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private ResiduoCatalog catalog;
    private readonly List<Residuo> currentResults = new List<Residuo>();
    private readonly List<ResiduoCard> cardPool = new List<ResiduoCard>();
    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private readonly Dictionary<Button, Color> baseButtonColors = new Dictionary<Button, Color>();

    private RectTransform root;
    private InputField searchField;
    private Button organicButton;
    private Button inorganicButton;
    private Button materialButton;
    private Button sortButton;
    private Text materialButtonText;
    private Text sortButtonText;
    private ScrollRect scrollRect;
    private RectTransform content;
    private GameObject materialOverlay;
    private RectTransform materialPanel;
    private RectTransform materialPanelContent;

    private GameObject detailsOverlay;
    private Image detailsImage;
    private Text detailsNameText;
    private Text detailsCategoryText;
    private Text detailsMaterialText;
    private Text detailsDisposalText;
    private Text detailsDescriptionText;

    private readonly List<Button> materialOptionButtons = new List<Button>();

    private string currentCategoryFilter;
    private string currentMaterialFilter;
    private bool sortAscending = true;

    [Header("Estilo")]
    [SerializeField] private Font uiFont;

    private Font defaultFont;

    private void Awake()
    {
        defaultFont = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    private void Start()
    {
        catalog = LoadCatalog();
        if (catalog == null)
        {
            Debug.LogError("No se pudo cargar el catálogo de residuos.");
            return;
        }

        BuildLayout();
        ApplyFilters();
    }

    private ResiduoCatalog LoadCatalog()
    {
        var textAsset = Resources.Load<TextAsset>(catalogResourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"No se encontró el archivo de catálogo en Resources/{catalogResourcePath}.json");
            return null;
        }

        try
        {
            return JsonUtility.FromJson<ResiduoCatalog>(textAsset.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error al parsear catálogo de residuos: {ex.Message}");
            return null;
        }
    }

    private void BuildLayout()
    {
        var children = new List<Transform>();
        foreach (Transform child in transform)
        {
            children.Add(child);
        }
        foreach (var child in children)
        {
            Destroy(child.gameObject);
        }

        root = CreateRectTransform("ResiduoListRoot", transform as RectTransform);
        var background = root.gameObject.AddComponent<Image>();
        background.color = Color.white;

        BuildHeader();
        BuildSearchBar();
        BuildFilters();
        BuildMaterialPanel();
        BuildScrollView();
        BuildDetailsOverlay();
    }

    private void BuildHeader()
    {
        var header = CreateRectTransform("Header", root);
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0f, 120f);

        var headerBackground = header.gameObject.AddComponent<Image>();
        headerBackground.color = new Color(0.95f, 0.96f, 0.98f);

        var backButton = CreateButton("BackButton", header, new Vector2(80f, 80f));
        backButton.anchorMin = new Vector2(0f, 1f);
        backButton.anchorMax = new Vector2(0f, 1f);
        backButton.pivot = new Vector2(0f, 1f);
        backButton.anchoredPosition = new Vector2(40f, -40f);
        var backImage = backButton.GetComponent<Image>();
        backImage.color = new Color(0.9f, 0.9f, 0.9f);
        var backLabel = CreateTextElement("Label", backButton, "←", 42, FontStyle.Bold);
        backLabel.alignment = TextAnchor.MiddleCenter;
        var back = backButton.GetComponent<Button>();
        back.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));

    }

    private void BuildSearchBar()
    {
        var container = CreateRectTransform("SearchBar", root);
        container.anchorMin = new Vector2(0.05f, 1f);
        container.anchorMax = new Vector2(0.95f, 1f);
        container.pivot = new Vector2(0.5f, 1f);
        container.anchoredPosition = new Vector2(0f, -150f);
        container.sizeDelta = new Vector2(0f, 90f);

        var searchObject = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField));
        var searchRect = searchObject.GetComponent<RectTransform>();
        searchRect.SetParent(container, false);
        searchRect.anchorMin = new Vector2(0f, 0f);
        searchRect.anchorMax = new Vector2(1f, 1f);
        searchRect.offsetMin = Vector2.zero;
        searchRect.offsetMax = Vector2.zero;
        var searchImage = searchObject.GetComponent<Image>();
        searchImage.color = new Color(0.94f, 0.95f, 0.97f);
        searchImage.raycastTarget = true;

        var placeholder = CreateTextElement("Placeholder", searchRect, "Buscar residuo o palabra clave", 30, FontStyle.Normal);
        placeholder.color = new Color(0.45f, 0.45f, 0.45f, 0.75f);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.rectTransform.anchorMin = new Vector2(0f, 0f);
        placeholder.rectTransform.anchorMax = new Vector2(1f, 1f);
        placeholder.rectTransform.offsetMin = new Vector2(20f, 0f);
        placeholder.rectTransform.offsetMax = new Vector2(-20f, 0f);
        placeholder.horizontalOverflow = HorizontalWrapMode.Overflow;

        var text = CreateTextElement("Text", searchRect, string.Empty, 32, FontStyle.Normal);
        text.color = new Color(0.15f, 0.2f, 0.25f);
        text.alignment = TextAnchor.MiddleLeft;
        text.rectTransform.anchorMin = new Vector2(0f, 0f);
        text.rectTransform.anchorMax = new Vector2(1f, 1f);
        text.rectTransform.offsetMin = new Vector2(20f, 0f);
        text.rectTransform.offsetMax = new Vector2(-20f, 0f);

        searchField = searchObject.GetComponent<InputField>();
        searchField.textComponent = text;
        searchField.placeholder = placeholder;
        searchField.onValueChanged.AddListener(_ => ApplyFilters());
        searchField.lineType = InputField.LineType.SingleLine;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    private void BuildFilters()
    {
        var filterRow = new GameObject("FilterRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        var filterRect = filterRow.GetComponent<RectTransform>();
        filterRect.SetParent(root, false);
        filterRect.anchorMin = new Vector2(0f, 1f);
        filterRect.anchorMax = new Vector2(1f, 1f);
        filterRect.pivot = new Vector2(0.5f, 1f);
        filterRect.anchoredPosition = new Vector2(0f, -260f);
        filterRect.sizeDelta = new Vector2(0f, 90f);

        var layout = filterRow.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(40, 40, 0, 0);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childControlHeight = false;
        layout.childControlWidth = false;

        organicButton = CreatePillButton("Orgánico", filterRect, new Color(0.84f, 0.94f, 0.84f));
        organicButton.onClick.AddListener(() => ToggleCategory("ORGANICO"));
        var organicLayout = organicButton.GetComponent<LayoutElement>();
        organicLayout.minWidth = 180f;
        organicLayout.preferredWidth = 200f;

        inorganicButton = CreatePillButton("Inorgánico", filterRect, new Color(0.84f, 0.9f, 0.97f));
        inorganicButton.onClick.AddListener(() => ToggleCategory("INORGANICO"));
        var inorganicLayout = inorganicButton.GetComponent<LayoutElement>();
        inorganicLayout.minWidth = 200f;
        inorganicLayout.preferredWidth = 220f;

        materialButton = CreatePillButton("Materiales", filterRect, new Color(0.93f, 0.93f, 0.93f));
        materialButton.interactable = false;
        materialButton.onClick.AddListener(ToggleMaterialPanel);
        materialButtonText = materialButton.GetComponentInChildren<Text>();
        materialButton.GetComponent<LayoutElement>().preferredWidth = 220f;
        materialButtonText.alignment = TextAnchor.MiddleCenter;
        materialButtonText.horizontalOverflow = HorizontalWrapMode.Overflow;
        materialButtonText.resizeTextForBestFit = true;
        materialButtonText.resizeTextMinSize = 18;
        materialButtonText.resizeTextMaxSize = materialButtonText.fontSize;

        var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(filterRect, false);
        var spacerLayout = spacer.GetComponent<LayoutElement>();
        spacerLayout.flexibleWidth = 1f;

        sortButton = CreatePillButton("A-Z", filterRect, new Color(0.9f, 0.9f, 0.9f));
        var sortLayout = sortButton.GetComponent<LayoutElement>();
        sortLayout.preferredWidth = 140f;
        sortButton.onClick.AddListener(() =>
        {
            sortAscending = !sortAscending;
            UpdateSortButtonLabel();
            ApplyFilters();
        });
        sortButtonText = sortButton.GetComponentInChildren<Text>();
        sortButtonText.alignment = TextAnchor.MiddleCenter;
        sortButtonText.horizontalOverflow = HorizontalWrapMode.Overflow;
        UpdateSortButtonLabel();
    }

    private void BuildMaterialPanel()
    {
        materialOverlay = new GameObject("MaterialOverlay", typeof(RectTransform), typeof(Image));
        var overlayRect = materialOverlay.GetComponent<RectTransform>();
        overlayRect.SetParent(root, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        var overlayImage = materialOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.45f);
        overlayImage.raycastTarget = true;

        var overlayButton = materialOverlay.AddComponent<Button>();
        overlayButton.transition = Selectable.Transition.None;
        overlayButton.targetGraphic = overlayImage;
        overlayButton.onClick.AddListener(HideMaterialOverlay);

        materialPanel = new GameObject("MaterialPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        materialPanel.SetParent(overlayRect, false);
        materialPanel.anchorMin = new Vector2(0.5f, 0.5f);
        materialPanel.anchorMax = new Vector2(0.5f, 0.5f);
        materialPanel.pivot = new Vector2(0.5f, 0.5f);
        materialPanel.sizeDelta = new Vector2(640f, 820f);
        var panelImage = materialPanel.GetComponent<Image>();
        panelImage.color = Color.white;

        var panelLayout = materialPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(36, 36, 36, 36);
        panelLayout.spacing = 20f;
        panelLayout.childAlignment = TextAnchor.UpperCenter;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandHeight = false;

        var closeButton = CreatePillButton("Salir", materialPanel, new Color(0.92f, 0.92f, 0.92f));
        var closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.SetSiblingIndex(0);
        var closeLayout = closeButton.GetComponent<LayoutElement>();
        closeLayout.ignoreLayout = true;
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-20f, -20f);
        closeRect.sizeDelta = new Vector2(160f, 64f);
        closeButton.onClick.AddListener(HideMaterialOverlay);
        var closeLabel = closeButton.GetComponentInChildren<Text>();
        if (closeLabel != null)
        {
            closeLabel.alignment = TextAnchor.MiddleCenter;
        }

        var title = CreateTextElement("Title", materialPanel, "Filtrar por material", 40, FontStyle.Bold);
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.15f, 0.2f, 0.25f);
        var titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 80f;

        var instructions = CreateTextElement("Hint", materialPanel, "Selecciona un material para refinar los resultados.", 28, FontStyle.Normal);
        instructions.alignment = TextAnchor.MiddleCenter;
        instructions.color = new Color(0.3f, 0.3f, 0.35f);
        var instructionsLayout = instructions.gameObject.AddComponent<LayoutElement>();
        instructionsLayout.preferredHeight = 60f;

        var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
        scrollRectTransform.SetParent(materialPanel, false);
        scrollRectTransform.sizeDelta = new Vector2(0f, 0f);
        var scrollImage = scrollGO.GetComponent<Image>();
        scrollImage.color = new Color(0.97f, 0.97f, 0.98f);

        var scrollLayout = scrollGO.AddComponent<LayoutElement>();
        scrollLayout.preferredHeight = 520f;
        scrollLayout.flexibleHeight = 1f;

        var materialScroll = scrollGO.GetComponent<ScrollRect>();
        materialScroll.horizontal = false;
        materialScroll.vertical = true;
        materialScroll.movementType = ScrollRect.MovementType.Elastic;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.SetParent(scrollRectTransform, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-10f, -10f);
        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = Color.white;
        viewportImage.raycastTarget = true;
        var viewportMask = viewport.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        materialPanelContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        materialPanelContent.SetParent(viewportRect, false);
        materialPanelContent.anchorMin = new Vector2(0f, 1f);
        materialPanelContent.anchorMax = new Vector2(1f, 1f);
        materialPanelContent.pivot = new Vector2(0.5f, 1f);
        materialPanelContent.anchoredPosition = Vector2.zero;

        var verticalLayout = materialPanelContent.GetComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.spacing = 12f;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childControlHeight = false;
        verticalLayout.childForceExpandHeight = false;

        var fitter = materialPanelContent.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        materialScroll.content = materialPanelContent;
        materialScroll.viewport = viewportRect;

        materialOverlay.SetActive(false);
    }

    private void BuildScrollView()
    {
        var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
        scrollRectTransform.SetParent(root, false);
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(40f, 40f);
        scrollRectTransform.offsetMax = new Vector2(-40f, -360f);

        var scrollBackground = scrollGO.GetComponent<Image>();
        scrollBackground.color = new Color(0.97f, 0.97f, 0.98f);

        scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.SetParent(scrollRectTransform, false);
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-10f, -10f);
        var maskImage = viewport.GetComponent<Image>();
        maskImage.color = Color.white;
        maskImage.raycastTarget = true;
        var mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        content.SetParent(viewportRect, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        var grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(320f, 360f);
        grid.spacing = new Vector2(24f, 24f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = content;
        scrollRect.viewport = viewportRect;
    }

    private void BuildDetailsOverlay()
    {
        detailsOverlay = new GameObject("DetailsOverlay", typeof(RectTransform), typeof(Image));
        var overlayRect = detailsOverlay.GetComponent<RectTransform>();
        overlayRect.SetParent(root, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        var overlayImage = detailsOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.5f);
        overlayImage.raycastTarget = true;

        var overlayButton = detailsOverlay.AddComponent<Button>();
        overlayButton.transition = Selectable.Transition.None;
        overlayButton.targetGraphic = overlayImage;
        overlayButton.onClick.AddListener(HideDetails);
        detailsOverlay.SetActive(false);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.SetParent(overlayRect, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720f, 960f);
        var panelImage = panel.GetComponent<Image>();
        panelImage.color = Color.white;

        var panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(40, 40, 40, 40);
        panelLayout.spacing = 20f;
        panelLayout.childAlignment = TextAnchor.UpperLeft;

        var closeButton = CreatePillButton("Salir", panelRect, new Color(0.92f, 0.92f, 0.92f));
        var closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.SetSiblingIndex(0);
        var closeLayout = closeButton.GetComponent<LayoutElement>();
        closeLayout.ignoreLayout = true;
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-20f, -20f);
        closeRect.sizeDelta = new Vector2(160f, 64f);
        closeButton.onClick.AddListener(HideDetails);
        closeButton.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleCenter;

        detailsNameText = CreateTextElement("Name", panelRect, string.Empty, 42, FontStyle.Bold);
        detailsNameText.alignment = TextAnchor.UpperLeft;
        detailsNameText.color = new Color(0.15f, 0.2f, 0.25f);
        var nameLayout = detailsNameText.gameObject.AddComponent<LayoutElement>();
        nameLayout.preferredHeight = 90f;

        var imageContainer = new GameObject("Image", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
        var imageRect = imageContainer.GetComponent<RectTransform>();
        imageRect.SetParent(panelRect, false);
        imageRect.sizeDelta = new Vector2(0f, 0f);
        var aspect = imageContainer.GetComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
        aspect.aspectRatio = 1.2f;
        var imageLayout = imageContainer.AddComponent<LayoutElement>();
        imageLayout.preferredHeight = 0f;
        imageLayout.flexibleHeight = 1f;
        detailsImage = imageContainer.GetComponent<Image>();
        detailsImage.color = new Color(0.9f, 0.9f, 0.9f);

        detailsCategoryText = CreateTextElement("Category", panelRect, string.Empty, 32, FontStyle.Bold);
        detailsCategoryText.color = new Color(0.2f, 0.45f, 0.25f);
        var categoryLayout = detailsCategoryText.gameObject.AddComponent<LayoutElement>();
        categoryLayout.preferredHeight = 60f;

        detailsMaterialText = CreateTextElement("Material", panelRect, string.Empty, 30, FontStyle.Normal);
        detailsMaterialText.color = new Color(0.25f, 0.25f, 0.25f);
        var materialLayout = detailsMaterialText.gameObject.AddComponent<LayoutElement>();
        materialLayout.preferredHeight = 50f;

        detailsDisposalText = CreateTextElement("Disposal", panelRect, string.Empty, 30, FontStyle.Italic);
        detailsDisposalText.color = new Color(0.18f, 0.35f, 0.55f);
        var disposalLayout = detailsDisposalText.gameObject.AddComponent<LayoutElement>();
        disposalLayout.preferredHeight = 60f;

        detailsDescriptionText = CreateTextElement("Description", panelRect, string.Empty, 28, FontStyle.Normal);
        detailsDescriptionText.color = new Color(0.2f, 0.2f, 0.2f);
        detailsDescriptionText.alignment = TextAnchor.UpperLeft;
        detailsDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailsDescriptionText.verticalOverflow = VerticalWrapMode.Truncate;
        var descriptionLayout = detailsDescriptionText.gameObject.AddComponent<LayoutElement>();
        descriptionLayout.preferredHeight = 260f;
        descriptionLayout.flexibleHeight = 1f;
    }

    private void ToggleCategory(string category)
    {
        if (string.Equals(currentCategoryFilter, category, StringComparison.OrdinalIgnoreCase))
        {
            currentCategoryFilter = null;
        }
        else
        {
            currentCategoryFilter = category;
        }

        if (currentCategoryFilter == null)
        {
            currentMaterialFilter = null;
            materialButton.interactable = false;
            materialButtonText.text = "Materiales";
            HideMaterialOverlay();
        }
        else
        {
            materialButton.interactable = true;
            materialButtonText.text = string.IsNullOrEmpty(currentMaterialFilter) ? "Materiales" : currentMaterialFilter;
            UpdateMaterialOptions();
        }

        UpdateCategoryButtonVisuals();
        ApplyFilters();
    }

    private void ToggleMaterialPanel()
    {
        if (materialOverlay == null)
        {
            return;
        }

        if (!materialOverlay.activeSelf)
        {
            UpdateMaterialOptions();
            materialOverlay.SetActive(true);
        }
        else
        {
            HideMaterialOverlay();
        }
    }

    private void UpdateCategoryButtonVisuals()
    {
        SetButtonState(organicButton, string.Equals(currentCategoryFilter, "ORGANICO", StringComparison.OrdinalIgnoreCase));
        SetButtonState(inorganicButton, string.Equals(currentCategoryFilter, "INORGANICO", StringComparison.OrdinalIgnoreCase));
    }

    private void SetButtonState(Button button, bool active)
    {
        var colors = button.colors;
        baseButtonColors.TryGetValue(button, out var baseColor);
        if (baseColor == default)
        {
            baseColor = new Color(0.9f, 0.9f, 0.9f);
        }

        Color activeColor;
        Color highlightedActive;
        Color pressedActive;
        if (button == organicButton)
        {
            activeColor = new Color(0.2f, 0.6f, 0.4f);
            highlightedActive = new Color(0.25f, 0.65f, 0.45f);
            pressedActive = new Color(0.18f, 0.55f, 0.38f);
        }
        else if (button == inorganicButton)
        {
            activeColor = new Color(0.2f, 0.45f, 0.75f);
            highlightedActive = new Color(0.25f, 0.5f, 0.8f);
            pressedActive = new Color(0.18f, 0.4f, 0.7f);
        }
        else
        {
            activeColor = new Color(0.2f, 0.6f, 0.4f);
            highlightedActive = new Color(0.25f, 0.65f, 0.45f);
            pressedActive = new Color(0.18f, 0.55f, 0.38f);
        }

        Color inactiveColor = baseColor;
        Color inactiveHighlight = Color.Lerp(inactiveColor, Color.white, 0.15f);
        Color inactivePressed = Color.Lerp(inactiveColor, Color.black, 0.1f);

        colors.normalColor = active ? activeColor : inactiveColor;
        colors.highlightedColor = active ? highlightedActive : inactiveHighlight;
        colors.pressedColor = active ? pressedActive : inactivePressed;
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = colors.normalColor;
        }

        var text = button.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.color = active ? Color.white : new Color(0.2f, 0.2f, 0.2f);
        }
    }

    private void UpdateSortButtonLabel()
    {
        if (sortButtonText != null)
        {
            sortButtonText.text = sortAscending ? "A-Z" : "Z-A";
        }
    }

    private void UpdateMaterialOptions()
    {
        foreach (var button in materialOptionButtons)
        {
            if (button != null)
            {
                baseButtonColors.Remove(button);
                Destroy(button.gameObject);
            }
        }
        materialOptionButtons.Clear();

        if (string.IsNullOrEmpty(currentCategoryFilter))
        {
            HideMaterialOverlay();
            return;
        }

        var materials = catalog.residuos
            .Where(r => string.Equals(r.categoria, currentCategoryFilter, StringComparison.OrdinalIgnoreCase))
            .Select(r => r.material)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m)
            .ToList();

        CreateMaterialOption("Todos los materiales", null);
        foreach (var material in materials)
        {
            CreateMaterialOption(material, material);
        }
    }

    private void CreateMaterialOption(string label, string value)
    {
        var optionButton = CreatePillButton(label, materialPanelContent, new Color(0.92f, 0.92f, 0.92f));
        var layout = optionButton.GetComponent<LayoutElement>();
        layout.preferredWidth = 0f;
        layout.flexibleWidth = 1f;

        bool isSelected = string.IsNullOrEmpty(value)
            ? string.IsNullOrEmpty(currentMaterialFilter)
            : string.Equals(currentMaterialFilter, value, StringComparison.OrdinalIgnoreCase);
        SetButtonState(optionButton, isSelected);

        optionButton.onClick.AddListener(() =>
        {
            currentMaterialFilter = value;
            materialButtonText.text = string.IsNullOrEmpty(value) ? "Materiales" : value;
            HideMaterialOverlay();
            ApplyFilters();
        });
        materialOptionButtons.Add(optionButton);
    }

    private void ApplyFilters()
    {
        if (catalog == null || catalog.residuos == null)
        {
            return;
        }

        var term = ResiduoStringUtility.Normalize(searchField != null ? searchField.text : string.Empty);

        IEnumerable<Residuo> query = catalog.residuos;
        if (!string.IsNullOrEmpty(currentCategoryFilter))
        {
            query = query.Where(r => string.Equals(r.categoria, currentCategoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(currentMaterialFilter))
        {
            query = query.Where(r => string.Equals(r.material, currentMaterialFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(term))
        {
            query = query.Where(r => r.AllSearchTokens().Any(token => token.Contains(term)));
        }

        currentResults.Clear();
        currentResults.AddRange(query);
        currentResults.Sort((a, b) => string.Compare(a.nombre, b.nombre, StringComparison.OrdinalIgnoreCase));
        if (!sortAscending)
        {
            currentResults.Reverse();
        }

        RenderResults();
    }

    private void RenderResults()
    {
        for (int i = 0; i < currentResults.Count; i++)
        {
            ResiduoCard card;
            if (i < cardPool.Count)
            {
                card = cardPool[i];
            }
            else
            {
                card = CreateCard();
                card.root.transform.SetParent(content, false);
                cardPool.Add(card);
            }

            var residuo = currentResults[i];
            card.root.SetActive(true);
            card.data = residuo;
            card.nameText.text = residuo.nombre;
            card.categoryText.text = FormatCategory(residuo);
            card.button.onClick.RemoveAllListeners();
            var capturedCard = card;
            card.button.onClick.AddListener(() => ShowDetails(capturedCard));
            card.thumbnail.sprite = null;
            card.thumbnail.color = new Color(0.85f, 0.85f, 0.85f);
            card.imageUrl = residuo.img;
            StartCoroutine(LoadImageForCard(card));
        }

        for (int i = currentResults.Count; i < cardPool.Count; i++)
        {
            cardPool[i].root.SetActive(false);
        }
    }

    private IEnumerator LoadImageForCard(ResiduoCard card)
    {
        if (string.IsNullOrWhiteSpace(card.imageUrl))
        {
            yield break;
        }

        if (spriteCache.TryGetValue(card.imageUrl, out var cachedSprite))
        {
            card.thumbnail.sprite = cachedSprite;
            card.thumbnail.color = Color.white;
            yield break;
        }

        using (var request = UnityWebRequestTexture.GetTexture(card.imageUrl))
        {
            yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogWarning($"No se pudo cargar la imagen {card.imageUrl}: {request.error}");
                yield break;
            }

            var texture = DownloadHandlerTexture.GetContent(request);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            spriteCache[card.imageUrl] = sprite;

            if (card.data != null && string.Equals(card.data.img, card.imageUrl, StringComparison.OrdinalIgnoreCase))
            {
                card.thumbnail.sprite = sprite;
                card.thumbnail.color = Color.white;
            }
        }
    }

    private ResiduoCard CreateCard()
    {
        var cardObject = new GameObject("ResiduoCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        var rect = cardObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var background = cardObject.GetComponent<Image>();
        background.color = new Color(0.94f, 0.95f, 0.97f);

        var button = cardObject.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = background.color;
        colors.highlightedColor = new Color(0.88f, 0.9f, 0.95f);
        colors.pressedColor = new Color(0.8f, 0.83f, 0.9f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;

        var layout = cardObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        var layoutElement = cardObject.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 360f;
        layoutElement.flexibleHeight = 0f;

        var thumbnailObject = new GameObject("Thumbnail", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter), typeof(LayoutElement));
        var thumbnailRect = thumbnailObject.GetComponent<RectTransform>();
        thumbnailRect.SetParent(cardObject.transform, false);
        var thumbnailImage = thumbnailObject.GetComponent<Image>();
        thumbnailImage.color = new Color(0.85f, 0.85f, 0.85f);
        thumbnailImage.preserveAspect = true;
        var aspect = thumbnailObject.GetComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
        aspect.aspectRatio = 1f;
        var thumbnailLayout = thumbnailObject.GetComponent<LayoutElement>();
        thumbnailLayout.preferredHeight = 200f;
        thumbnailLayout.flexibleHeight = 1f;

        var nameText = CreateTextElement("Name", cardObject.transform, string.Empty, 30, FontStyle.Bold);
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = new Color(0.2f, 0.2f, 0.25f);
        nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
        var nameLayoutElement = nameText.gameObject.AddComponent<LayoutElement>();
        nameLayoutElement.preferredHeight = 72f;
        nameLayoutElement.minHeight = 60f;

        var categoryText = CreateTextElement("Category", cardObject.transform, string.Empty, 26, FontStyle.Normal);
        categoryText.alignment = TextAnchor.MiddleCenter;
        categoryText.color = new Color(0.3f, 0.35f, 0.4f);
        categoryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        var categoryLayoutElement = categoryText.gameObject.AddComponent<LayoutElement>();
        categoryLayoutElement.preferredHeight = 58f;
        categoryLayoutElement.minHeight = 48f;

        return new ResiduoCard
        {
            root = cardObject,
            button = button,
            thumbnail = thumbnailImage,
            nameText = nameText,
            categoryText = categoryText,
            background = background
        };
    }

    private void ShowDetails(ResiduoCard card)
    {
        if (card == null || card.data == null)
        {
            return;
        }

        detailsOverlay.SetActive(true);
        detailsNameText.text = card.data.nombre;
        detailsCategoryText.text = FormatCategory(card.data);
        detailsMaterialText.text = $"Material: {card.data.material}{(string.IsNullOrWhiteSpace(card.data.submaterial) ? string.Empty : " (" + card.data.submaterial + ")")}";
        detailsDisposalText.text = GetDisposalHint(card.data);
        detailsDescriptionText.text = string.IsNullOrWhiteSpace(card.data.descripcion) ? "Sin descripción disponible." : card.data.descripcion;

        if (!string.IsNullOrWhiteSpace(card.data.img) && spriteCache.TryGetValue(card.data.img, out var sprite))
        {
            detailsImage.sprite = sprite;
            detailsImage.color = Color.white;
        }
        else
        {
            detailsImage.sprite = null;
            detailsImage.color = new Color(0.9f, 0.9f, 0.9f);
        }
    }

    private void HideDetails()
    {
        detailsOverlay.SetActive(false);
    }

    private void HideMaterialOverlay()
    {
        if (materialOverlay != null)
        {
            materialOverlay.SetActive(false);
        }
    }

    private string FormatCategory(Residuo residuo)
    {
        var categoria = string.Equals(residuo.categoria, "ORGANICO", StringComparison.OrdinalIgnoreCase) ? "Orgánico" : "Inorgánico";
        if (!string.IsNullOrWhiteSpace(residuo.subcategoria))
        {
            return $"{categoria} · {residuo.subcategoria}";
        }

        return categoria;
    }

    private string GetDisposalHint(Residuo residuo)
    {
        if (residuo == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(residuo.subcategoria))
        {
            if (residuo.subcategoria.IndexOf("reciclable", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Depositar en contenedor de reciclables limpios.";
            }

            if (residuo.subcategoria.IndexOf("compost", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Agregar a composta o contenedor orgánico.";
            }

            if (residuo.subcategoria.IndexOf("peligroso", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Llevar a un centro de acopio para residuos peligrosos.";
            }
        }

        return string.Equals(residuo.categoria, "ORGANICO", StringComparison.OrdinalIgnoreCase)
            ? "Depositar en contenedor de orgánicos."
            : "Depositar en contenedor gris o de inorgánicos.";
    }

    private RectTransform CreateRectTransform(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private RectTransform CreateButton(string name, RectTransform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = new Color(0.9f, 0.9f, 0.9f);
        image.raycastTarget = true;
        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = colors.normalColor;
        }
        return rect;
    }

    private Button CreatePillButton(string text, RectTransform parent, Color backgroundColor)
    {
        var go = new GameObject(text + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.color = backgroundColor;
        image.raycastTarget = true;

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.15f);
        colors.disabledColor = Color.Lerp(backgroundColor, Color.white, 0.6f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;
        button.targetGraphic = image;

        var layoutElement = go.GetComponent<LayoutElement>();
        layoutElement.minHeight = 64f;
        layoutElement.preferredHeight = 64f;

        var label = CreateTextElement("Label", rect, text, 28, FontStyle.Bold);
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.2f, 0.2f, 0.25f);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(24f, 12f);
        label.rectTransform.offsetMax = new Vector2(-24f, -12f);

        baseButtonColors[button] = backgroundColor;

        return button;
    }

    private Text CreateTextElement(string name, Transform parent, string value, int fontSize, FontStyle fontStyle)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = defaultFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.text = value;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.black;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.supportRichText = false;
        text.raycastTarget = false;
        text.lineSpacing = 1.1f;

        var rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return text;
    }

    private class ResiduoCard
    {
        public GameObject root;
        public Button button;
        public Image background;
        public Image thumbnail;
        public Text nameText;
        public Text categoryText;
        public Residuo data;
        public string imageUrl;
    }
}
