using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ants
{
    partial class Bot
    {
        static Bot _instance = new Bot();
        public static Bot Instance { get { return _instance; } }
        Random rnd;
        bool UseDebugDraw;
        public bool SaveInputData;
        bool iSeeEnemy = false;

        private Bot()
        {
        }

        public void DoTurn(GameState state)
        {
            foreach (var ant in state.MyAnts)
            {
                ant.Food = null;
                ant.Move = false;
                ant.EnemyHill = null;
                ant.Processed = false;
                ant.Defend = null;
                if (ant.PointFromHill != null)
                    if (ant.PointFromHill.Equals(ant))
                        ant.PointFromHill = null;
                for (int i = 0; i < 4; i++)
                    ant.EnemyHillDirections[i] = false;
                ant.AttackEnemyHill = false;
            }

            if (state.EnemyAnts.Count > 0)
                iSeeEnemy = true;

            #region Find hill defence

            foreach (var hill in state.Hills)
            {
                var sortedAnts = state.MyAnts.Where(a => hill.DistanceMap[a.X, a.Y] >= 0 && hill.DistanceMap[a.X, a.Y] <= 100).OrderBy(a => hill.DistanceMap[a.X, a.Y]).ToArray();
                if (sortedAnts.Length == 0)
                    continue;
                int index = 0;
                int pathLength = 2;
                do
                {
                    while (index < sortedAnts.Length && hill.DistanceMap[sortedAnts[index].X, sortedAnts[index].Y] <= pathLength)
                        index++;
                    int enemyCount = 0;
                    foreach (var enemyAnt in state.EnemyAnts)
                        if (hill.DistanceMap[enemyAnt.X, enemyAnt.Y] >= 0 && hill.DistanceMap[enemyAnt.X, enemyAnt.Y] <= pathLength)
                            enemyCount++;
                    if (enemyCount > 0)
                        if (enemyCount > index)
                            for (int i = 0; i < Math.Min(enemyCount + 1, sortedAnts.Length); i++)
                            {
                                if (sortedAnts[i].Defend != null && sortedAnts[i].DefendDistance <= hill.DistanceMap[sortedAnts[i].X, sortedAnts[i].Y])
                                {
                                    enemyCount++;
                                    continue;
                                }
                                sortedAnts[i].Defend = hill;
                                sortedAnts[i].DefendDistance = hill.DistanceMap[sortedAnts[i].X, sortedAnts[i].Y];
                            }
                        else
                            if (enemyCount == index)
                            {
                                int add = 1;
                                for (int i = 0; i < index + add; i++)
                                {
                                    if (i == sortedAnts.Length)
                                        break;
                                    if (sortedAnts[i].Defend != null && sortedAnts[i].DefendDistance <= hill.DistanceMap[sortedAnts[i].X, sortedAnts[i].Y])
                                    {
                                        add++;
                                        continue;
                                    }
                                    sortedAnts[i].Defend = hill;
                                    sortedAnts[i].DefendDistance = hill.DistanceMap[sortedAnts[i].X, sortedAnts[i].Y];
                                }
                            }
                    if (index == sortedAnts.Length)
                        break;
                    pathLength++;
                }
                while (pathLength < 20);
            }

            #endregion

            #region Find food correspondence

            var fIndex = new List<int>(Enumerable.Range(0, state.Foods.Count));
            var aIndex = new List<int>(Enumerable.Range(0, state.MyAnts.Count));
            for (int i = 0; i < state.MyAnts.Count; i++)
                if (state.MyAnts[i].Defend != null)
                    aIndex.Remove(i);

            while (fIndex.Count > 0 && aIndex.Count > 0)
            {
                int antIndex = -1;
                int foodIndex = -1;
                int minDistance = int.MaxValue;
                foreach (var fi in fIndex)
                    foreach (var ai in aIndex)
                    {
                        var distance = state.Foods[fi].DistanceMap[state.MyAnts[ai].X, state.MyAnts[ai].Y];
                        if (distance < minDistance && distance != -1)
                        {
                            minDistance = distance;
                            antIndex = ai;
                            foodIndex = fi;
                        }
                    }
                if (antIndex >= 0 && foodIndex >= 0)
                {
                    state.MyAnts[antIndex].Food = state.Foods[foodIndex];
                    state.MyAnts[antIndex].FoodDistance = minDistance;
                    fIndex.Remove(foodIndex);
                    aIndex.Remove(antIndex);
                }
            }

            #endregion

            #region Find hill correspondence

            if (state.EnemyHills.Count > 0)
                foreach (var ant in state.MyAnts)
                {
                    int index = -1;
                    int minDistance = int.MaxValue;
                    for (int i = 0; i < state.EnemyHills.Count; i++)
                        if (state.EnemyHills[i].DistanceMap[ant.X, ant.Y] != -1 &&
                            state.EnemyHills[i].DistanceMap[ant.X, ant.Y] < minDistance)
                        {
                            index = i;
                            minDistance = state.EnemyHills[i].DistanceMap[ant.X, ant.Y];
                        }
                    if (index >= 0)
                    {
                        ant.EnemyHill = state.EnemyHills[index];
                        ant.EnemyHillDistance = minDistance;
                        for (int i = 0; i < 4; i++)
                            if (ant.RightDirections[i])
                            {
                                var newLoc = ant + PathFinding.Directions[i];
                                if (ant.EnemyHill.DistanceMap[newLoc.X, newLoc.Y] != -1)
                                    if (ant.EnemyHill.DistanceMap[newLoc.X, newLoc.Y] < ant.EnemyHill.DistanceMap[ant.X, ant.Y])
                                        ant.EnemyHillDirections[i] = true;
                            }
                    }
                }

            List<MyAnt> antListT = new List<MyAnt>(state.MyAnts);
            for (int i = 0; i < state.MyAnts.Count; i++)
                if (state.MyAnts[i].EnemyHill == null)
                    antListT[i].EnemyHillDistance = int.MaxValue;
            antListT.Sort((a1, a2) => a1.EnemyHillDistance.CompareTo(a2.EnemyHillDistance));
            for (int i = antListT.Count / 4; i < antListT.Count; i++)
                antListT[i].EnemyHill = null;

            #endregion

            #region Exploration

            foreach (var ant in state.MyAnts)
                for (int i = 0; i < 4; i++)
                {
                    ant.Exploration[i].NewCellCount = 0;
                    ant.Exploration[i].VisibleCellCount = 0;
                    if (ant.RightDirections[i])
                    {
                        var newLoc = ant + PathFinding.Directions[i];
                        foreach (var p in state.MoveVision)
                        {
                            var pv = newLoc + p;
                            if (state.Map[pv.X, pv.Y] == Tile.Unseen)
                                ant.Exploration[i].NewCellCount++;
                            if (!state.VisibleMap[pv.X, pv.Y] && state.Map[pv.X, pv.Y] != Tile.Water)
                                ant.Exploration[i].VisibleCellCount++;
                        }
                    }
                }

            #endregion

            #region Move ants

            if (state.TurnNumber == 456)
            {
            }

            if (state.MyAnts.Count > state.Width * state.Height / 60)
            {
                bool change = true;
                while (change)
                {
                    change = false;
                    for (int j = 0; j < state.FinalAttack.Length; j++)
                        for (int i = 0; i < state.EnemyAnts.Count; i++)
                        {
                            var loc = state.EnemyAnts[i] + state.FinalAttack[j];
                            var index = state.MyAnts.BinarySearch(new MyAnt(loc));
                            if (index >= 0 && !state.MyAnts[index].Processed)
                            {
                                int dx = Math.Sign(state.FinalAttack[j].X);
                                int dy = Math.Sign(state.FinalAttack[j].Y);

                                List<int> order = new List<int> { 0, 1, 2, 3 };
                                order = order.OrderBy(dir =>
                                    {
                                        var nl = loc + PathFinding.Directions[dir];
                                        return (nl.X - state.EnemyAnts[i].X) * (nl.X - state.EnemyAnts[i].X) +
                                               (nl.Y - state.EnemyAnts[i].Y) * (nl.Y - state.EnemyAnts[i].Y);
                                    }).ToList();

                                #region Move

                                for (int rndCount = 0; rndCount < 4; rndCount++)
                                {
                                    int rndIndex = order[rndCount];
                                    if (dx == 1 && rndIndex == 1)
                                    {
                                        var newLoc = loc + PathFinding.Directions[1];
                                        if (state.MyAnts[index].RightDirections[1])
                                            if (!state.NextTurnPreview[newLoc.X, newLoc.Y])
                                            {
                                                state.MyAnts[index].Processed = true;
                                                state.MyAnts[index].Move = true;
                                                state.MyAnts[index].Direction = (Direction)1;
                                                state.NextTurnPreview[newLoc.X, newLoc.Y] = true;
                                                break;
                                            }
                                    }
                                    if (dx == -1 && rndIndex == 3)
                                    {
                                        var newLoc = loc + PathFinding.Directions[3];
                                        if (state.MyAnts[index].RightDirections[3])
                                            if (!state.NextTurnPreview[newLoc.X, newLoc.Y])
                                            {
                                                state.MyAnts[index].Processed = true;
                                                state.MyAnts[index].Move = true;
                                                state.MyAnts[index].Direction = (Direction)3;
                                                state.NextTurnPreview[newLoc.X, newLoc.Y] = true;
                                                break;
                                            }
                                    }
                                    if (dy == 1 && rndIndex == 0)
                                    {
                                        var newLoc = loc + PathFinding.Directions[0];
                                        if (state.MyAnts[index].RightDirections[0])
                                            if (!state.NextTurnPreview[newLoc.X, newLoc.Y])
                                            {
                                                state.MyAnts[index].Processed = true;
                                                state.MyAnts[index].Move = true;
                                                state.MyAnts[index].Direction = (Direction)0;
                                                state.NextTurnPreview[newLoc.X, newLoc.Y] = true;
                                                break;
                                            }
                                    }
                                    if (dy == -1 && rndIndex == 2)
                                    {
                                        var newLoc = loc + PathFinding.Directions[2];
                                        if (state.MyAnts[index].RightDirections[2])
                                            if (!state.NextTurnPreview[newLoc.X, newLoc.Y])
                                            {
                                                state.MyAnts[index].Processed = true;
                                                state.MyAnts[index].Move = true;
                                                state.MyAnts[index].Direction = (Direction)2;
                                                state.NextTurnPreview[newLoc.X, newLoc.Y] = true;
                                                break;
                                            }
                                    }
                                }

                                #endregion

                                if (state.MyAnts[index].Processed)
                                    change = true;
                            }
                        }
                }
            }

            while (true)
            {
                int index = 0;
                while (state.MyAnts[index].Processed)
                {
                    index++;
                    if (index == state.MyAnts.Count)
                        break;
                }
                if (index == state.MyAnts.Count)
                    break;
                if (!ProcessAnt(state.MyAnts[index], state, 0, null, null))
                {
                    state.MyAnts[index].Processed = true;
                    state.MyAnts[index].Move = false;
                    state.NextTurnPreview[state.MyAnts[index].X, state.MyAnts[index].Y] = true;
                }
                if (state.RemainMs < 75)
                    break;
            }

            #endregion

            if (UseDebugDraw)
                SendGameState(state);
        }

        bool ProcessAnt(MyAnt ant, GameState state, int depth, List<MyAnt> usedAnts, List<Location> usedLocs)
        {
            if (!ant.RightDirections[0] &&
                !ant.RightDirections[1] &&
                !ant.RightDirections[2] &&
                !ant.RightDirections[3])
            {
                int minAttack = int.MaxValue;
                int goodDir = -1;
                for (int dir = 0; dir < 4; dir++)
                {
                    var newLoc = ant + PathFinding.Directions[dir];
                    if (state.Map[newLoc.X, newLoc.Y] != Tile.Food && state.Map[newLoc.X, newLoc.Y] != Tile.Water)
                    {
                        int index = state.MyAnts.BinarySearch(new MyAnt(newLoc));
                        if (index >= 0 && state.MyAnts[index].Processed)
                            continue;
                        if (!state.NextTurnPreview[newLoc.X, newLoc.Y])
                        {
                            int att = state.GetAttackLoc(newLoc);
                            if (att < minAttack)
                            {
                                goodDir = dir;
                                minAttack = att;
                            }
                        }
                    }
                }
                if (minAttack != int.MaxValue)
                {
                    ant.Move = true;
                    ant.Direction = (Direction)goodDir;
                    ant.Processed = true;
                    var newLoc = ant + PathFinding.Directions[goodDir];
                    state.NextTurnPreview[newLoc.X, newLoc.Y] = true;
                    return true;
                }
                ant.Move = false;
                ant.Processed = true;
                state.NextTurnPreview[ant.X, ant.Y] = true;
                return false;
            }

            var possibleMoves = TryMove(ant, state);
            if (possibleMoves.Count == 0)
                return false;

            foreach (var md in possibleMoves)
                md.Order = rnd.Next(0, 1000);

            foreach (var md in possibleMoves.OrderBy(m => m.Order))
            {
                List<MyAnt> rUsedAnts = null;
                List<Location> rUsedLocs = null;
                bool result = true;
                ant.Processed = true;
                state.NextTurnPreview[md.X, md.Y] = true;
                var index = state.MyAnts.BinarySearch(new MyAnt(md));
                if (index >= 0)
                    if (!state.MyAnts[index].Processed)
                    {
                        rUsedAnts = new List<MyAnt>();
                        rUsedLocs = new List<Location>();
                        result = ProcessAnt(state.MyAnts[index], state, depth + 1, rUsedAnts, rUsedLocs);
                    }
                if (result)
                    if (!md.Critical || ant.FightDirections[(int)md.Direction] > 1)
                        if (!md.DeathForFood || (ant.FightDirections[(int)md.Direction] > 1 && state.Players == 2))
                            if (ant.FightDirections[(int)md.Direction] > 0)
                            {
                                result = CheckForFight(md, state, null, null);
                                if (!result && rUsedAnts != null && rUsedLocs != null)
                                {
                                    for (int i = 0; i < rUsedAnts.Count; i++)
                                        rUsedAnts[i].Processed = false;
                                    for (int i = 0; i < rUsedLocs.Count; i++)
                                        state.NextTurnPreview[rUsedLocs[i].X, rUsedLocs[i].Y] = false;
                                }
                            }
                if (!result)
                {
                    if (depth == 0)
                        ant.RightDirections[(int)md.Direction] = false;
                    ant.Processed = false;
                    ant.Move = false;
                    state.NextTurnPreview[md.X, md.Y] = false;
                }
                if (result)
                {
                    ant.Processed = true;
                    ant.Direction = md.Direction;
                    ant.Move = true;
                    ant.AttackEnemyHill = md.AttackEnemyHill;
                    if (usedAnts != null && usedLocs != null)
                    {
                        usedAnts.Add(ant);
                        usedLocs.Add(md);
                        if (rUsedAnts != null && rUsedLocs != null)
                        {
                            usedAnts.AddRange(rUsedAnts);
                            usedLocs.AddRange(rUsedLocs);
                        }
                    }
                    return true;
                }
            }

            if (depth == 0)
                return ProcessAnt(ant, state, depth, null, null);
            else
                return false;
        }

        bool CheckForFight(Location antLoc, GameState state, List<MyAnt> usedAnts, List<Location> usedLocs)
        {
            if (usedAnts == null)
                usedAnts = new List<MyAnt>();
            if (usedLocs == null)
                usedLocs = new List<Location>();
            List<MyAnt> rUsedAnts = null;
            List<Location> rUsedLocs = null;

            int myPower = 0;
            for (int dir = 0; dir < 8; dir++)
            {
                var newLoc = antLoc + PathFinding.Dir8[dir];
                if (state.Map[newLoc.X, newLoc.Y] == Tile.Water)
                    continue;
                // if attack zone here
                if (state.GetAttackLoc(newLoc) > 0)
                    // if there is ant here -> power ++
                    if (state.NextTurnPreview[newLoc.X, newLoc.Y])
                        myPower++;
                    else
                    {
                        // if not ant, find him
                        var index = state.MyAnts.BinarySearch(new MyAnt(newLoc));
                        bool findAnt = false;
                        if (index >= 0)
                            // if ant not moved
                            if (!state.MyAnts[index].Processed)
                            {
                                state.MyAnts[index].Processed = true;
                                state.MyAnts[index].Move = false;
                                state.NextTurnPreview[newLoc.X, newLoc.Y] = true;
                                rUsedAnts = new List<MyAnt>();
                                rUsedLocs = new List<Location>();
                                if (CheckForFight(newLoc, state, rUsedAnts, rUsedLocs))
                                {
                                    myPower++;
                                    usedAnts.Add(state.MyAnts[index]);
                                    usedLocs.Add(newLoc);
                                    usedAnts.AddRange(rUsedAnts);
                                    rUsedLocs.AddRange(rUsedLocs);
                                    findAnt = true;
                                }
                                else
                                {
                                    state.MyAnts[index].Processed = false;
                                    state.NextTurnPreview[newLoc.X, newLoc.Y] = false;
                                }
                            }
                        // try to move here neighbor ant
                        if (!findAnt)
                        {
                            for (int iDir = 0; iDir < 4; iDir++)
                            {
                                var neibLoc = newLoc + PathFinding.Directions[iDir];
                                if (state.Map[neibLoc.X, neibLoc.Y] == Tile.Water)
                                    continue;
                                index = state.MyAnts.BinarySearch(new MyAnt(neibLoc));
                                if (index >= 0 && !state.MyAnts[index].Processed)
                                {
                                    state.MyAnts[index].Processed = true;
                                    state.MyAnts[index].Move = true;
                                    state.MyAnts[index].Direction = ((Direction)iDir).Opposide();
                                    state.NextTurnPreview[newLoc.X, newLoc.Y] = true;
                                    rUsedAnts = new List<MyAnt>();
                                    rUsedLocs = new List<Location>();
                                    if (CheckForFight(newLoc, state, rUsedAnts, rUsedLocs))
                                    {
                                        myPower++;
                                        usedAnts.Add(state.MyAnts[index]);
                                        usedLocs.Add(newLoc);
                                        usedAnts.AddRange(rUsedAnts);
                                        rUsedLocs.AddRange(rUsedLocs);
                                        break;
                                    }
                                    else
                                    {
                                        state.MyAnts[index].Processed = false;
                                        state.NextTurnPreview[newLoc.X, newLoc.Y] = false;
                                    }
                                }
                            }
                        }
                    }
            }

            if (myPower < state.GetAttackLoc(antLoc))
            {
                for (int i = 0; i < usedAnts.Count; i++)
                    usedAnts[i].Processed = false;
                for (int i = 0; i < usedLocs.Count; i++)
                    state.NextTurnPreview[usedLocs[i].X, usedLocs[i].Y] = false;
                return false;
            }

            return true;
        }

        bool CheckForFightX(Location antLoc, GameState state)
        {
            //for (int i 

            return true;
        }

        List<MoveData> TryMove(MyAnt ant, GameState state)
        {
            List<MoveData> result;

            if (ant.Defend != null)
            {
                result = DefendHill(ant, state);
                for (int i = 0; i < result.Count; i++)
                    result[i].Critical = true;
                if (result.Count == 0)
                    return MoveRandom(ant, state);
                else
                    return result;
            }

            if (ant.EnemyHill != null && ant.EnemyHillDistance < 10)
            {
                result = MoveToEnemyHill(ant, state);
                if (result.Count > 0)
                    return result;
            }

            if (state.Hills.Count > 0)
                if (ant.Food != null)
                {
                    result = MoveToFood(ant, state);
                    if (ant.FoodDistance < 10)
                        for (int i = 0; i < result.Count; i++)
                            result[i].DeathForFood = true;
                    if (result.Count > 0)
                        return result;
                }

            if (!iSeeEnemy)
            {
                if (state.RemainMs > 150)
                    result = MoveFromHill(ant, state);
                else
                    result = MoveFromHillFast(ant, state);
                if (result.Count > 0)
                    return result;
            }

            result = MoveToExploreNew(ant, state);
            if (result.Count > 0)
                return result;

            if (!ant.GoToEnemyHill)
            {
                if (state.RemainMs > 150)
                    result = MoveFromHill(ant, state);
                else
                    result = MoveFromHillFast(ant, state);
                if (result.Count > 0)
                    return result;
            }

            if (ant.EnemyHill != null)
                {
                    result = MoveToEnemyHill(ant, state);
                    if (result.Count > 0)
                        return result;
                }

            result = SpecificMoveFromHill(ant, state);
            if (result.Count > 0)
                return result;

            result = MoveToExploreNV(ant, state);
            if (result.Count > 0)
                return result;

            result = MoveRandom(ant, state);

            return result;
        }

        List<MoveData> DefendHill(MyAnt ant, GameState state)
        {
            var result = new List<MoveData>();
            for (int i = 0; i < 4; i++)
                if (ant.RightDirections[i])
                {
                    var newLoc = ant + PathFinding.Directions[i];
                    if (ant.DefendDistance > ant.Defend.DistanceMap[newLoc.X, newLoc.Y] && ant.Defend.DistanceMap[newLoc.X, newLoc.Y] != -1 && !state.NextTurnPreview[newLoc.X, newLoc.Y])
                        result.Add(new MoveData(newLoc, (Direction)i));
                }
            return result;
        }

        List<MoveData> MoveToFood(MyAnt ant, GameState state)
        {
            var result = new List<MoveData>();
            for (int i = 0; i < 4; i++)
                if (ant.RightDirections[i])
                {
                    var newLoc = ant + PathFinding.Directions[i];
                    if (ant.Food.DistanceMap[newLoc.X, newLoc.Y] == ant.FoodDistance - 1 && !state.NextTurnPreview[newLoc.X, newLoc.Y])
                        result.Add(new MoveData(newLoc, (Direction)i));
                }
            return result;
        }

        List<MoveData> MoveToEnemyHill(MyAnt ant, GameState state)
        {
            var result = new List<MoveData>();
            for (int i = 0; i < 4; i++)
                if (ant.RightDirections[i])
                {
                    var newLoc = ant + PathFinding.Directions[i];
                    if (ant.EnemyHill.DistanceMap[newLoc.X, newLoc.Y] == ant.EnemyHillDistance - 1 && !state.NextTurnPreview[newLoc.X, newLoc.Y])
                        result.Add(new MoveData(newLoc, (Direction)i) { AttackEnemyHill = true });
                }
            return result;
        }

        List<MoveData> MoveToExploreNew(MyAnt ant, GameState state)
        {
            var result = new List<MoveData>();
            int maxExplore = 0;
            for (int i = 0; i < 4; i++)
                if (ant.RightDirections[i])
                {
                    var newLoc = ant + PathFinding.Directions[i];
                    if (ant.Exploration[i].NewCellCount > maxExplore && !state.NextTurnPreview[newLoc.X, newLoc.Y])
                        maxExplore = ant.Exploration[i].NewCellCount;
                }
            if (maxExplore > 0)
                for (int i = 0; i < 4; i++)
                    if (ant.RightDirections[i])
                    {
                        var newLoc = ant + PathFinding.Directions[i];
                        if (ant.Exploration[i].NewCellCount == maxExplore && !state.NextTurnPreview[newLoc.X, newLoc.Y])
                            result.Add(new MoveData(newLoc, (Direction)i));
                    }
            return result;
        }

        List<MoveData> MoveToExploreNV(MyAnt ant, GameState state)
        {
            var result = new List<MoveData>();
            int maxExplore = 0;
            for (int i = 0; i < 4; i++)
                if (ant.RightDirections[i])
                {
                    var newLoc = ant + PathFinding.Directions[i];
                    if (ant.Exploration[i].VisibleCellCount > maxExplore && !state.NextTurnPreview[newLoc.X, newLoc.Y])
                        maxExplore = ant.Exploration[i].VisibleCellCount;
                }
            if (maxExplore > 0)
                for (int i = 0; i < 4; i++)
                    if (ant.RightDirections[i])
                    {
                        var newLoc = ant + PathFinding.Directions[i];
                        if (ant.Exploration[i].VisibleCellCount == maxExplore && !state.NextTurnPreview[newLoc.X, newLoc.Y])
                            result.Add(new MoveData(newLoc, (Direction)i));
                    }
            return result;
        }

        List<MoveData> MoveFromHill(MyAnt ant, GameState state)
        {
            var result = new List<MoveData>();
            if (ant.Home == null)
                return result;
            if (ant.PointFromHill != null)
                return result;

            var fromHillDirections = new int[4];
            var stochastics = new int[4];
            var locs = new Location[4];
            var count = 0;
            for (int i = 0; i < 4; i++)
                if (ant.RightDirections[i])
                {
                    var newLoc = ant + PathFinding.Directions[i];
                    if (state.NextTurnPreview[newLoc.X, newLoc.Y])
                        continue;
                    if (ant.Home.DistanceMap[ant.X, ant.Y] < ant.Home.DistanceMap[newLoc.X, newLoc.Y] && ant.Home.DistanceMap[ant.X, ant.Y] != -1)
                    {
                        if (ant.Home.StochasticMap[newLoc.X, newLoc.Y] == 0)
                            ant.Home.StochasticMap[newLoc.X, newLoc.Y] = PathFinding.GetTotalLeafCount(newLoc, ant.Home);
                        fromHillDirections[count] = i;
                        stochastics[count] = ant.Home.StochasticMap[newLoc.X, newLoc.Y];
                        locs[count] = newLoc;
                        count++;
                    }
                }

            if (count == 0 || stochastics.Sum() < 20)
                return result;
            else
            {
                int r = rnd.Next(stochastics.Sum());
                int sum = 0;
                for (int i = 0; i < count; i++)
                {
                    if (r >= sum && r < sum + stochastics[i])
                        return new List<MoveData> { new MoveData(locs[i], (Direction)fromHillDirections[i]) };
                    sum += stochastics[i];
                }
                return result;
            }
        }

        List<MoveData> MoveFromHillFast(MyAnt ant, GameState state)
        {
            var result = new List<MoveData>();
            if (ant.PointFromHill != null)
                return result;

            var fromHillDirections = new bool[4];
            for (int i = 0; i < 4; i++)
                if (ant.RightDirections[i])
                {
                    var newLoc = ant + PathFinding.Directions[i];
                    if (state.NextTurnPreview[newLoc.X, newLoc.Y])
                        continue;
                    if (ant.Home.DistanceMap[ant.X, ant.Y] <= ant.Home.DistanceMap[newLoc.X, newLoc.Y] && ant.Home.DistanceMap[ant.X, ant.Y] != -1)
                        result.Add(new MoveData(newLoc, (Direction)i));
                }
            return result;
        }

        List<MoveData> SpecificMoveFromHill(MyAnt ant, GameState state)
        {
            if (ant.Home == null)
                return new List<MoveData>();

            if (ant.PointFromHill != null)
            {
                var result = new List<MoveData>();
                for (int i = 0; i < 4; i++)
                    if (ant.RightDirections[i])
                    {
                        var newLoc = ant + PathFinding.Directions[i];
                        if (!state.NextTurnPreview[newLoc.X, newLoc.Y])
                            if (ant.SpecificPath[newLoc.X, newLoc.Y] != -1 && ant.SpecificPath[newLoc.X, newLoc.Y] < ant.SpecificPath[ant.X, ant.Y])
                                result.Add(new MoveData(newLoc, (Direction)i));
                    }
                return result;
            }

            int radius = Math.Min(30, Math.Min(state.Width - 1, state.Height - 1));
            do
            {
                var d = new Location();
                for (int dCoo = 0; dCoo < radius; dCoo++)
                    for (int k = 0; k < 4; k++)
                    {
                        d.X = dCoo * (k / 2 == 0 ? -1 : 1);
                        d.Y = (radius - dCoo) * (k % 2 == 0 ? -1 : 1);
                        var loc = ant + d;
                        if (!state.NextTurnPreview[loc.X, loc.Y])
                            if (ant.Home.DistanceMap[loc.X, loc.Y] > ant.Home.DistanceMap[ant.X, ant.Y])
                            {
                                if (ant.Home.StochasticMap[loc.X, loc.Y] == 0)
                                    ant.Home.StochasticMap[loc.X, loc.Y] = PathFinding.GetTotalLeafCount(loc, ant.Home);
                                if (ant.Home.StochasticMap[loc.X, loc.Y] < 20)
                                    continue;
                                
                                ant.PointFromHill = loc;
                                if (ant.SpecificPath == null)
                                    ant.SpecificPath = new int[state.Width, state.Height];
                                PathFinding.FillArray(loc, ant.SpecificPath, ant);

                                var result = new List<MoveData>();
                                for (int i = 0; i < 4; i++)
                                    if (ant.RightDirections[i])
                                    {
                                        var newLoc = ant + PathFinding.Directions[i];
                                        if (!state.NextTurnPreview[newLoc.X, newLoc.Y])
                                            if (ant.SpecificPath[newLoc.X, newLoc.Y] != -1 && ant.SpecificPath[newLoc.X, newLoc.Y] < ant.SpecificPath[ant.X, ant.Y])
                                                result.Add(new MoveData(newLoc, (Direction)i));
                                    }
                                return result;
                            }
                    }
                radius++;
            }
            while (radius < state.Width && radius < state.Height);
            return new List<MoveData>();
        }

        List<MoveData> MoveRandom(MyAnt ant, GameState state)
        {
            var result = new List<MoveData>();
            for (int i = 0; i < 4; i++)
                if (ant.RightDirections[i])
                {
                    var newLoc = ant + PathFinding.Directions[i];
                    if (!state.NextTurnPreview[newLoc.X, newLoc.Y])
                        result.Add(new MoveData(newLoc, (Direction)i));
                }
            return result;
        }

        public void InitRandom(long seed)
        {
            rnd = new Random((int)(seed % int.MaxValue));
        }

        partial void SendGameState(GameState state);

        static void Main(string[] args)
        {
            //System.Threading.Thread.Sleep(1000);
            Instance.UseDebugDraw = false;
            Instance.SaveInputData = false;
            Shell.PlayGame(Instance);
        }
    }
}