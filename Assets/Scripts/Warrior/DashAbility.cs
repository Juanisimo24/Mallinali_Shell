using UnityEngine;
using System.Collections;

// This line allows you to right click in Project -> Create -> Abilities -> Dash
[CreateAssetMenu(menuName = "Abilities/Dash")]
public class DashAbility : Ability
{
    public float dashForce = 20f;
    public float dashDuration = 0.2f;

    public override bool Activate(GameObject parent, CharacterStats stats)
    {
        Rigidbody2D rb = parent.GetComponent<Rigidbody2D>();
        SpriteRenderer sr = parent.GetComponent<SpriteRenderer>();

        if (rb == null) return false;

        // We need to run a Coroutine to handle the dash duration.
        // Since ScriptableObjects can't run Coroutines, we ask the Parent (Monobehaviour) to run it.
        MonoBehaviour runner = parent.GetComponent<MonoBehaviour>();
        if (runner != null)
        {
            runner.StartCoroutine(DashRoutine(rb, sr));
            return true;
        }
        return false;
    }

    private IEnumerator DashRoutine(Rigidbody2D rb, SpriteRenderer sr)
    {
        // 1. Calculate Direction
        // If moving, dash in movement direction. If idle, dash in facing direction.
        float horizontal = Input.GetAxisRaw("Horizontal");
        Vector2 dashDir = new Vector2(horizontal, 0);

        if (Mathf.Abs(horizontal) < 0.01f)
        {
            // Use Sprite Flip to determine direction if standing still
            dashDir = new Vector2(sr.flipX ? -1 : 1, 0);
        }

        // 2. Apply Physical Dash
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0; // Anti-gravity during dash
        rb.linearVelocity = dashDir.normalized * dashForce; // Unity 6 linearVelocity

        // Optional: Trigger "Dash" animation trigger if you have reference
        // parent.GetComponent<Animator>()?.SetTrigger("Dash");

        // 3. Wait
        yield return new WaitForSeconds(dashDuration);

        // 4. Cleanup
        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero; // Stop momentum or keep it? Zero gives crisp control.
    }
}