using System.Collections;
using Pathfinding;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class NPC_Dialogue : MonoBehaviour
{
    public Dialogue_1 dialogueData;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;

    public GameObject turtle;  
    private PlayerController control;  
    public GameObject manager;  

private int dialogueIndex;
private bool isTyping,isDialogueActive;

void Update()
    {
        // Solo escuchamos teclas si el diálogo está activo
        if (isDialogueActive)
        {
            // Si presionas Espacio (o Fire1/Click izquierdo)
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Fire1"))
            {
                NextLine();
            }
        }
    }
    public void beInteracted()
    {
        if (dialogueData == null || !isDialogueActive)
            return;

        if (isDialogueActive)
        {
            NextLine();
        }
        else
        {
            StartDialogue();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
         if (other.CompareTag("Player")) 
        {
            control = other.GetComponent<PlayerController>();
            if(control != null) control.canControl = false;
            
            isDialogueActive = true;
            StartDialogue();
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
        // 1. Si está escribiendo, SALTAMOS al final
        if (isTyping)
        {
            StopAllCoroutines(); // Detiene el efecto de máquina de escribir
            
            // Muestra el texto completo inmediatamente
            dialogueText.SetText(dialogueData.dialogueLine[dialogueIndex]); 
            
            isTyping = false;
        }
        // 2. Si ya terminó de escribir, pasamos a la SIGUIENTE línea
        else 
        {
            // Incrementamos el índice
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

        // Lógica de retratos (asegúrate que tu array autoProgressLine tenga el tamaño correcto)
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

        // Auto-avance (Ojo: Si saltas el texto con Espacio, StopAllCoroutines cancelará esto,
        // lo cual es correcto: el jugador toma el control manual).
        // Nota: Tu lógica original comparaba Length > Index, lo cual siempre es true si el array existe.
        // Asumo que tienes una variable bool para saber si debe avanzar solo.
        /* 
        if (dialogueData.autoProgressLine.Length > dialogueIndex) // Esto parece peligroso si no es lo que buscas
        {
            yield return new WaitForSeconds(dialogueData.autoProgressDelay);
            NextLine();
        } 
        */
    }

    public void EndDialogue()
    {
        control.canControl = true;
        StopAllCoroutines();
        isDialogueActive = false;
        dialogueText.SetText("");
        dialoguePanel.SetActive(false);
        
        result();
        Destroy(gameObject);
    }

    public void result()
    {
        if (PlayerManagerDual.Instance != null)
        {
            PlayerManagerDual.Instance.UnlockTurtle();
            Debug.Log("Has encontrado a Malinalli. Pulsa TAB para cambiar.");
        }

        // Aseguramos que las referencias existan antes de usarlas
        if (manager != null) manager.SetActive(true);
        
        if (turtle != null)
        {
            var turtleFollow = turtle.GetComponent<Seeker>();
            var turtleGroundFollow = turtle.GetComponent<CompanionAStar2D>();
            var turtleHealth = turtle.GetComponent<CharacterStats>();

            if(turtleFollow) turtleFollow.enabled = true;
            if(turtleGroundFollow) turtleGroundFollow.enabled = true;
            if(turtleHealth) turtleHealth.enabled = true;
        }
    }

}
