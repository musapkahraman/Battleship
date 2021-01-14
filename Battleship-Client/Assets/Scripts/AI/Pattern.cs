﻿using System.Collections.Generic;
using BattleshipGame.Core;
using UnityEngine;

namespace BattleshipGame.AI
{
    public struct Pattern
    {
        public Ship Ship;
        public Vector3Int Pivot;
        public List<Vector2Int> CheckedPartCoordinates;

        public Pattern(Ship ship, Vector3Int pivot, Vector2Int shotPartOfShip)
        {
            Ship = ship;
            Pivot = pivot;
            CheckedPartCoordinates = new List<Vector2Int> {shotPartOfShip};
        }
    }
}