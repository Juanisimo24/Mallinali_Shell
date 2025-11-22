using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class PlayerManagerDual : MonoBehaviour
{
    private int attackCounter = 0;
    public CinemachineCamera camera;
    public GameObject warrior;    // Guerrero azteca
    public GameObject turtle;     // Tortuga
    public KeyCode switchKey = KeyCode.Tab;

    private GameObject active;
    private GameObject follower;

    public SpecialFloor floorsito;

    private bool underWater;

    void Start()
    {
        
        SetActive(warrior); // Arrancamos con el guerrero
    }

    void Update()
    {
        if (floorsito != null)
            underWater = floorsito.getUnderWater();
            
        if (Input.GetKeyDown(switchKey))
            Switch();
    }

     void CambiarTarget(Transform nuevoObjetivo)
    {
        camera.Follow = nuevoObjetivo;
        camera.LookAt = nuevoObjetivo;
    }

    void Switch()
    {
        if (active == warrior)
        {
            SetActive(turtle);
            CambiarTarget(turtle.transform);

        }
        else
        {
            SetActive(warrior);
            CambiarTarget(warrior.transform);
        }
    }

    void SetActive(GameObject newActive)
    {
        active = newActive;
        if (newActive == warrior)
        {

            follower = turtle;
            var activeCtrl = active.GetComponent<PlayerController>();
            if (activeCtrl) activeCtrl.enabled = true;

            if (!underWater)
            {
                var activeFol = active.GetComponent<FollowerGround2D>();
            if (activeFol) activeFol.enabled = false;

            // Desactivar control del seguidor y activar seguimiento
            var folCtrl = follower.GetComponent<TortugaController>();
            var folFol = follower.GetComponent<FollowerGround2D>();
            if (folCtrl) folCtrl.enabled = false;
            if (folFol)
            {
                folFol.leader = active.transform;
                //folFol.WarpBehindLeader();  lo coloca detrás y en el piso
                folFol.enabled = true;
            }
            }
            
        }else if (newActive == turtle)
        {
            follower = warrior;
            if (!underWater)
            {
                var activeCtrl = active.GetComponent<TortugaController>();
                if (activeCtrl) activeCtrl.enabled = true;
            }
            
            var activeFol = active.GetComponent<FollowerGround2D>();
            
            if (activeFol) activeFol.enabled = false;

            // Desactivar control del seguidor y activar seguimiento
            var folCtrl = follower.GetComponent<PlayerController>();
            var folFol = follower.GetComponent<FollowerGround2D>();
            if (folCtrl) folCtrl.enabled = false;
            if (folFol)
            {
                folFol.leader = active.transform;
                //folFol.WarpBehindLeader();  lo coloca detrás y en el piso
                folFol.enabled = true;
            }
        }
        
    }

    public void RegisterAttack()
    {
        attackCounter++;
        if (attackCounter >= 3)
        {
            // Combo dual
            follower.GetComponent<PlayerCombat>().PerformComboAttack();
            attackCounter = 0;
        }
    }
    
    public GameObject GetActive()
    {
        return active;
    }
        public GameObject GetFollower()
    {
        return follower;
    }
}
