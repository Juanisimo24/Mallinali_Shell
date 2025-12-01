using UnityEngine;

public class RareItem : MonoBehaviour
{
    [Header("Refs")]      // Asigna la referencia (o se buscará en escena)
    [SerializeField] private Collider2D triggerCollider;      // Deja vacío si usarás el propio collider
    [SerializeField] private AudioClip pickupSFX;
    [SerializeField] private GameObject pickupVFX;

    [Header("Dialogo Sagrado")]
    [TextArea(3,8)] 
    public string[] sacredLines = new string[]
    {
        "«Oh caparazón de Malinalli, domador de tempestades y guardián del silencio.",
        "Que tu llamado resuene como trueno en el ombligo del mundo;",
        "que los enemigos vuelvan su rostro y su furia a ti,",
        "pues en tu escudo duerme el rugido de los soles antiguos.»"
    };

    [SerializeField] private float autoHideDelay = 0f; // 0 = espera input del jugador

    private bool _consumed;

    private void Reset()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null) triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed) return;
        if (other.CompareTag("Player")||other.CompareTag("Tortuga")) Unlock(); // Asegúrate de que la Tortuga/Jugador tenga tag "Player"


    }

    private void Unlock()
    {
        _consumed = true;

        

        if (pickupSFX) AudioSource.PlayClipAtPoint(pickupSFX, transform.position);
        if (pickupVFX) Instantiate(pickupVFX, transform.position, Quaternion.identity);

        // Mostrar diálogo sagrado
        if (SacredDialogueUI.Instance != null && sacredLines != null && sacredLines.Length > 0)
        {
            SacredDialogueUI.Instance.Show(sacredLines, autoHideDelay);
        }

        if (triggerCollider == null) triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider) triggerCollider.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        // Opcional: destruir luego de un tiempo (por si hay VFX)
        Destroy(gameObject, 2f);
    }


}
