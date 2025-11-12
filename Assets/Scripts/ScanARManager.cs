using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections; // ¡NECESARIO para la Corutina de animación!
using TMPro; 

public class ScanARManager : MonoBehaviour
{
    [Header("Lógica de Escaneo")]
    public float duracionTimeout = 10f; 
    private bool targetEncontrado = false; 

    [Header("UI - Barra Superior")]
    public Button backButton; 

    // --- ¡MODIFICADO! ---
    [Header("UI - Pop-up General")]
    public CanvasGroup popupFondoCanvasGroup; // Cambiamos GameObject por CanvasGroup
    private Coroutine activeFadeCoroutine; // Para controlar la animación

    [Header("UI - Textos del Pop-up")]
    public TextMeshProUGUI textoTitulo;
    public TextMeshProUGUI textoDescripcion;

    [Header("UI - Contenedores de Botones")]
    public GameObject contenedorExito; 
    public GameObject contenedorFallo; 

    [Header("UI - Botones del Pop-up")]
    public Button botonAceptar;
    public Button botonCerrar;

    
    void Start()
    {
        // 1. Ocultar pop-up al inicio (¡NUEVA FORMA!)
        popupFondoCanvasGroup.alpha = 0f;
        popupFondoCanvasGroup.interactable = false;
        popupFondoCanvasGroup.blocksRaycasts = false;

        // 2. Asignar funciones a los botones
        backButton.onClick.AddListener(IrAMainMenu);
        botonAceptar.onClick.AddListener(CerrarPopup);
        botonCerrar.onClick.AddListener(CerrarPopup);

        // --- ¡NUEVO! Escuchar clics en el link del texto ---

        // 3. Iniciar el temporizador
        StartCoroutine(TemporizadorDeFallo());
    }

    // --- LÓGICA DEL TEMPORIZADOR Y POP-UPS ---

    void MostrarPopupExito(string nombreResiduo, string clasificacion, string material, string desechar, string adicional = "")
    {
        if (targetEncontrado) return;
        targetEncontrado = true;
        StopAllCoroutines();

        // ¡MODIFICADO! Llama a la animación
        ShowPopup(true); 
        contenedorExito.SetActive(true);
        contenedorFallo.SetActive(false);

        textoTitulo.text = $"Residuo: {nombreResiduo}"; 

        string descripcionCompleta = $"Tipo: {clasificacion}\n" +
                                     $"Material: {material}\n" +
                                     $"Desechar: {desechar}";
        
        if (!string.IsNullOrEmpty(adicional))
        {
            descripcionCompleta += $"\nRecomendación: {adicional}"; 
        }
        
        textoDescripcion.text = descripcionCompleta;
    }

    // Corrutina para el pop-up de fallo (timeout)
    IEnumerator TemporizadorDeFallo()
    {
        yield return new WaitForSeconds(duracionTimeout);
        if (!targetEncontrado)
        {
            targetEncontrado = true; 
            
            // ¡MODIFICADO! Llama a la animación
            ShowPopup(true);
            contenedorExito.SetActive(false);
            contenedorFallo.SetActive(true);

            textoTitulo.text = "Residuo no identificado";

            // --- ¡TEXTO MODIFICADO CON LINK Y ESTILO! ---
            textoDescripcion.text = "Se sugiere buscar un residuo similar en el <link=\"listado\"><u><i><b>Listado de residuos</b></i></u></link>.";
        }
    }

    // --- FUNCIONES DE LOS BOTONES ---

    void CerrarPopup()
    {
        // ¡MODIFICADO! Llama a la animación
        ShowPopup(false);
        
        // Reinicia el estado después de que la animación termine (o casi)
        // Para ser más precisos, esto debería ir en la corutina
        // pero por simplicidad, lo dejamos aquí.
        targetEncontrado = false;
        StartCoroutine(TemporizadorDeFallo());
    }

    void IrAMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void IrAListado()
    {
        SceneManager.LoadScene("ListadoScene");
    }

    // --- ¡NUEVAS FUNCIONES! (Para el Link y la Animación) ---

    // Esta función se llama cuando se hace clic en el <link="listado">

    // Función pública que llaman los botones "Filtros" y "Cerrar"
    public void ShowPopup(bool show)
    {
        if (activeFadeCoroutine != null)
        {
            StopCoroutine(activeFadeCoroutine);
        }
        float duration = 0.2f; // Duración en segundos
        activeFadeCoroutine = StartCoroutine(FadePopup(show, duration));
    }

    // La Corutina que hace la animación
    private IEnumerator FadePopup(bool show, float duration)
    {
        float startTime = Time.time;
        float startAlpha = popupFondoCanvasGroup.alpha;
        float targetAlpha = show ? 1.0f : 0.0f;

        if (show)
        {
            popupFondoCanvasGroup.interactable = true;
            popupFondoCanvasGroup.blocksRaycasts = true;
        }

        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            float newAlpha = Mathf.SmoothStep(startAlpha, targetAlpha, t);
            popupFondoCanvasGroup.alpha = newAlpha;
            yield return null;
        }

        popupFondoCanvasGroup.alpha = targetAlpha;

        if (!show)
        {
            popupFondoCanvasGroup.interactable = false;
            popupFondoCanvasGroup.blocksRaycasts = false;
        }

        activeFadeCoroutine = null;
    }

    // ------------------------------------------------------------------
    // --- "Base de Datos" de ResiduOS (¡TUS FUNCIONES!) ---
    // ------------------------------------------------------------------

    public void EncontradoCocaCola()
    {
        MostrarPopupExito(
            "Coca Cola (Lata)", // nombreResiduo
            "Inorgánico", // clasificacion
            "Aluminio", // material
            "Contenedor de Inorgánicos (Reciclables)", // desechar
            "Enjuagar y aplastar la lata." // adicional (ahora se mostrará como Recomendación)
        );
    }

    public void EncontradoJumex()
    {
        MostrarPopupExito(
            "Jumex (Tetra Pak)",
            "Inorgánico",
            "Tetra Pak (Cartón, plástico y aluminio)",
            "Contenedor de Inorgánicos",
            "Aplastar el envase."
        );
    }

    public void EncontradoSabritas()
    {
        MostrarPopupExito(
            "Sabritas",
            "Inorgánico",
            "Plástico metalizado (BOPP)",
            "Contenedor de Inorgánicos (No reciclable)",
            "" // Sin adicional
        );
    }

    public void EncontradoYogurt()
    {
        MostrarPopupExito(
            "Yogurt (Bote)",
            "Inorgánico",
            "Plástico (PET o PP)",
            "Contenedor de Inorgánicos (Reciclables)",
            "Enjuagar bien el envase."
        );
    }

    public void EncontradoMagnum()
    {
        MostrarPopupExito(
            "Magnum (Envoltura)",
            "Inorgánico",
            "Plástico metalizado",
            "Contenedor de Inorgánicos (No reciclable)",
            "" // Sin adicional
        );
    }

    // --- ¡NUEVA FUNCIÓN AÑADIDA! ---
    public void EncontradoGalletas()
    {
        MostrarPopupExito(
            "Emperador (Envoltura)",
            "Inorgánico",
            "Plástico metalizado",
            "Contenedor de Inorgánicos (No reciclable)",
            "Desechar sin ningun alimento dentro." // adicional
        );
    }

 public void EncontradoHersheysDark()
    {
        MostrarPopupExito(
            "Hershey's Dark (envoltura)",
            "Inorgánico",
            "Plástico laminado metalizado (BOPP/PP con aluminio)",
            "Inorgánicos NO reciclables",
            "Compacta la envoltura y evita restos de chocolate."
        );
    }

    public void EncontradoSidralMundet()
    {
        MostrarPopupExito(
            "Sidral Mundet (botella)",
            "Inorgánico",
            "PET (#1) + tapa PP (#5) + etiqueta BOPP",
            "Inorgánicos RECICLABLES",
            "Separa tapa y etiqueta; enjuaga y compacta."
        );
    }

    public void EncontradoGalletaArcoiris()
    {
        MostrarPopupExito(
            "Arcoiris (envoltura)",
            "Inorgánico",
            "Plástico metalizado (BOPP)",
            "Inorgánicos NO reciclables",
            "Cierra la envoltura y deséchala limpia."
        );
    }

    public void EncontradoBarritasFresa()
    {
        MostrarPopupExito(
            "Barritas Fresa (envoltura)",
            "Inorgánico",
            "Plástico laminado/metalizado",
            "Inorgánicos NO reciclables",
            "Sacude migas antes de tirar para evitar lixiviados."
        );
    }

    public void EncontradoBubuLubu()
    {
        MostrarPopupExito(
            "Bubulubu (envoltura)",
            "Inorgánico",
            "Plástico metalizado (flow-pack)",
            "Inorgánicos NO reciclables",
            "Compacta la envoltura para reducir volumen."
        );
    }

    public void EncontradoTridentXtraCare()
    {
        MostrarPopupExito(
            "Trident XtraCare (empaque)",
            "Inorgánico (mixto)",
            "Caja de cartón + envolturas interiores plastificadas/metalizadas",
            "Cartón → Inorgánicos RECICLABLES | Envolturas → Inorgánicos NO reciclables",
            "Separa componentes: recicla la cajita; envolturas al no reciclable."
        );
    }

    // -------------------------------------------------
    // NUEVOS RESIDUOS agregados ahora (nombres sin ml ni g)
    // -------------------------------------------------
    public void EncontradoMelatonina()
    {
        MostrarPopupExito(
            "Melatonina (caja y blíster)",
            "Especial",
            "Caja de cartón + blíster aluminio/PVC",
            "Cartón → Inorgánicos RECICLABLES | Blíster/medicamento → ESPECIAL (llevar a contenedor de medicamentos en farmacia)",
            "No tires tabletas al drenaje ni a la basura común; usa puntos de acopio para fármacos."
        );
    }

    public void EncontradoMonsterEnergy()
    {
        MostrarPopupExito(
            "Monster Energy (lata)",
            "Inorgánico",
            "Aluminio",
            "Inorgánicos RECICLABLES",
            "Vacía, enjuaga y compacta ligeramente la lata. La anilla puede ir junto con la lata."
        );
    }

    public void EncontradoSabritasAdobadas()
    {
        MostrarPopupExito(
            "Sabritas Adobadas (bolsa)",
            "Inorgánico",
            "Plástico laminado metalizado",
            "Inorgánicos NO reciclables",
            "Cierra y compacta la bolsa; evita residuos de aceite."
        );
    }

    public void EncontradoPredatorEnergy()
    {
        MostrarPopupExito(
            "Predator Energy (lata)",
            "Inorgánico",
            "Aluminio",
            "Inorgánicos RECICLABLES",
            "Vacía, enjuaga y compacta ligeramente."
        );
    }

    public void EncontradoSantaClaraVainilla()
    {
        MostrarPopupExito(
            "Santa Clara Vainilla (tetrapak)",
            "Inorgánico",
            "Cartón + polietileno + aluminio",
            "Inorgánicos RECICLABLES (si hay acopio de tetrapak)",
            "Enjuaga, escurre y aplana el envase antes de depositarlo."
        );
    }

    public void EncontradoSantaClaraFresa()
    {
        MostrarPopupExito(
            "Santa Clara Fresa (tetrapak)",
            "Inorgánico",
            "Cartón + polietileno + aluminio",
            "Inorgánicos RECICLABLES (si hay acopio de tetrapak)",
            "Enjuaga, escurre y aplana el envase antes de depositarlo."
        );
    }

    public void EncontradoSantaClaraCapuccino()
    {
        MostrarPopupExito(
            "Santa Clara Capuccino (tetrapak)",
            "Inorgánico",
            "Cartón + polietileno + aluminio",
            "Inorgánicos RECICLABLES (si hay acopio de tetrapak)",
            "Enjuaga, escurre y aplana el envase antes de depositarlo."
        );
    }
}