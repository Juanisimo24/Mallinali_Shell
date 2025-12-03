using UnityEngine;

public class BackgroundParallax : MonoBehaviour
{
 private float startPos,length;
  private float startPosY;
 public GameObject cam;
 public float parallaxEffect
;

 void Start()
    {
        startPos = transform.position.x;
        length = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void FixedUpdate()
    {
        float distance = cam.transform.position.x * parallaxEffect; // 0 with cam|| 1 not move || 0.5 half
        float movement = cam.transform.position.x *(1-parallaxEffect);

        transform.position = new Vector3(startPos+distance,transform.position.y,transform.position.z);

        if (movement > startPos + length)
        {
            startPos +=length*3;
        }else if (movement < startPos - length)
        {
            startPos-=length*3;
        }
    }
}

