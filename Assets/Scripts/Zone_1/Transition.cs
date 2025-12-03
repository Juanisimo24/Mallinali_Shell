using System;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Transition : MonoBehaviour, IInteract
{
    [SerializeField] PolygonCollider2D mapBoundry;
    CinemachineConfiner2D confiner;
    [SerializeField] Direction direction;
    enum Direction { Up, Down, Left, Right };

   [SerializeField]
   private TextMeshProUGUI interactText;

   private bool interactAllowed;

   private GameObject player;
   private bool haveText;
    [SerializeField] int distance;
    public GameObject[] parallaxBackgrounds;
    private void Awake()
    {
        confiner = FindAnyObjectByType<CinemachineConfiner2D>();
        haveText = interactText!=null;
        if(haveText)
            interactText.gameObject.SetActive(false);
    }

    void Update()
    {
        if(canInteract())
            beInteracted();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
 if (other.gameObject.CompareTag("Player"))
        {
            
            if(haveText)
                interactText.gameObject.SetActive(true);
            interactAllowed = true;
            player = other.gameObject;
        }

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        interactAllowed = false;
        if(haveText)
            interactText.gameObject.SetActive(false);
    }

    private void UpdatePlayerPosition(GameObject player)
    {
        Vector3 newPos = player.transform.position;
        switch (direction)
        {
            case Direction.Up:
            newPos.y += distance;
            break;
            case Direction.Down:
            newPos.y -= distance;
            break;
            case Direction.Left:
            newPos.x -= distance;
            break;
            case Direction.Right:
            newPos.x += distance;
            break;

        }     
        player.transform.position = newPos; 
    }

        public void beInteracted()
    {
        if (parallaxBackgrounds != null)
            {
                foreach (var bg in parallaxBackgrounds)
                {
                    if (bg != null)
                    {
                        bg.SetActive(false); // Esto apaga el script y detiene el movimiento
                    }
                }
            }
        confiner.BoundingShape2D = mapBoundry;
        UpdatePlayerPosition(player);
        interactAllowed = false;
        if(haveText)
            interactText.gameObject.SetActive(false);
    }

    public bool canInteract()
    {
        if (interactAllowed&& Input.GetKeyDown(KeyCode.E))
            return true;
        return false;
    }
}
