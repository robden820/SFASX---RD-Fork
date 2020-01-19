using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Game : MonoBehaviour
{
    [SerializeField] private Camera MainCamera;
    [SerializeField] private Character Character;
    [SerializeField] private Robot Robot;
    [SerializeField] private Orc Orc;
    [SerializeField] private Canvas Menu;
    [SerializeField] private Canvas Hud;
    [SerializeField] private Transform CharacterStart;

    private RaycastHit[] mRaycastHits;
    private Character mCharacter;
    private int mCharacterHealth;
    private Robot mRobot;
    private Orc mOrc;
    private Environment mMap;
    private bool robotClicked;

    private readonly int NumberOfRaycastHits = 1;
    private PlayerHealth PlayerHealth;

    void Start()
    {

        mRaycastHits = new RaycastHit[NumberOfRaycastHits];
        mMap = GetComponentInChildren<Environment>();
        mCharacter = Instantiate(Character, transform);
        mCharacterHealth = 3;
        mRobot = Instantiate(Robot, transform);
        mOrc = Instantiate(Orc, transform);
        InitialiseCamera(MainCamera);
        ShowMenu(true);
        PlayerHealth = Hud.transform.GetChild(1).gameObject.GetComponent<PlayerHealth>();
        UpdatedHealthBar(mCharacterHealth);

        robotClicked = false;
    }

    private void Update()
    {
        CameraFollow(mCharacter, MainCamera);
        //UpdatedHud(mCharacterHealth);

        if(Input.GetMouseButtonDown(0))
        {
            Ray screenClick = MainCamera.ScreenPointToRay(Input.mousePosition);
            int hits = Physics.RaycastNonAlloc(screenClick, mRaycastHits);
            if( hits > 0)
            {
                EnvironmentTile tile = mRaycastHits[0].transform.GetComponent<EnvironmentTile>();
                if (!robotClicked && tile == mRobot.CurrentPosition)
                {
                    robotClicked = true;
                    tile = mMap.GetNeighbourTile(tile);
                }

                if (tile != null)
                {
                    List<EnvironmentTile> mCharRoute = mMap.Solve(mCharacter.CurrentPosition, tile);
                    mCharacter.GoTo(mCharRoute);
                }
            }
        }

        // Activate robot if the plaer clicks and is next to the robot
        if (robotClicked)
        {
            //Player must be next to the robot to activate it
            EnvironmentTile directConnection = mCharacter.CurrentPosition.Connections.Find(c => c == mRobot.CurrentPosition);
            if (directConnection != null)
            {
                Debug.Log("You have activated the robot");
                mRobot.activated = true;

            }
        }

        // If the robot isn't moving and activated then move the robot
        if (mRobot.activated && !mRobot.moving)
        {
            // Once activated the robot follows the player
             List<EnvironmentTile> mRobotRoute = mMap.Solve(mRobot.CurrentPosition, mCharacter.LastPosition);
             mRobot.GoTo(mRobotRoute);
        }

        // If orc has seen the player then move towards the player
        // This if statement means it will interrupt the orcs current movement when it spots the player
        if (mOrc.spottedPlayer || (mOrc.trackingPlayer && !mOrc.moving))
        {
            List<EnvironmentTile> mOrcRoute = mMap.Solve(mOrc.CurrentPosition, mCharacter.CurrentPosition);
            mOrc.GoTo(mOrcRoute);
            mOrc.spottedPlayer = false;
            mOrc.trackingPlayer = true;
        }
        // Moves the orc randomly
        else if (!mOrc.moving)
        {
            int x = Random.Range(0, mMap.Size.x);
            int y = Random.Range(0, mMap.Size.y);
            Vector2Int position = new Vector2Int(Random.Range(0, mMap.Size.x), Random.Range(0, mMap.Size.y));

            EnvironmentTile tile = mMap.GetTileAtPosition(position);

            List<EnvironmentTile> mOrcRoute = mMap.Solve(mOrc.CurrentPosition, tile);
            mOrc.GoTo(mOrcRoute);
        }

        // Win condition and reduce health/lost condition
        // If player is caught their position is reset
        if (mCharacter.CurrentPosition.IsWin && mRobot.activated)
        {
            Debug.Log("YOu have won the game");
        }
        else if (mCharacter.CurrentPosition == mOrc.CurrentPosition)
        {
            // Needs both of these otherwise player loses >1 health
            mCharacter.CurrentPosition = mMap.CharStart;
            mCharacter.transform.position = mMap.CharStart.Position;
            mCharacterHealth -= 1;
            UpdatedHealthBar(mCharacterHealth);
            // Stop orc moving towards player
            mOrc.trackingPlayer = false;
            mOrc.spottedPlayer = false;
            // Stop robot moving towards player
            mRobot.activated = false;
            robotClicked = false;
            // If player loses all health then player loses
            if (mCharacterHealth == 0)
            {
                Debug.Log("You have lost the game");
            }
        }
    }

    public void ShowMenu(bool show)
    {
        if (Menu != null && Hud != null)
        {
            Menu.enabled = show;
            Hud.enabled = !show;

            if( show )
            {
                mCharacter.transform.position = CharacterStart.position;
                mCharacter.transform.rotation = CharacterStart.rotation;
                mMap.CleanUpWorld();
            }
            else
            {
                mCharacter.transform.position = mMap.CharStart.Position;
                mCharacter.transform.rotation = Quaternion.identity;
                mCharacter.CurrentPosition = mMap.CharStart;

                mRobot.transform.position = mMap.RobotStart.Position;
                mRobot.transform.rotation = Quaternion.identity;
                mRobot.CurrentPosition = mMap.RobotStart;

                mOrc.transform.position = mMap.OrcStart.Position;
                mOrc.transform.rotation = Quaternion.identity;
                mOrc.CurrentPosition = mMap.OrcStart;
            }
        }
    }

    void UpdatedHealthBar(int health)
    {
        PlayerHealth.UpdateHealthBar(health);
    }

    public void Generate()
    {
        mMap.GenerateWorld();
    }

    public void Exit()
    {
#if !UNITY_EDITOR
        Application.Quit();
#endif
    }

    void InitialiseCamera(Camera camera)
    {
        camera.transform.rotation = Quaternion.Euler(0, 0, 0);
        camera.transform.Rotate(70, 0, 0);
    }

    //Make camera follow the player but clamped by map edges
    void CameraFollow(Character character, Camera MainCamera)
    {
        Vector3 characterPos = character.transform.position;
        Vector3 cameraPos = new Vector3();

        cameraPos.x = Mathf.Clamp(characterPos.x, -85f, 85f);
        cameraPos.y = 300;
        cameraPos.z = Mathf.Clamp(characterPos.z - 100f, -205f, -15f);

        MainCamera.transform.position = cameraPos;
    }
}
