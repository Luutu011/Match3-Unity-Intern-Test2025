using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };

    public enum eLevelMode
    {
        TIMER,
        MOVES
    }

    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_LOSE,
        GAME_WIN,
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set
        {
            m_state = value;

            StateChangedAction(m_state);
        }
    }


    private GameSettings m_gameSettings;


    private BoardController m_boardController;

    private UIMainManager m_uiMenu;

    private LevelCondition m_levelCondition;

    public List<Cell> bottomCells = new List<Cell>(5);
    //public List<Transform> bottomCellsUI = new List<Transform>(5);

    private void Awake()
    {
        State = eStateGame.SETUP;

        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);

        m_uiMenu = FindObjectOfType<UIMainManager>();
        m_uiMenu.Setup(this);
    }

    void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

    // Update is called once per frame
    void Update()
    {
       //goi 2 lan if (m_boardController != null) m_boardController.Update();
       if( m_boardController != null)
        {
            if(m_boardController.IsBoardEmpty()) GameWon();
        }
    }


    internal void SetState(eStateGame state)
    {
        State = state;

        if (State == eStateGame.PAUSE)
        {
            DOTween.PauseAll();
        }
        else
        {
            DOTween.PlayAll();
        }
    }

    public void LoadLevel(eLevelMode mode)
    {
        m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
        m_boardController.StartGame(this, m_gameSettings);

        if (mode == eLevelMode.MOVES)
        {
            m_levelCondition = this.gameObject.AddComponent<LevelMoves>();
            m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), m_boardController);
        }
        else if (mode == eLevelMode.TIMER)
        {
            m_levelCondition = this.gameObject.AddComponent<LevelTime>();
            m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), this);
        }

        m_levelCondition.ConditionCompleteEvent += GameOver;

        State = eStateGame.GAME_STARTED;
    }

   public void GameWon()
    {
        StartCoroutine(WaitBoardController(eStateGame.GAME_WIN));
    }
    public void GameOver()
    {
        StartCoroutine(WaitBoardController(eStateGame.GAME_LOSE));
    }

    internal void ClearLevel()
    {
        if (m_boardController)
        {
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }
    }

    private IEnumerator WaitBoardController(eStateGame state)
    {
        while (m_boardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);

        State = state;

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;

            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }

    internal void MoveToBottomCells(Cell c1)
    {
        Item item = c1.Item;
        int index = -1;
        for (int i = 0; i < bottomCells.Count; i++)
        {
            if (bottomCells[i].GetComponent<Cell>().Item == null)
            {
                Debug.Log("Move to bottom cells: " + item + " to slot " + i);
                item.View.DOMove(bottomCells[i].transform.position, 0.3f);
                bottomCells[i].Assign(item);

                c1.Free();

                bottomCells[i].gameObject.GetComponent<Image>().sprite = bottomCells[i].Item.View.GetComponent<SpriteRenderer>().sprite;
                index = i;
                break;
            }
        }
        ClearBottomCellsMatch();
        if (index == -1)
        {
            GameOver();
        }
    }

    internal void ClearBottomCellsMatch()
    {
        for (int i = 0; i < bottomCells.Count; i++)
        {
            int count = 1;
            int pos1 = i, pos2 = -1, pos3 = -1;
            for (int j = 0; j < bottomCells.Count; j++)
            {
                if (i == j) continue;
                if (bottomCells[i].IsSameType(bottomCells[j]))
                {
                    count++;
                    if (pos2 == -1)
                    {
                        pos2 = j;
                    }
                    else if (pos3 == -1)
                    {
                        pos3 = j;
                    }
                    if (count >= 3)
                    {
                        bottomCells[pos1].Clear();
                        bottomCells[pos1].gameObject.GetComponent<Image>().sprite = null;

                        bottomCells[pos2].Clear();
                        bottomCells[pos2].gameObject.GetComponent<Image>().sprite = null;

                        bottomCells[pos3].Clear();
                        bottomCells[pos3].gameObject.GetComponent<Image>().sprite = null;

                        Debug.Log("Cleared bottom cells at positions: " + pos1 + ", " + pos2 + ", " + pos3);

                    }
                }
            }
           
        }
    }
}
