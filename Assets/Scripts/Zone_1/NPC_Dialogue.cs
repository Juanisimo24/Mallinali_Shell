using System.Collections;
using Pathfinding;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
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
        control = other.GetComponent<PlayerController>();
        control.canControl = false;
        isDialogueActive=true;
        StartDialogue();
    }

    void StartDialogue()
    {
        isDialogueActive = true;
        dialogueIndex = 0;
        nameText.SetText(dialogueData.npcName);
        portraitImage.sprite = dialogueData.npcPortrait;
        dialoguePanel.SetActive(true);
        
        StartCoroutine(TypeLine());
    }

    void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.SetText(dialogueData.dialogueLine[dialogueIndex]);
            isTyping=false;
        }else if (++dialogueIndex < dialogueData.dialogueLine.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.SetText("");
        if (dialogueData.autoProgressLine[dialogueIndex])
        {
            portraitImage.sprite = dialogueData.npcPortrait;
        }
        else
        {
            portraitImage.sprite = dialogueData.playerPortrait;
        }
        foreach(char letter in dialogueData.dialogueLine[dialogueIndex])
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueData.typingSpeed);
        }

        isTyping = false;

        if(dialogueData.autoProgressLine.Length > dialogueIndex  )
        {
            yield return new WaitForSeconds(dialogueData.autoProgressDelay);
            NextLine();
        }
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
        //var manage = manager.GetComponent<PlayerManagerDual>();
        var turtleFollow = turtle.GetComponent<Seeker>();
        var turtleGroundFollow = turtle.GetComponent<CompanionAStar2D>();
        //var turtleFollow = turtle.GetComponent<SmartPlatformFollower2D>();
        var turtleHealth = turtle.GetComponent<CharacterStats>();
        manager.SetActive(true);
        turtleFollow.enabled = true;
        turtleGroundFollow.enabled = true;
        turtleHealth.enabled = true;

    }

}
