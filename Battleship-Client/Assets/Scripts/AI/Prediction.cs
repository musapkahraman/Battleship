﻿using System.Collections.Generic;
using System.Linq;
using BattleshipGame.Common;
using BattleshipGame.ScriptableObjects;
using UnityEngine;

namespace BattleshipGame.AI
{
    public class Prediction
    {
        private readonly int _areaWidth;
        private readonly Dictionary<int, List<Probability>> _probabilityMap = new Dictionary<int, List<Probability>>();
        private List<int> _playerShipsHealth = new List<int>(); // To figure out diffs on each Prediction.Update call.
        private List<int> _shots;

        public Prediction(Rules rules)
        {
            _areaWidth = rules.areaSize.x;
            InitProbabilityMap(rules);
        }

        private void InitProbabilityMap(Rules rules)
        {
            int cellCount = rules.areaSize.x * rules.areaSize.y;
            var shipId = 0;
            foreach (var ship in rules.ships)
            {
                float shipProbability = CalculateProbability(ship.PartCoordinates.Count, cellCount);
                for (var i = 0; i < ship.amount; i++)
                {
                    var shipProbabilities = new List<Probability>();
                    for (var cell = 0; cell < cellCount; cell++)
                        shipProbabilities.Add(new Probability(cell, shipProbability));

                    _probabilityMap.Add(shipId, shipProbabilities);

                    // Initialize the local list to hold the health values of player ships. This is to figure out diffs.
                    _playerShipsHealth.Add(ship.PartCoordinates.Count);

                    shipId++;
                }
            }
        }

        private static float CalculateProbability(int remainingShipPartCount, int availableCellCount)
        {
            return (float) remainingShipPartCount / availableCellCount;
        }

        private float GetProbabilityValue(int shipId, int cell)
        {
            return _probabilityMap[shipId].Single(p => p.Cell == cell).Value;
        }

        public List<int> GetMostProbableCells(IEnumerable<int> unmarkedCells, int size, IEnumerable<int> shipIds)
        {
            var cells = new List<int>();

            var probabilities = (from cell in unmarkedCells
                let sum = shipIds.Sum(shipId => GetProbabilityValue(shipId, cell))
                select new Probability(cell, sum)).ToList();

            // If the probabilities of finding a ship in multiple cells are at most, select them randomly.
            float max = probabilities.Max(p => p.Value);
            int count = probabilities.Count(p => Mathf.Approximately(p.Value, max));
            if (count > size)
            {
                var randomPool = (from probability in probabilities
                    where Mathf.Approximately(probability.Value, max)
                    select probability.Cell).ToList();

                for (var i = 0; i < size; i++)
                {
                    int index = Random.Range(0, randomPool.Count);
                    if (cells.Count >= size) break;
                    cells.Add(randomPool[index]);
                    randomPool.Remove(cells[i]);
                }

                _shots = cells.ToList();
                return cells;
            }

            var orderedProbabilities = probabilities.OrderByDescending(p => p.Value);
            foreach (var probability in orderedProbabilities)
            {
                if (cells.Count >= size || probability.Value <= 0f) break;
                cells.Add(probability.Cell);
            }

            _shots = cells.ToList();
            return cells;
        }

        public void Update(List<int> playerShipsHealth, SortedDictionary<int, Ship> pool)
        {
            var damagedShips = new List<Ship>();
            var totalDamage = 0;
            for (var shipId = 0; shipId < playerShipsHealth.Count; shipId++)
            {
                int damage = _playerShipsHealth[shipId] - playerShipsHealth[shipId];
                totalDamage += damage;
                if (damage > 0)
                {
                    Debug.Log($"Ship {shipId} was damaged {damage} units.");
                    damagedShips.Add(pool[shipId]);
                    if (damage > 1) Debug.Log($"Ship {shipId} had multiple shots.");

                    if (playerShipsHealth[shipId] <= 0) Debug.Log($"Ship {shipId} was sunk.");
                }
            }

            if (totalDamage > 0) FindPossiblePatterns();

            _playerShipsHealth = playerShipsHealth.ToList();

            void FindPossiblePatterns()
            {
                if (_shots == null)
                {
                    Debug.Log("This is the first call.");
                    return;
                }

                foreach (int shot in _shots)
                {
                    var shotCoordinate = GridUtils.CellIndexToCoordinate(shot, _areaWidth);
                    Debug.Log($"Pattern try for shot at cell: {shot} -> {shotCoordinate}");
                    foreach (var ship in damagedShips)
                    {
                        Debug.Log($"ship: {ship.rankOrder}, {ship.name}");
                        (int shipWidth, int shipHeight) = ship.GetShipSize();
                        foreach (var shipPartCoordinate in ship.PartCoordinates)
                        {
                            var cellCoordinate = shotCoordinate - (Vector3Int) shipPartCoordinate;
                            if (GridUtils.DoesShipFitIn(shipWidth, shipHeight, cellCoordinate, _areaWidth))
                            {
                                Debug.Log($"{shipPartCoordinate} fits.");
                                var pattern = new Pattern(ship, shipPartCoordinate, shot, shotCoordinate);
                            }
                        }
                    }
                }
            }
        }
    }
}