﻿using System;
using System.Collections;
using BattleshipGame.Core;
using BattleshipGame.Managers;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static BattleshipGame.Core.GameStateContainer.GameState;

namespace BattleshipGame.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject progressBarCanvasPrefab;
        [SerializeField] private GameStateContainer gameStateContainer;
        [SerializeField] private AiSelectMenuController aiSelectMenuController;
        [SerializeField] private OptionsMenuController optionsMenuController;
        [SerializeField] private LanguageMenuController languageMenuController;
        [SerializeField] private ButtonController singlePlayerButton;
        [SerializeField] private ButtonController multiplayerCancelButton;
        [SerializeField] private ButtonController multiplayerButton;
        [SerializeField] private ButtonController optionsButton;
        [SerializeField] private ButtonController quitButton;
        private bool _isConnecting;
        private bool _isConnectionCanceled;
        private GameObject _progressBar;
        private GameManager _gameManager;
        private Canvas _canvas;

        private void Awake()
        {
            if (!GameManager.TryGetInstance(out _gameManager)) SceneManager.LoadScene(0);
        }

        private void Start()
        {
            _canvas = GetComponent<Canvas>();
            singlePlayerButton.AddListener(() =>
            {
                _canvas.enabled = false;
                aiSelectMenuController.Show();
            });
            optionsButton.AddListener(() =>
            {
                _canvas.enabled = false;
                optionsMenuController.Show();
            });
            multiplayerButton.AddListener(PlayWithFriends);
            multiplayerCancelButton.AddListener(CancelConnection);
            multiplayerCancelButton.Hide();
            quitButton.AddListener(Quit);

            void PlayWithFriends()
            {
                if (!_isConnecting)
                {
                    _isConnecting = true;
                    singlePlayerButton.SetInteractable(false);
                    optionsButton.SetInteractable(false);
                    quitButton.SetInteractable(false);
                    multiplayerButton.Hide();
                    multiplayerCancelButton.Show();
                    if (progressBarCanvasPrefab) _progressBar = Instantiate(progressBarCanvasPrefab);
                    _gameManager.ConnectToServer(() =>
                    {
                        _isConnecting = false;
                        if (_isConnectionCanceled)
                            StartCoroutine(FinishNetworkClient());
                        else
                            GameSceneManager.Instance.GoToLobby();
                    }, () =>
                    {
                        _isConnecting = false;
                        _isConnectionCanceled = false;
                        gameStateContainer.State = NetworkError;
                        ResetMenu();
                    });
                }

                IEnumerator FinishNetworkClient()
                {
                    yield return new WaitForSecondsRealtime(1);
                    _gameManager.FinishNetworkClient();
                    _isConnectionCanceled = false;
                    multiplayerButton.SetInteractable(true);
                }
            }

            void CancelConnection()
            {
                _isConnectionCanceled = true;
                ResetMenu();
                multiplayerButton.SetInteractable(false);
            }

            void ResetMenu()
            {
                singlePlayerButton.SetInteractable(true);
                optionsButton.SetInteractable(true);
                quitButton.SetInteractable(true);
                multiplayerButton.Show();
                multiplayerCancelButton.Hide();
                Destroy(_progressBar);
                gameStateContainer.State = MainMenu;
            }
        }

        public void Show()
        {
            _canvas.enabled = true;
            gameStateContainer.State = MainMenu;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                switch (gameStateContainer.State)
                {
                    case GameStart:
                    case MainMenu:
                        Quit();
                        break;
                    case OptionsMenu:
                        optionsMenuController.Close();
                        break;
                    case LanguageOptionsMenu:
                        languageMenuController.Close();
                        break;
                    case AiSelectionMenu:
                        aiSelectMenuController.Close();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
                Application.OpenURL("about:blank");
#else
                Application.Quit();
#endif
        }
    }
}