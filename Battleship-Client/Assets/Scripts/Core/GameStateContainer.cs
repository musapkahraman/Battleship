﻿using System;
using UnityEngine;

namespace BattleshipGame.Core
{
    [CreateAssetMenu(menuName = "Battleship/Game State Container")]
    public class GameStateContainer : ScriptableObject
    {
        private GameState _state;

        public GameState State
        {
            get => _state;
            set
            {
                _state = value;
                StateChanged?.Invoke(_state);
            }
        }

        public event Action<GameState> StateChanged;

        public enum GameState
        {
            GameStart,
            MainMenu,
            OptionsMenu,
            LanguageOptionsMenu,
            AiSelectionMenu,
            NetworkError,
            BeginLobby,
            Connecting,
            WaitingOpponentJoin,
            BeginPlacement,
            PlacementImpossible,
            PlacementReady,
            WaitingOpponentPlacement,
            BeginBattle,
            PlayerTurn,
            OpponentTurn,
            BattleResult,
            WaitingOpponentRematchDecision
        }
    }
}