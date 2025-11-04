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
    private RectTransform headerRect;
    private RectTransform searchBarRect;
    private RectTransform filterRect;

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
    private RectTransform detailsPanelRect;
    private RectTransform detailsContentRect;
    private ScrollRect detailsScroll;
    private Image detailsImage;
    private Text detailsNameText;
    private Text detailsCategoryText;
    private Text detailsMaterialText;
    private Text detailsDisposalText;
    private Text detailsDescriptionText;
    private Text detailsHeaderText;

    private readonly List<Button> materialOptionButtons = new List<Button>();

    private string currentCategoryFilter;
    private string currentMaterialFilter;
    private bool sortAscending = true;

    private const string DefaultMaterialLabel = "Materiales";

    private Font defaultFont;

    private void Awake()
    {
        defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
        StartCoroutine(DelayedResponsiveRebuild());
    }

    private IEnumerator DelayedResponsiveRebuild()
    {
        yield return null;
        ApplyResponsiveLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyResponsiveLayout();
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
        foreach (Transform child in transform) children.Add(child);
        foreach (var child in children) Destroy(child.gameObject);

        root = CreateRectTransform("ResiduoListRoot", transform as RectTransform);
        var background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.97f, 0.96f, 0.99f);

        BuildHeader();
        BuildSearchBar();
        BuildFilters();
        BuildMaterialPanel();
        BuildScrollView();
        BuildDetailsOverlay();
        UpdateCategoryButtonVisuals();
    }

    private void BuildHeader()
    {
        headerRect = CreateRectTransform("Header", root);
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = Vector2.zero;
        headerRect.sizeDelta = new Vector2(0f, 100f);

        var headerBackground = headerRect.gameObject.AddComponent<Image>();
        headerBackground.color = new Color(0.95f, 0.95f, 0.99f);

        var backButton = CreateButton("BackButton", headerRect, new Vector2(60f, 60f));
        backButton.anchorMin = new Vector2(0f, 0.5f);
        backButton.anchorMax = new Vector2(0f, 0.5f);
        backButton.pivot = new Vector2(0f, 0.5f);
        backButton.anchoredPosition = new Vector2(25f, 0f);
        var backImage = backButton.GetComponent<Image>();
        backImage.color = new Color(0.85f, 0.85f, 0.85f);
        var backLabel = CreateTextElement("Label", backButton, "←", 36, FontStyle.Bold);
        backLabel.alignment = TextAnchor.MiddleCenter;
        var back = backButton.GetComponent<Button>();
        back.onClick.AddListener(() => SceneManager.LoadScene(mainMenuSceneName));

        var title = CreateTextElement("Title", headerRect, "Listado de productos", 34, FontStyle.Bold);
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.21f, 0.24f, 0.32f);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 0f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = new Vector2(100f, 0f);
        titleRt.offsetMax = new Vector2(-100f, 0f);
    }

    private void BuildSearchBar()
    {
        searchBarRect = CreateRectTransform("SearchBar", root);
        searchBarRect.anchorMin = new Vector2(0.05f, 1f);
        searchBarRect.anchorMax = new Vector2(0.95f, 1f);
        searchBarRect.pivot = new Vector2(0.5f, 1f);
        searchBarRect.anchoredPosition = new Vector2(0f, -120f);
        searchBarRect.sizeDelta = new Vector2(0f, 70f);

        var searchObject = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField));
        var searchRect = searchObject.GetComponent<RectTransform>();
        searchRect.SetParent(searchBarRect, false);
        searchRect.anchorMin = new Vector2(0f, 0f);
        searchRect.anchorMax = new Vector2(1f, 1f);
        searchRect.offsetMin = Vector2.zero;
        searchRect.offsetMax = Vector2.zero;
        var searchImage = searchObject.GetComponent<Image>();
        searchImage.color = Color.white;

        var placeholder = CreateTextElement("Placeholder", searchRect, "Buscar residuo o palabra clave", 24, FontStyle.Normal);
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.rectTransform.offsetMin = new Vector2(20f, 0f);
        placeholder.rectTransform.offsetMax = new Vector2(-20f, 0f);

        var text = CreateTextElement("Text", searchRect, string.Empty, 26, FontStyle.Normal);
        text.color = new Color(0.2f, 0.2f, 0.2f);
        text.alignment = TextAnchor.MiddleLeft;
        text.rectTransform.offsetMin = new Vector2(20f, 0f);
        text.rectTransform.offsetMax = new Vector2(-20f, 0f);

        searchField = searchObject.GetComponent<InputField>();
        searchField.textComponent = text;
        searchField.placeholder = placeholder;
        searchField.onValueChanged.AddListener(_ => ApplyFilters());
        searchField.lineType = InputField.LineType.SingleLine;
    }

    private void BuildFilters()
    {
        filterRect = CreateRectTransform("FilterContainer", root);
        filterRect.anchorMin = new Vector2(0f, 1f);
        filterRect.anchorMax = new Vector2(1f, 1f);
        filterRect.pivot = new Vector2(0.5f, 1f);
        filterRect.anchoredPosition = new Vector2(0f, -210f);
        filterRect.sizeDelta = new Vector2(0f, 70f);

        var filterBackground = filterRect.gameObject.AddComponent<Image>();
        filterBackground.color = Color.white;

        var layout = filterRect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 10, 10);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;

        sortButton = CreatePillButton("A-Z", filterRect, new Color(0.3f, 0.5f, 0.8f));
        sortButton.onClick.AddListener(() =>
        {
            sortAscending = !sortAscending;
            UpdateSortButtonLabel();
            ApplyFilters();
        });
        sortButtonText = sortButton.GetComponentInChildren<Text>();
        UpdateSortButtonLabel();

        organicButton = CreatePillButton("Orgánico", filterRect, new Color(0.2f, 0.65f, 0.45f));
        organicButton.onClick.AddListener(() => ToggleCategory("ORGANICO"));

        inorganicButton = CreatePillButton("Inorgánico", filterRect, new Color(0.3f, 0.5f, 0.8f));
        inorganicButton.onClick.AddListener(() => ToggleCategory("INORGANICO"));

        materialButton = CreatePillButton(DefaultMaterialLabel, filterRect, new Color(0.8f, 0.4f, 0.65f));
        materialButton.onClick.AddListener(ToggleMaterialPanel);
        materialButtonText = materialButton.GetComponentInChildren<Text>();

        SpreadFilterButtonsEvenly();
    }

    private void SpreadFilterButtonsEvenly()
    {
        var buttons = new[] { sortButton, organicButton, inorganicButton, materialButton };
        foreach (var b in buttons)
        {
            if (!b) continue;
            var le = b.GetComponent<LayoutElement>();
            if (!le) le = b.gameObject.AddComponent<LayoutElement>();
            le.minWidth = 0f;
            le.preferredWidth = -1f;
            le.flexibleWidth = 1f;
        }
    }

    private void BuildMaterialPanel()
    {
        materialOverlay = new GameObject("MaterialOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
        var overlayRect = materialOverlay.GetComponent<RectTransform>();
        overlayRect.SetParent(root, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        var overlayImage = materialOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.5f);

        var overlayButton = materialOverlay.GetComponent<Button>();
        overlayButton.transition = Selectable.Transition.None;
        overlayButton.targetGraphic = overlayImage;
        overlayButton.onClick.AddListener(HideMaterialOverlay);

        materialPanel = new GameObject("MaterialPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
        materialPanel.SetParent(overlayRect, false);
        materialPanel.anchorMin = new Vector2(0.5f, 0.5f);
        materialPanel.anchorMax = new Vector2(0.5f, 0.5f);
        materialPanel.pivot = new Vector2(0.5f, 0.5f);
        materialPanel.sizeDelta = new Vector2(550f, 750f);
        var panelImage = materialPanel.GetComponent<Image>();
        panelImage.color = Color.white;

        var panelLayout = materialPanel.GetComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(30, 30, 30, 30);
        panelLayout.spacing = 20f;
        panelLayout.childAlignment = TextAnchor.UpperCenter;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childControlHeight = true;
        panelLayout.childForceExpandHeight = false;

        var title = CreateTextElement("Title", materialPanel, "Seleccionar Material", 34, FontStyle.Bold);
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.2f, 0.2f, 0.3f);

        var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
        scrollRectTransform.SetParent(materialPanel, false);
        var scrollImage = scrollGO.GetComponent<Image>();
        scrollImage.color = new Color(0.97f, 0.97f, 0.98f);
        var scrollLayout = scrollGO.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;

        var materialScroll = scrollGO.GetComponent<ScrollRect>();
        materialScroll.horizontal = false;
        materialScroll.vertical = true;
        materialScroll.movementType = ScrollRect.MovementType.Elastic;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask));
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.SetParent(scrollRectTransform, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(5f, 5f);
        viewportRect.offsetMax = new Vector2(-5f, -5f);

        materialPanelContent = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        materialPanelContent.SetParent(viewportRect, false);
        materialPanelContent.anchorMin = new Vector2(0f, 1f);
        materialPanelContent.anchorMax = new Vector2(1f, 1f);
        materialPanelContent.pivot = new Vector2(0.5f, 1f);
        materialPanelContent.anchoredPosition = Vector2.zero;

        var verticalLayout = materialPanelContent.GetComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.spacing = 10f;
        verticalLayout.childAlignment = TextAnchor.UpperCenter;
        verticalLayout.childControlWidth = true;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childControlHeight = true;
        verticalLayout.childForceExpandHeight = false;

        var fitter = materialPanelContent.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        materialScroll.content = materialPanelContent;
        materialScroll.viewport = viewportRect;

        var closeButton = CreatePillButton("Cerrar", materialPanel, new Color(0.85f, 0.85f, 0.87f));
        var closeLE = closeButton.GetComponent<LayoutElement>() ?? closeButton.gameObject.AddComponent<LayoutElement>();
        closeLE.preferredWidth = 260f;
        closeLE.minHeight = 52f;
        closeButton.onClick.AddListener(HideMaterialOverlay);

        materialOverlay.SetActive(false);
    }

    private void BuildScrollView()
    {
        var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
        scrollRectTransform.SetParent(root, false);
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);

        float topOffset = ComputeTopOffset() + 20f;
        scrollRectTransform.offsetMin = new Vector2(25f, 25f);
        scrollRectTransform.offsetMax = new Vector2(-25f, -topOffset);

        scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask));
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.SetParent(scrollRectTransform, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        content.SetParent(viewportRect, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);

        var grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(260f, 380f);
        grid.spacing = new Vector2(18f, 22f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = content;
        scrollRect.viewport = viewportRect;
    }

    private float ComputeTopOffset()
    {
        float h = headerRect != null ? headerRect.sizeDelta.y : 0f;
        float s = searchBarRect != null ? searchBarRect.sizeDelta.y : 0f;
        float f = filterRect != null ? filterRect.sizeDelta.y : 0f;
        return h + s + f + 120f + 210f - h + 40f;
    }

    private void BuildDetailsOverlay()
    {
        detailsOverlay = new GameObject("DetailsOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
        var overlayRect = detailsOverlay.GetComponent<RectTransform>();
        overlayRect.SetParent(root, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        var overlayImage = detailsOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.65f);

        var overlayButton = detailsOverlay.GetComponent<Button>();
        overlayButton.transition = Selectable.Transition.None;
        overlayButton.targetGraphic = overlayImage;
        overlayButton.onClick.AddListener(HideDetails);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        detailsPanelRect = panel.GetComponent<RectTransform>();
        detailsPanelRect.SetParent(overlayRect, false);
        detailsPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        detailsPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        detailsPanelRect.pivot = new Vector2(0.5f, 0.5f);
        detailsPanelRect.sizeDelta = new Vector2(620f, 920f);
        var panelImage = panel.GetComponent<Image>();
        panelImage.color = Color.white;

        var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
        scrollRectTransform.SetParent(detailsPanelRect, false);
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(25, 25);
        scrollRectTransform.offsetMax = new Vector2(-25, -25);

        detailsScroll = scrollGO.GetComponent<ScrollRect>();
        detailsScroll.horizontal = false;
        detailsScroll.vertical = true;
        detailsScroll.movementType = ScrollRect.MovementType.Elastic;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask));
        var viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.SetParent(scrollRectTransform, false);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        detailsContentRect = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        detailsContentRect.SetParent(viewportRect, false);
        detailsContentRect.anchorMin = new Vector2(0f, 1f);
        detailsContentRect.anchorMax = new Vector2(1f, 1f);
        detailsContentRect.pivot = new Vector2(0.5f, 1f);
        detailsContentRect.anchoredPosition = Vector2.zero;

        var contentLayout = detailsContentRect.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(24, 24, 20, 20); // más margen lateral
        contentLayout.spacing = 16f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandHeight = false;

        var fitter = detailsContentRect.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        detailsScroll.content = detailsContentRect;
        detailsScroll.viewport = viewportRect;

        detailsHeaderText = CreateTextElement("Header", detailsContentRect, "Ficha técnica", 38, FontStyle.Bold, true);
        detailsHeaderText.alignment = TextAnchor.MiddleCenter;
        detailsHeaderText.color = new Color(0.2f, 0.2f, 0.3f);

        detailsNameText = CreateTextElement("Name", detailsContentRect, string.Empty, 32, FontStyle.Bold, true);
        detailsNameText.alignment = TextAnchor.MiddleCenter;
        detailsNameText.color = Color.black;

        detailsCategoryText = CreateTextElement("Category", detailsContentRect, string.Empty, 26, FontStyle.Bold, true);
        detailsCategoryText.alignment = TextAnchor.MiddleCenter;
        detailsCategoryText.color = Color.black;

        var imageContainer = new GameObject("Image", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter), typeof(LayoutElement));
        var imageRect = imageContainer.GetComponent<RectTransform>();
        imageRect.SetParent(detailsContentRect, false);
        var aspect = imageContainer.GetComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
        aspect.aspectRatio = 1.33f;
        detailsImage = imageContainer.GetComponent<Image>();
        detailsImage.color = new Color(0.9f, 0.9f, 0.9f);
        detailsImage.preserveAspect = true;

        detailsMaterialText = CreateTextElement("Material", detailsContentRect, string.Empty, 24, FontStyle.Normal, true);
        detailsMaterialText.color = Color.black;
        detailsMaterialText.alignment = TextAnchor.UpperLeft;

        detailsDisposalText = CreateTextElement("Disposal", detailsContentRect, string.Empty, 24, FontStyle.Italic, true);
        detailsDisposalText.color = new Color(0.3f, 0.3f, 0.3f);
        detailsDisposalText.alignment = TextAnchor.UpperLeft;

        // Bloque con padding propio para la descripción (márgenes extra)
        var descBlock = new GameObject("DescriptionBlock", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
        descBlock.SetParent(detailsContentRect, false);
        var descVL = descBlock.GetComponent<VerticalLayoutGroup>();
        descVL.padding = new RectOffset(16, 16, 0, 0); // margen interno adicional sólo para la descripción
        descVL.spacing = 6f;
        descVL.childAlignment = TextAnchor.UpperLeft;
        descVL.childControlWidth = true;
        descVL.childForceExpandWidth = true;
        descVL.childControlHeight = true;
        descVL.childForceExpandHeight = false;

        detailsDescriptionText = CreateTextElement("Description", descBlock, string.Empty, 24, FontStyle.Normal, true);
        detailsDescriptionText.color = Color.black;
        detailsDescriptionText.alignment = TextAnchor.UpperLeft;

        // Botón Cerrar tipo “pill” pequeño y centrado
        var closeButton = CreatePillButton("Cerrar", detailsContentRect, new Color(0.3f, 0.5f, 0.8f));
        var closeLE = closeButton.GetComponent<LayoutElement>() ?? closeButton.gameObject.AddComponent<LayoutElement>();
        closeLE.preferredWidth = 200f;
        closeLE.minHeight = 46f;
        var closeRt = closeButton.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.5f, 0.5f);
        closeRt.anchorMax = new Vector2(0.5f, 0.5f);
        closeRt.pivot = new Vector2(0.5f, 0.5f); // centra el botón con ancho fijo
        var closeText = closeButton.GetComponentInChildren<Text>();
        if (closeText != null) closeText.color = Color.white;
        closeButton.onClick.AddListener(HideDetails);

        detailsOverlay.SetActive(false);
    }

    private void ToggleCategory(string category)
    {
        if (string.Equals(currentCategoryFilter, category, StringComparison.OrdinalIgnoreCase))
            currentCategoryFilter = null;
        else
            currentCategoryFilter = category;

        if (currentCategoryFilter == null)
        {
            currentMaterialFilter = null;
            materialButtonText.text = DefaultMaterialLabel;
            HideMaterialOverlay();
        }
        else
        {
            materialButtonText.text = string.IsNullOrEmpty(currentMaterialFilter) ? DefaultMaterialLabel : currentMaterialFilter;
            UpdateMaterialOptions();
        }

        UpdateCategoryButtonVisuals();
        ApplyFilters();
    }

    private void ToggleMaterialPanel()
    {
        if (materialOverlay == null) return;

        if (!materialOverlay.activeSelf)
        {
            if (string.IsNullOrEmpty(currentCategoryFilter))
            {
                Debug.Log("Selecciona primero una categoría (Orgánico o Inorgánico)");
                return;
            }
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
        if (button == null) return;

        var colors = button.colors;
        baseButtonColors.TryGetValue(button, out var baseColor);

        Color activeColor = baseColor;
        Color inactiveColor = Color.Lerp(baseColor, Color.white, 0.5f);

        colors.normalColor = active ? activeColor : inactiveColor;
        colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(colors.normalColor, Color.black, 0.15f);
        button.colors = colors;

        var text = button.GetComponentInChildren<Text>();
        if (text != null) text.color = active ? Color.white : new Color(0.4f, 0.4f, 0.4f);
    }

    private void UpdateSortButtonLabel()
    {
        if (sortButtonText != null) sortButtonText.text = sortAscending ? "A-Z" : "Z-A";
    }

    private void UpdateMaterialOptions()
    {
        foreach (var button in materialOptionButtons) if (button != null) Destroy(button.gameObject);
        materialOptionButtons.Clear();

        if (string.IsNullOrEmpty(currentCategoryFilter)) return;

        var materials = catalog.residuos
            .Where(r => string.Equals(r.categoria, currentCategoryFilter, StringComparison.OrdinalIgnoreCase))
            .Select(r => r.material)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m)
            .ToList();

        CreateMaterialOption("Todos los materiales", null);
        foreach (var material in materials) CreateMaterialOption(material, material);
    }

    private void CreateMaterialOption(string label, string value)
    {
        var optionButton = CreatePillButton(label, materialPanelContent, new Color(0.92f, 0.92f, 0.93f));
        var layout = optionButton.GetComponent<LayoutElement>();
        layout.minHeight = 48f;
        layout.preferredHeight = 52f;

        bool isSelected = string.IsNullOrEmpty(value)
            ? string.IsNullOrEmpty(currentMaterialFilter)
            : string.Equals(currentMaterialFilter, value, StringComparison.OrdinalIgnoreCase);

        var buttonColors = optionButton.colors;
        var txt = optionButton.GetComponentInChildren<Text>();
        if (isSelected)
        {
            buttonColors.normalColor = new Color(0.3f, 0.5f, 0.8f);
            optionButton.colors = buttonColors;
            if (txt) txt.color = Color.white;
        }
        else
        {
            if (txt) txt.color = new Color(0.2f, 0.2f, 0.2f);
        }

        optionButton.onClick.AddListener(() =>
        {
            currentMaterialFilter = value;
            materialButtonText.text = string.IsNullOrEmpty(value) ? DefaultMaterialLabel : value;
            HideMaterialOverlay();
            ApplyFilters();
        });
        materialOptionButtons.Add(optionButton);
    }

    private void ApplyFilters()
    {
        if (catalog == null || catalog.residuos == null) return;

        var term = ResiduoStringUtility.Normalize(searchField != null ? searchField.text : string.Empty);

        IEnumerable<Residuo> query = catalog.residuos;
        if (!string.IsNullOrEmpty(currentCategoryFilter))
            query = query.Where(r => string.Equals(r.categoria, currentCategoryFilter, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(currentMaterialFilter))
            query = query.Where(r => string.Equals(r.material, currentMaterialFilter, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(term))
            query = query.Where(r => r.AllSearchTokens().Any(token => token.Contains(term)));

        currentResults.Clear();
        currentResults.AddRange(query);
        currentResults.Sort((a, b) => string.Compare(a.nombre, b.nombre, StringComparison.OrdinalIgnoreCase));
        if (!sortAscending) currentResults.Reverse();

        RenderResults();
        ApplyResponsiveLayout();
    }

    private void RenderResults()
    {
        for (int i = 0; i < currentResults.Count; i++)
        {
            ResiduoCard card;
            if (i < cardPool.Count) card = cardPool[i];
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
            var isOrganic = string.Equals(residuo.categoria, "ORGANICO", StringComparison.OrdinalIgnoreCase);
            card.categoryText.color = isOrganic ? new Color(0.2f, 0.6f, 0.4f) : new Color(0.28f, 0.45f, 0.75f);
            card.categoryText.gameObject.SetActive(true);

            card.button.onClick.RemoveAllListeners();
            var capturedCard = card;
            card.button.onClick.AddListener(() => ShowDetails(capturedCard));
            card.thumbnail.sprite = null;
            card.thumbnail.color = new Color(0.85f, 0.85f, 0.85f);
            card.imageUrl = residuo.img;
            StartCoroutine(LoadImageForCard(card));
        }

        for (int i = 0; i < cardPool.Count; i++)
        {
            if (i >= currentResults.Count) cardPool[i].root.SetActive(false);
        }
    }

    private IEnumerator LoadImageForCard(ResiduoCard card)
    {
        if (string.IsNullOrWhiteSpace(card.imageUrl)) yield break;

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
        var cardObject = new GameObject("ResiduoCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup));
        var rect = cardObject.GetComponent<RectTransform>();

        var background = cardObject.GetComponent<Image>();
        background.color = new Color(0.96f, 0.96f, 0.98f);

        var button = cardObject.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = background.color;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.95f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.9f);
        button.colors = colors;

        var layout = cardObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

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

        var textContainer = new GameObject("TextContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        var textContainerRect = textContainer.GetComponent<RectTransform>();
        textContainerRect.SetParent(cardObject.transform, false);
        var textContainerLayout = textContainer.GetComponent<VerticalLayoutGroup>();
        textContainerLayout.padding = new RectOffset(12, 12, 8, 12);
        textContainerLayout.spacing = 6f;
        textContainerLayout.childAlignment = TextAnchor.MiddleCenter;
        textContainerLayout.childControlWidth = true;
        textContainerLayout.childForceExpandWidth = true;
        textContainerLayout.childControlHeight = true;
        textContainerLayout.childForceExpandHeight = false;
        var textContainerLayoutElement = textContainer.GetComponent<LayoutElement>();
        textContainerLayoutElement.preferredHeight = 150f;

        var nameText = CreateTextElement("Name", textContainerRect, string.Empty, 22, FontStyle.Bold);
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = Color.black;
        nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameText.verticalOverflow = VerticalWrapMode.Overflow;
        nameText.resizeTextForBestFit = true;
        nameText.resizeTextMinSize = 14;
        nameText.resizeTextMaxSize = 22;
        var nameLE = nameText.gameObject.AddComponent<LayoutElement>();
        nameLE.minHeight = 40f;
        nameLE.preferredHeight = 56f;

        var categoryText = CreateTextElement("Category", textContainerRect, string.Empty, 18, FontStyle.Normal);
        categoryText.alignment = TextAnchor.MiddleCenter;
        categoryText.color = Color.black;
        categoryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        categoryText.verticalOverflow = VerticalWrapMode.Overflow;
        categoryText.resizeTextForBestFit = true;
        categoryText.resizeTextMinSize = 14;
        categoryText.resizeTextMaxSize = 18;
        var catLE = categoryText.gameObject.AddComponent<LayoutElement>();
        catLE.minHeight = 28f;
        catLE.preferredHeight = 40f;

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
            Debug.LogWarning("Card o datos nulos en ShowDetails");
            return;
        }

        detailsOverlay.SetActive(true);

        detailsHeaderText.text = "Ficha técnica";
        detailsNameText.text = card.data.nombre;
        detailsCategoryText.text = FormatCategory(card.data);
        var isOrganic = string.Equals(card.data.categoria, "ORGANICO", StringComparison.OrdinalIgnoreCase);
        detailsCategoryText.color = isOrganic ? new Color(0.2f, 0.6f, 0.4f) : new Color(0.3f, 0.5f, 0.75f);

        var materialText = $"<b>Material:</b> {card.data.material}";
        if (!string.IsNullOrWhiteSpace(card.data.submaterial))
            materialText += $"\n<b>Submaterial:</b> {card.data.submaterial}";
        detailsMaterialText.text = materialText;

        var disposalHint = GetDisposalHint(card.data);
        detailsDisposalText.text = $"<b>Disposición:</b>\n{disposalHint}";

        var description = string.IsNullOrWhiteSpace(card.data.descripcion) ? "Sin descripción disponible." : card.data.descripcion;
        detailsDescriptionText.text = $"<b>Descripción:</b>\n{description}";

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

        ResizeDetailsToContent();
    }

    private void ResizeDetailsToContent()
    {
        if (detailsPanelRect == null || detailsContentRect == null || root == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(detailsContentRect);
        float contentPref = LayoutUtility.GetPreferredHeight(detailsContentRect);
        float scrollChrome = 50f;

        float maxH = Mathf.Min(root.rect.height * 0.9f, 1200f);
        float minH = 520f;
        float newH = Mathf.Clamp(contentPref + scrollChrome, minH, maxH);

        float maxW = Mathf.Min(root.rect.width * 0.9f, root.rect.width - 40f);
        float minW = 520f;
        float newW = Mathf.Clamp(root.rect.width * 0.9f, minW, maxW);

        detailsPanelRect.sizeDelta = new Vector2(newW, newH);

        if (detailsScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            detailsScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void HideDetails()
    {
        detailsOverlay.SetActive(false);
    }

    private void HideMaterialOverlay()
    {
        if (materialOverlay != null) materialOverlay.SetActive(false);
    }

    private string FormatCategory(Residuo residuo)
    {
        var categoria = string.Equals(residuo.categoria, "ORGANICO", StringComparison.OrdinalIgnoreCase) ? "Orgánico" : "Inorgánico";
        if (!string.IsNullOrWhiteSpace(residuo.subcategoria))
            return $"{categoria} · {residuo.subcategoria.ToUpperInvariant()}";
        return categoria;
    }

    private string GetDisposalHint(Residuo residuo)
    {
        if (residuo == null) return string.Empty;

        if (!string.IsNullOrWhiteSpace(residuo.subcategoria))
        {
            if (residuo.subcategoria.IndexOf("reciclable", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Depositar en contenedor de reciclables limpios.";
            if (residuo.subcategoria.IndexOf("compost", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Agregar a composta o contenedor orgánico.";
            if (residuo.subcategoria.IndexOf("peligroso", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Llevar a un centro de acopio para residuos peligrosos.";
        }

        return string.Equals(residuo.categoria, "ORGANICO", StringComparison.OrdinalIgnoreCase)
            ? "Depositar en contenedor de orgánicos."
            : "Depositar en contenedor gris o de inorgánicos.";
    }

    // ----------------- Responsive helpers -----------------

    private void ApplyResponsiveLayout()
    {
        SpreadFilterButtonsEvenly();
        UpdateGridColumnsAndCellSize();
        ResizeModal(materialPanel, 0.86f, 0.86f, 520f, 1100f);
        ResizeModal(detailsPanelRect, 0.9f, 0.9f, 520f, 1200f);
    }

    private void UpdateGridColumnsAndCellSize()
    {
        if (scrollRect == null || scrollRect.viewport == null || content == null) return;

        var grid = content.GetComponent<GridLayoutGroup>();
        if (grid == null) return;

        float viewportWidth = scrollRect.viewport.rect.width;
        if (viewportWidth <= 0f) return;

        int columns = 3;
        if (viewportWidth < 900f) columns = 2;
        if (viewportWidth < 520f) columns = 1;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;

        float spacingX = grid.spacing.x;
        float cellWidth = Mathf.Floor((viewportWidth - (columns - 1) * spacingX) / columns);
        float cellHeight = 380f;
        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }

    private void ResizeModal(RectTransform panel, float widthPercent, float heightPercent, float minWidth, float maxHeight)
    {
        if (panel == null || root == null) return;

        float rootW = root.rect.width;
        float rootH = root.rect.height;
        if (rootW <= 0f || rootH <= 0f) return;

        float targetW = Mathf.Clamp(rootW * widthPercent, minWidth, rootW - 40f);
        float targetH = Mathf.Clamp(rootH * heightPercent, 520f, Mathf.Min(maxHeight, rootH - 40f));
        panel.sizeDelta = new Vector2(targetW, targetH);
    }

    // ----------------- Utils UI -----------------

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
        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        button.colors = colors;
        return rect;
    }

    private Button CreatePillButton(string text, RectTransform parent, Color backgroundColor)
    {
        var go = new GameObject(text + "Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        var image = go.GetComponent<Image>();
        image.color = backgroundColor;

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.2f);
        colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.2f);
        colors.disabledColor = Color.Lerp(backgroundColor, Color.white, 0.5f);
        button.colors = colors;

        var layoutElement = go.GetComponent<LayoutElement>();
        layoutElement.minHeight = 50f;
        layoutElement.preferredHeight = 50f;
        layoutElement.flexibleWidth = 1f;

        var label = CreateTextElement("Label", rect, text, 20, FontStyle.Bold);
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Truncate;

        baseButtonColors[button] = backgroundColor;

        return button;
    }

    private Text CreateTextElement(string name, Transform parent, string value, int fontSize, FontStyle fontStyle, bool supportRichText = false)
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
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.supportRichText = supportRichText;
        text.lineSpacing = 1.15f;

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