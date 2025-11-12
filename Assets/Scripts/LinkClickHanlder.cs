using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class LinkClickHandler : MonoBehaviour, IPointerClickHandler
{
    // Arrastra tu ScanARManager aquí en el Inspector
    public ScanARManager scanManager;

    public void OnPointerClick(PointerEventData eventData)
    {
        // Obtener el componente de texto de este mismo objeto
        TMP_Text pTextMeshPro = GetComponent<TMP_Text>();
        if (pTextMeshPro == null) return;

        // Comprobar si el clic fue en un link
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(pTextMeshPro, eventData.position, eventData.pressEventCamera);

        if (linkIndex != -1)
        {
            // ¡Se hizo clic en un link!
            TMP_LinkInfo linkInfo = pTextMeshPro.textInfo.linkInfo[linkIndex];

            // Comprobar el ID del link (el que pusimos en el script: <link="listado">)
            if (linkInfo.GetLinkID() == "listado")
            {
                // Llamar a la función pública en el manager
                if (scanManager != null)
                {
                    scanManager.IrAListado();
                }
                else
                {
                    Debug.LogError("ScanARManager no está asignado en LinkClickHandler");
                }
            }
        }
    }
}