using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModeloController : MonoBehaviour
{
    public string categoria = "Organico";
    public Transform espacioModelo;
    public Button btnAnterior, btnSiguiente;
    public TMP_Text uiCategoria;
    public TMP_Text uiDescripcion;

    private List<GameObject> modelos = new List<GameObject>();
    private GameObject modeloActual;
    private int indiceActual = 0;

    void Start()
    {
        btnAnterior.onClick.AddListener(() => CambiarModelo(-1));
        btnSiguiente.onClick.AddListener(() => CambiarModelo(1));
    }

    // Métodos para botones de categoría
    public void OnBotonOrganico() => MostrarCategoria("Organico");
    public void OnBotonInorganico() => MostrarCategoria("Inorganico");
    public void OnBotonNoReciclable() => MostrarCategoria("NoReciclable");

    public void MostrarCategoria(string nuevaCategoria)
    {
        categoria = nuevaCategoria;
        uiCategoria.text = categoria;
        CargarModelos();
        indiceActual = 0;
        MostrarModeloActual();
    }

    void CargarModelos()
    {
        modelos.Clear();
        string ruta = "Models/" + categoria;
        GameObject[] cargados = Resources.LoadAll<GameObject>(ruta);

        foreach (GameObject go in cargados)
        {
            if (go == null) continue;

            if (categoria == "Organico")
            {
                if (go.name == "banana" || go.name == "appleuvw" || go.name == "pizza")
                    modelos.Add(go);
            }
            else if (categoria == "Inorganico")
            {
                if (go.name == "TirePile" || go.name == "lata" || go.name == "botella")
                    modelos.Add(go);
            }
            else if (categoria == "NoReciclable")
            {
                if (go.name == "lapiz" || go.name == "candy" || go.name == "ceramica")
                    modelos.Add(go);
            }
            // Puedes añadir más categorías aquí
        }

        if (modelos.Count == 0)
            Debug.LogWarning("⚠ No se encontraron prefabs válidos en: " + ruta);
    }

    void MostrarModeloActual()
    {
        if (modeloActual != null)
            Destroy(modeloActual);
        if (modelos.Count == 0) return;

        GameObject prefab = modelos[indiceActual];
        modeloActual = Instantiate(prefab, espacioModelo);

        // Nombre exacto del prefab para la descripción
        string nombre = prefab.name;
        switch (nombre)
        {
            case "pizza":
                uiDescripcion.text = "Pizza: Restos de comida orgánica compostable.";
                break;
            case "banana":
                uiDescripcion.text = "Banana: Cáscara biodegradable rica en nutrientes.";
                break;
            case "appleuvw":
                uiDescripcion.text = "Apple: Restos de manzana orgánica, compostable.";
                break;
            case "TirePile":
                uiDescripcion.text = "Llanta: Neumático de coche, reciclable en puntos especiales.";
                break;
            case "lata":
                uiDescripcion.text = "Lata: Metal que puede fundirse y reciclarse.";
                break;
            case "botella":
                uiDescripcion.text = "Botella: Plástico duro, reciclable en contenedor amarillo.";
                break;
            case "candy":
                uiDescripcion.text = "Dulces: Envoltura de dulces, galletas, chocolates, no reciclable.";
                break;
            case "lapiz":
                uiDescripcion.text = "Lápiz: Material de madera y grafito, no reciclable.";
                break;
            case "ceramica":
                uiDescripcion.text = "Cerámica: Material cerámico, no reciclable en contenedores comunes.";
                break;
            default:
                uiDescripcion.text = string.Empty;
                break;
        }
    }

    void CambiarModelo(int direccion)
    {
        if (modelos.Count == 0) return;
        indiceActual = (indiceActual + direccion + modelos.Count) % modelos.Count;
        MostrarModeloActual();
    }
}
