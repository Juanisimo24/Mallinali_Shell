using Unity.Cinemachine;
using UnityEngine;

public class Transition_S2_S1 : MonoBehaviour
{
    [SerializeField] PolygonCollider2D mapBoundry;
    CinemachineConfiner2D confiner;

    private void Awake()
    {
        confiner = FindAnyObjectByType<CinemachineConfiner2D>();

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Principal"))
        {
            confiner.BoundingShape2D = mapBoundry;
        }
    }
}
