using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SimpleNPC_Dialogue : MonoBehaviour
{
    [Header("Data & UI")]
    public Dialogue_1 dialogueData;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    
    public Image portraitImage;

[Header("Eventos")] // <--- 2. NUEVA SECCIÓN
    [Tooltip("Arrastra aquí lo que quieras que pase al terminar de hablar")]
    public UnityEvent onDialogueEnded;
    
    // Internal references
    private PlayerController control;
    private int dialogueIndex;
    private bool isTyping, isDialogueActive;
private Coroutine autoProgressCoroutine;
    void Update()
    {
        // Listen for input only when dialogue is active
        if (isDialogueActive)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Fire1"))
            {
                NextLine();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Trigger automatically when player enters
        if (other.CompareTag("Player")) 
        {
            control = other.GetComponent<PlayerController>();
            if(control != null) 
            {
                control.canControl = false; // Freeze player
                // If using the updated PlayerController I gave you earlier, 
                // ensure it stops moving immediately:
                Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
                if(rb) rb.linearVelocity = Vector2.zero; 
            }
            
            StartDialogue();
        }
    }

     public void StartDialogueManually()
    {
        // 1. Buscar al jugador activo para congelarlo
        if (PlayerManagerDual.Instance != null)
        {
            GameObject activePlayer = PlayerManagerDual.Instance.GetActive();
            /*if (activePlayer != null)
            {
                control = activePlayer.GetComponent<PlayerController>();
                if (control)
                {
                    control.canControl = false;
                    // Frenar movimiento residual
                    var rb = activePlayer.GetComponent<Rigidbody2D>();
                    if (rb) rb.linearVelocity = Vector2.zero;
                }
            }*/
        }

        // 2. Iniciar la lógica normal
        isDialogueActive = true;
        
        // Aseguramos que el panel se active si estaba apagado
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        
        StartDialogue();
    }

    // Optional: Keep this if you want to trigger it via a button E instead of walking into it
    public void beInteracted()
    {
        if (dialogueData == null) return;

        if (!isDialogueActive)
        {
            StartDialogue();
        }
        else
        {
            NextLine();
        }
    }

    void StartDialogue()
    {
        isDialogueActive = true;
        dialogueIndex = 0;
        
        if (dialogueData.npcName != null) nameText.SetText(dialogueData.npcName);
        if (dialogueData.npcPortrait != null) portraitImage.sprite = dialogueData.npcPortrait;
        
        dialoguePanel.SetActive(true);
        StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        // CASO 1: El jugador quiere saltar la animación de escribir
        if (isTyping)
        {
            StopAllCoroutines(); // Detenemos tipeo y cualquier auto-avance pendiente
            
            // Mostramos el texto completo de golpe
            dialogueText.SetText(dialogueData.dialogueLine[dialogueIndex]);
            isTyping = false;

            // IMPORTANTE: Si esta línea tenía auto-progreso, debemos reactivar la espera
            // incluso si el jugador saltó la animación.
            TryTriggerAutoProgress();
        }
        // CASO 2: El texto ya se mostró completo y pasamos a la siguiente línea
        else 
        {
            // Detenemos cualquier auto-avance que estuviera corriendo para no saltar doble
            StopAllCoroutines(); 

            dialogueIndex++;

            if (dialogueIndex < dialogueData.dialogueLine.Length)
            {
                StartCoroutine(TypeLine());
            }
            else
            {
                EndDialogue();
            }
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.SetText("");

        // Lógica de retratos
        if (dialogueIndex < dialogueData.autoProgressLine.Length)
        {
            if (dialogueData.autoProgressLine[dialogueIndex])
                portraitImage.sprite = dialogueData.npcPortrait;
            else
                portraitImage.sprite = dialogueData.playerPortrait;
        }

        // Efecto de tipeo
        foreach (char letter in dialogueData.dialogueLine[dialogueIndex].ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueData.typingSpeed);
        }

        isTyping = false;

        // Una vez termina de escribir, intentamos activar el auto-avance
        TryTriggerAutoProgress();
    }

    // --- NUEVA FUNCIÓN PARA GESTIONAR EL AUTO-AVANCE ---
    void TryTriggerAutoProgress()
    {
        // Verificamos si existe el dato en el array y si es TRUE
        if (dialogueIndex < dialogueData.autoProgressLine.Length && dialogueData.autoProgressLine[dialogueIndex])
        {
            // Iniciamos la cuenta regresiva para pasar a la siguiente línea solo
            autoProgressCoroutine = StartCoroutine(AutoProgressTimer());
        }
    }

    IEnumerator AutoProgressTimer()
    {
        yield return new WaitForSeconds(dialogueData.autoProgressDelay);
        NextLine(); // Llamamos a NextLine automáticamente
    }

    public void EndDialogue()
    {
        if(control != null) control.canControl = true;
        
        StopAllCoroutines();
        isDialogueActive = false;
        dialogueText.SetText("");
        dialoguePanel.SetActive(false);
        
        onDialogueEnded?.Invoke();
    }
}