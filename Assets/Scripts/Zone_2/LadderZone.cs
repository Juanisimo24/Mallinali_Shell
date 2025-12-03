using UnityEngine;

public class LadderZone : MonoBehaviour
{
    [Header("Ladder Settings")]
    public float climbSpeed = 3f;
    [Tooltip("How fast they slide down if doing nothing (0 = stop completely)")]
    public float passiveSlideSpeed = 1f; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Try to find Warrior or Turtle controller
        PlayerController warrior = other.GetComponent<PlayerController>();
        TortugaController turtle = other.GetComponent<TortugaController>();

        if (warrior != null) warrior.SetLadderState(true, climbSpeed, passiveSlideSpeed);
        if (turtle != null) turtle.SetLadderState(true, climbSpeed, passiveSlideSpeed);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController warrior = other.GetComponent<PlayerController>();
        TortugaController turtle = other.GetComponent<TortugaController>();

        if (warrior != null) warrior.SetLadderState(false, 0, 0);
        if (turtle != null) turtle.SetLadderState(false, 0, 0);
    }
}