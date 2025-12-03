using System;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class TransitionNI : MonoBehaviour
{
    [SerializeField] PolygonCollider2D mapBoundry;
    CinemachineConfiner2D confiner;
    [SerializeField] Direction direction;
    enum Direction { Up, Down, Left, Right };

   [SerializeField]

   private GameObject player;
    [SerializeField] int distance;
    private void Awake()
    {
        confiner = FindAnyObjectByType<CinemachineConfiner2D>();
      
    }



    private void OnTriggerEnter2D(Collider2D other)
    {
 if (other.gameObject.CompareTag("Player"))
        {
            player = other.gameObject;
            beInteracted();
        }

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
        confiner.BoundingShape2D = mapBoundry;
        UpdatePlayerPosition(player);

    }

}
