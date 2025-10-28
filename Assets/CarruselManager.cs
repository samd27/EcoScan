// CarruselManager.cs actualizado para mostrar solo 1 modelo a la vez
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CarruselManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject modeloContenedor; // Empty para colocar el modelo 3D en el centro

    [Header("Botones de navegación")]
    public Button btnSiguiente;
    public Button btnAnterior;

    [Header("Configuración")]
    public string categoriaSeleccionada = "Organico";
    public string rutaBase = "Models"; // Dentro de Resources

    private List<GameObject> modelosPrecargados = new List<GameObject>();
    private int indiceActual = 0;
    private GameObject modeloInstanciado;

    void Start()
    {
        CargarModelosCategoria(categoriaSeleccionada);
        btnSiguiente.onClick.AddListener(() => CambiarModelo(1));
        btnAnterior.onClick.AddListener(() => CambiarModelo(-1));
    }

    public void CargarModelosCategoria(string categoria)
    {
        categoriaSeleccionada = categoria;
        modelosPrecargados.Clear();
        indiceActual = 0;

        string ruta = Path.Combine(rutaBase, categoria);
        GameObject[] modelos = Resources.LoadAll<GameObject>(ruta);

        foreach (GameObject modelo in modelos)
        {
            modelosPrecargados.Add(modelo);
        }

        MostrarModeloActual();
    }

    void MostrarModeloActual()
    {
        if (modeloInstanciado != null)
            Destroy(modeloInstanciado);

        if (modelosPrecargados.Count == 0) return;

        GameObject prefab = modelosPrecargados[indiceActual];
        modeloInstanciado = Instantiate(prefab, modeloContenedor.transform);

        modeloInstanciado.transform.localPosition = Vector3.zero;
        modeloInstanciado.transform.localRotation = Quaternion.identity;
        modeloInstanciado.transform.localScale = Vector3.one;
    }

    void CambiarModelo(int direccion)
    {
        if (modelosPrecargados.Count == 0) return;

        indiceActual += direccion;
        if (indiceActual < 0) indiceActual = modelosPrecargados.Count - 1;
        if (indiceActual >= modelosPrecargados.Count) indiceActual = 0;

        MostrarModeloActual();
    }
    public void CambiarCategoria(string nuevaCategoria)
    {
        categoriaSeleccionada = nuevaCategoria;
        CargarModelosCategoria(categoriaSeleccionada);
    }

}
