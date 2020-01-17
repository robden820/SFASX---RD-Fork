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
    private Robot mRobot;
    private Orc mOrc;
    private Environment mMap;

    private bool robotClicked;

    private readonly int NumberOfRaycastHits = 1;

    void Start()
    {
        mRaycastHits = new RaycastHit[NumberOfRaycastHits];
        mMap = GetComponentInChildren<Environment>();
        mCharacter = Instantiate(Character, transform);
        mRobot = Instantiate(Robot, transform);
        mOrc = Instantiate(Orc, transform);
        InitialiseCamera(MainCamera);
        ShowMenu(true);

        robotClicked = false;
    }

    private void Update()
    {
        CameraFollow(mCharacter, MainCamera);

        if(Input.GetMouseButtonDown(0))
        {
            Ray screenClick = MainCamera.ScreenPointToRay(Input.mousePosition);
            int hits = Physics.RaycastNonAlloc(screenClick, mRaycastHits);
            if( hits > 0)
            {
                EnvironmentTile tile = mRaycastHits[0].transform.GetComponent<EnvironmentTile>();
                if (tile != null)
                {
                    List<EnvironmentTile> mCharRoute = mMap.Solve(mCharacter.CurrentPosition, tile);
                    mCharacter.GoTo(mCharRoute);
                }
                if (tile == mRobot.CurrentPosition)
                {
                    robotClicked = true;
                }
                else
                {
                    robotClicked = false;
                }
            }
        }

        if (robotClicked)
        {
            EnvironmentTile directConnection = mCharacter.CurrentPosition.Connections.Find(c => c == mRobot.CurrentPosition);
            if (directConnection != null)
            {
                Debug.Log("You have activated the robot");
                mRobot.activated = true;
            }
        }

        if (mRobot.activated && !mRobot.moving)
        {
             List<EnvironmentTile> mRobotRoute = mMap.Solve(mRobot.CurrentPosition, mCharacter.CurrentPosition);
             mRobot.GoTo(mRobotRoute);
        }

        if (!mOrc.moving)
        {
            EnvironmentTile tile = new EnvironmentTile();

            if (mOrc.spottedPlayer)
            {
                Debug.Log("Moving towards player");
                tile = mCharacter.CurrentPosition;
            }
            else
            {
                int x = Random.Range(0, mMap.Size.x);
                int y = Random.Range(0, mMap.Size.y);
                Vector2Int position = new Vector2Int(Random.Range(0, mMap.Size.x), Random.Range(0, mMap.Size.y));

                tile = mMap.GetTileAtPosition(position);
            }
            List<EnvironmentTile> mOrcRoute = mMap.Solve(mOrc.CurrentPosition, tile);
            mOrc.GoTo(mOrcRoute);
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

    void CameraFollow(Character character, Camera MainCamera)
    {
        Vector3 characterPos = character.transform.position;
        characterPos.y += 300;
        characterPos.z -= 100;
        MainCamera.transform.position = characterPos;
    }
}
