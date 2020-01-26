using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orc : MonoBehaviour
{
    [SerializeField] private float SingleNodeMoveTime = 0.5f;

    public Character mTarget { get; set; } 

    public EnvironmentTile CurrentPosition { get; set; }
    public bool moving { get; set; }
    public bool spottedPlayer { get; set; }
    public bool trackingPlayer { get; set; }

    void Start()
    {
        moving = false;
        spottedPlayer = false;
        trackingPlayer = false;
    }

    void Update()
    {
        // If orc is following the player then speed up
        if (spottedPlayer || trackingPlayer)
        {
            SingleNodeMoveTime = 0.4f;
        }
        else
        {
            SingleNodeMoveTime = 0.5f;
        }
        // Test to see if the orc can see the player
        RaycastHit hit;

        Vector3 startPos = transform.position;
        startPos.y += 1f;
        Vector3 targetPos = mTarget.transform.position;
        targetPos.y += 1f;
        // If there is an unbroken line from orc to player
        if (!Physics.Linecast(startPos, targetPos, out hit))
        {
            
            // If player is within orcs line of sight
            float angle = Vector3.Angle(startPos, targetPos);
            if (angle < 60f && hit.distance < 20f)
            {
                // If orc isn't already following the player
                if (!(spottedPlayer || trackingPlayer))
                {
                    spottedPlayer = true;
                }
            }
        }
        else
        {
            spottedPlayer = false;
            trackingPlayer = false;
        }
    }

    private IEnumerator DoMove(Vector3 position, Vector3 destination)
    {
        // Move between the two specified positions over the specified amount of time
        if (position != destination)
        {
            transform.rotation = Quaternion.LookRotation(destination - position, Vector3.up);

            Vector3 p = transform.position;
            float t = 0.0f;

            while (t < SingleNodeMoveTime)
            {
                t += Time.deltaTime;
                p = Vector3.Lerp(position, destination, t / SingleNodeMoveTime);
                transform.position = p;
                yield return null;
            }
        }
    }

    private IEnumerator DoGoTo(List<EnvironmentTile> route)
    {
        // Move through each tile in the given route
        if (route != null)
        {
            moving = true;
            Vector3 position = CurrentPosition.Position;
            for (int count = 0; count < route.Count; count++)
            {
                Vector3 next = route[count].Position;
                yield return DoMove(position, next);
                CurrentPosition = route[count];
                position = next;
            }
        }
        moving = false;
    }

    public void GoTo(List<EnvironmentTile> route)
    {
        // Clear all coroutines before starting the new route so 
        // that clicks can interupt any current route animation
        StopAllCoroutines();
        StartCoroutine(DoGoTo(route));
    }
}
