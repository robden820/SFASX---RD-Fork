using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class Game : MonoBehaviour
{
    [SerializeField] private Camera MainCamera;
    [SerializeField] private Character Character;
    [SerializeField] private Robot Robot;
    [SerializeField] private Orc Orc;
    [SerializeField] private int numOrcs;
    [SerializeField] private Canvas Menu;
    [SerializeField] private Canvas Hud;
    [SerializeField] private Canvas EndScreen;
    [SerializeField] private Transform CharacterStart;

    private RaycastHit[] mRaycastHits;
    private Character mCharacter;
    private int mCharacterHealth;
    private Robot mRobot;
    private Orc[] mOrcs;
    private Environment mMap;
    private bool robotClicked;

    private readonly int NumberOfRaycastHits = 1;
    private PlayerHealth PlayerHealth;

    void Start()
    {
        mRaycastHits = new RaycastHit[NumberOfRaycastHits];
        mMap = GetComponentInChildren<Environment>();
        InitialiseCamera(MainCamera);
        
        //Create character
        mCharacter = Instantiate(Character, transform);
        
        // Create robot
        mRobot = Instantiate(Robot, transform);
        robotClicked = false;
        //Create enemy orcs
        mOrcs = new Orc[numOrcs];
        for (int i = 0; i < numOrcs; i++)
        {
            mOrcs[i] = Instantiate(Orc, transform);
            mOrcs[i].mTarget = mCharacter;
        }

        PlayerHealth = Hud.transform.GetChild(1).gameObject.GetComponent<PlayerHealth>();
        //HealthBar(mCharacterHealth);

        ShowMenu(true);
    }

    private void Update()
    {
        CameraFollow(mCharacter, MainCamera);

        if (Input.GetMouseButtonDown(0))
        {
            Ray screenClick = MainCamera.ScreenPointToRay(Input.mousePosition);
            int hits = Physics.RaycastNonAlloc(screenClick, mRaycastHits);
            if( hits > 0)
            {
                EnvironmentTile tile = mRaycastHits[0].transform.GetComponent<EnvironmentTile>();
                if (!robotClicked && tile == mRobot.CurrentPosition)
                {
                    robotClicked = true;
                    tile = mMap.GetNeighbourTile(tile); //Player moves next to robot instead of on-top, if tile were inaccessible player could not select
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
                robotClicked = false;

            }
        }
        // If the robot isn't moving and activated then move the robot
        if (mRobot.activated && !mRobot.moving)
        {
            // Once activated the robot follows the player
             List<EnvironmentTile> mRobotRoute = mMap.Solve(mRobot.CurrentPosition, mCharacter.LastPosition);
             mRobot.GoTo(mRobotRoute);
        }
        
        foreach (Orc orc in mOrcs)
        {
            // If orc has seen the player then move towards the player
            // This if statement means it will interrupt the orcs current movement when it spots the player
            if (orc.spottedPlayer || (orc.trackingPlayer && !orc.moving))
            {
                EnvironmentTile directConnection = orc.CurrentPosition.Connections.Find(c => c == mCharacter.CurrentPosition);
                List<EnvironmentTile> mOrcRoute = new List<EnvironmentTile>();
                if (directConnection)
                {
                    mOrcRoute = mMap.Solve(orc.CurrentPosition, mCharacter.CurrentPosition);
                }
                else
                {
                    mOrcRoute = mMap.Solve(orc.CurrentPosition, mCharacter.LastPosition);
                }
                
                orc.GoTo(mOrcRoute);
                orc.spottedPlayer = false;
                orc.trackingPlayer = true;
            }
            // Moves the orc randomly
            else if (!orc.moving)
            {
                int x = Random.Range(0, mMap.Size.x);
                int y = Random.Range(0, mMap.Size.y);
                Vector2Int position = new Vector2Int(Random.Range(0, mMap.Size.x), Random.Range(0, mMap.Size.y));

                EnvironmentTile tile = mMap.GetTileAtPosition(position);

                List<EnvironmentTile> mOrcRoute = mMap.Solve(orc.CurrentPosition, tile);
                orc.GoTo(mOrcRoute);
            }
        }

        // Win condition and reduce health/lost condition
        if (mCharacter.CurrentPosition.IsWin && mRobot.activated)
        {
            // Reset state of orcs
            foreach (Orc orc in mOrcs)
            {
                // Stop orc moving towards player
                orc.trackingPlayer = false;
                orc.spottedPlayer = false;
            }
            // Reset state of robot
            mRobot.activated = false;
            robotClicked = false;
            mCharacterHealth -= 1;
            HealthBar(mCharacterHealth);

            GameFinished(true);
        }
        // If player is caught their position is reset
        // If player health is reduced to zero then player loses
        else if (PlayerCaptured())
        {
            // Needs both of these otherwise player loses >1 health
            mCharacter.StopAllCoroutines();
            mCharacter.CurrentPosition = mMap.CharStart;
            mCharacter.transform.position = mMap.CharStart.Position;
            mCharacterHealth -= 1;
            HealthBar(mCharacterHealth);
            // Reset state of orcs
            foreach (Orc orc in mOrcs)
            {
                // Stop orc moving towards player
                orc.trackingPlayer = false;
                orc.spottedPlayer = false;
            }
            
            // Stop robot moving towards player
            mRobot.activated = false;
            robotClicked = false;
            // If player loses all health then player loses
            if (mCharacterHealth == 0)
            {
                GameFinished(false);
            }
        }
    }

    public void ShowMenu(bool show)
    {
        if (Menu != null && Hud != null)
        {
            Menu.enabled = show;
            Hud.enabled = !show;
            EndScreen.enabled = false;

            if( show )
            {
                mCharacter.transform.position = CharacterStart.position;
                mCharacter.transform.rotation = CharacterStart.rotation;
                mMap.CleanUpWorld();
            }
            else
            {
                // Initilase player with 3 health
                mCharacter.transform.position = mMap.CharStart.Position;
                mCharacter.transform.rotation = Quaternion.identity;
                mCharacter.CurrentPosition = mMap.CharStart;
                mCharacterHealth = 3;
                PlayerHealth.transform.gameObject.SetActive(true);
                HealthBar(mCharacterHealth);

                // Initialise robot
                mRobot.transform.position = mMap.RobotStart.Position;
                mRobot.transform.rotation = Quaternion.identity;
                mRobot.CurrentPosition = mMap.RobotStart;

                // Initialise all orcs
                foreach (Orc orc in mOrcs)
                {
                    orc.transform.position = mMap.OrcStart.Position;
                    orc.transform.rotation = Quaternion.identity;
                    orc.CurrentPosition = mMap.OrcStart;
                }
            }
        }
    }

    private void GameFinished(bool playerWin)
    {
        EndScreen.enabled = true;
        Hud.enabled = false;
        PlayerHealth.transform.gameObject.SetActive(false);

        string success = "CONGRATULATIONS";
        string successMessage = "You managed to rescue the robot from the orcs";
        string failure = "GAME OVER";
        string failureMessage = "You were killed by the orcs before saving the robot";
        // Get text components from canvas
        TextMeshProUGUI EndGameTitleText = EndScreen.transform.Find("EndGameText").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI EndGameMessageText = EndScreen.transform.Find("EndGameMessage").GetComponent<TextMeshProUGUI>();
        //Show different messages depending on playerWin
        if (playerWin)
        {
            EndGameTitleText.SetText("CONGRATULATIONS");
            EndGameMessageText.SetText(successMessage);
        }
        else
        {
            EndGameTitleText.SetText("GAME OVER");
            EndGameMessageText.SetText(failureMessage);
        }
        
    }

    // Enables menu and disables end game screen
    public void RestartGame()
    {
        EndScreen.enabled = false;
        ShowMenu(true);
    }

    //Updates the player health bar
    private void HealthBar(int health)
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

    // Sets initial camera rotation
    private void InitialiseCamera(Camera camera)
    {
        camera.transform.rotation = Quaternion.Euler(0, 0, 0);
        camera.transform.Rotate(70, 0, 0);
    }

    //Make camera follow the player
    private void CameraFollow(Character character, Camera MainCamera)
    {
        Vector3 characterPos = character.transform.position;
        Vector3 cameraPos = new Vector3();

        // Camp camera to map
        cameraPos.x = Mathf.Clamp(characterPos.x, -35f, 35f);
        cameraPos.y = 300;
        cameraPos.z = Mathf.Clamp(characterPos.z -85f, -155f, -70f);

        MainCamera.transform.position = cameraPos;
    }

    //Test whether any orc has captured the player
    // Returns true if any orc and player share a tile
    private bool PlayerCaptured()
    {
        bool captured = false;
        foreach (Orc orc in mOrcs)
        {
            if (mCharacter.CurrentPosition == orc.CurrentPosition)
            {
                captured = true;
            }
        }
        return captured;
    }
}
