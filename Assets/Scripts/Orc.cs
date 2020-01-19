﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orc : MonoBehaviour
{
    [SerializeField] private float SingleNodeMoveTime = 0.5f;

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
            SingleNodeMoveTime = 0.3f;
        }
        else
        {
            SingleNodeMoveTime = 0.5f;
        }

        // Uses raycasts for the orc to scan for the player
        if (!spottedPlayer && !trackingPlayer)
        {
            Vector3 startPos = transform.position;
            startPos.y += 0.05f;
            float startAngle = 200 * -0.5f;
            float endAngle = 200 * 0.5f;
            float increment = 200 / 20;

            RaycastHit hit;
            // Scan within orcs field of vision
            for (float i = startAngle; i <= endAngle; i += increment)
            {
                Vector3 targetPos = (Quaternion.Euler(0, i, 0) * transform.forward).normalized * 20f;

                if (Physics.Raycast(startPos, targetPos, out hit, 20f))
                {
                    // Test whether hit object is the player
                    GameObject hitObject = hit.transform.gameObject;
                    if (hitObject.tag == "Player")
                    {
                        Debug.DrawRay(startPos, targetPos * hit.distance, Color.red);
                        spottedPlayer = true;
                    }
                }
            }
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
            EnvironmentTile LastPosition = new EnvironmentTile();
            moving = true;
            Vector3 position = CurrentPosition.Position;
            for (int count = 0; count < route.Count; count++)
            {
                LastPosition = CurrentPosition;
                Vector3 next = route[count].Position;
                yield return DoMove(position, next);
                CurrentPosition = route[count];
                // Makes character current position un-accessible
                CurrentPosition.IsAccessible = false;
                //Makes character last position access-ible once character has moved on
                LastPosition.IsAccessible = true;
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
