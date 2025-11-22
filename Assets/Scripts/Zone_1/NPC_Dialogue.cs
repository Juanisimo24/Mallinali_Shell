using System.Collections;
using TMPro;
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
    private GameObject player;  
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
        var control = other.GetComponent<PlayerController>();
        control.enabled=false;
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
        //var control = player.GetComponent<PlayerController>();
        //control.enabled=true;
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
        var turtleFollow = turtle.GetComponent<FollowerGround2D>();
        var turtleHealth = turtle.GetComponent<TurtleHealth>();
        manager.SetActive(true);
        turtleFollow.enabled = true;
        turtleHealth.enabled = true;

    }

}
