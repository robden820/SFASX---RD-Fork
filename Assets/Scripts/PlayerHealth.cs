using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private Transform HealthIcon;

    private Vector3 Position;
    private Vector3 Scale;

    // Start is called before the first frame update
    void Start()
    {
        Position.x = 0;
        Position.y = 40;
        Position.z = 0;
        Scale = Vector3.one * 15f;
    }

    public void UpdateHealthBar(int CurrentPlayerHealth)
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        for (int i = -1; i < CurrentPlayerHealth - 1; i++)
        {
            Debug.Log(i);
            Transform point = Instantiate(HealthIcon);
            Position.x = 150 + 75 * i;
            point.localPosition = Position;
            point.localScale = Scale;
            point.SetParent(transform, false);
        }
    }
}
