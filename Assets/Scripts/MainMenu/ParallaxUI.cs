using UnityEngine;

public class ParallaxUI : MonoBehaviour
{
    public RectTransform[] layers;
    public float[] speed;

    private Vector2[] initialPos;

    void Start()
    {
        initialPos = new Vector2[layers.Length];

        for (int i = 0; i < layers.Length; i++)
            initialPos[i] = layers[i].anchoredPosition;
    }

    void Update()
    {
        for (int i = 0; i < layers.Length; i++)
        {
            float movement = Mathf.Sin(Time.time * speed[i]) * 10f;

            layers[i].anchoredPosition = new Vector2(
                initialPos[i].x + movement,
                initialPos[i].y
            );
        }
    }
}
