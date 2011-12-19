using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Ants
{
    static class Shell
    {
        private const string READY = "ready";
        private const string GO = "go";
        private const string END = "end";

        public static void PlayGame(Bot bot)
        {
            List<string> input = new List<string>();

            StreamWriter writer = null;
            if (Bot.Instance.SaveInputData)
                writer = File.CreateText("input.txt");

            while (true)
            {
                string line = ReadLine();

                if (Bot.Instance.SaveInputData)
                    writer.WriteLine(line);

                if (line.Equals(READY))
                {
                    ParseSetup(input);
                    FinishTurn();
                    input.Clear();
                }
                else if (line.Equals(GO))
                {
                    GameState.Instance.StartNewTurn();
                    ParseUpdate(input);
                    GameState.Instance.EndTurnInput();
                    bot.DoTurn(GameState.Instance);
                    FinishTurn();
                    input.Clear();
                }
                else if (line.Equals(END))
                    break;
                else
                    input.Add(line);
            }

            if (Bot.Instance.SaveInputData)
                writer.Close();
        }

        static void ParseSetup(List<string> input)
        {
            int width = 0, height = 0;
            int turntime = 0, loadtime = 0, players = 0;
            int viewradius2 = 0, attackradius2 = 0, spawnradius2 = 0;
            long seed = 0;

            foreach (string line in input)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                string[] tokens = line.Split();
                string key = tokens[0];

                if (key.Equals(@"cols"))
                    width = int.Parse(tokens[1]);
                else if (key.Equals(@"rows"))
                    height = int.Parse(tokens[1]);
                else if (key.Equals(@"player_seed"))
                    seed = long.Parse(tokens[1]);
                else if (key.Equals(@"turntime"))
                    turntime = int.Parse(tokens[1]);
                else if (key.Equals(@"loadtime"))
                    loadtime = int.Parse(tokens[1]);
                else if (key.Equals(@"viewradius2"))
                    viewradius2 = int.Parse(tokens[1]);
                else if (key.Equals(@"attackradius2"))
                    attackradius2 = int.Parse(tokens[1]);
                else if (key.Equals(@"spawnradius2"))
                    spawnradius2 = int.Parse(tokens[1]);
                else if (key.Equals(@"players"))
                    players = int.Parse(tokens[1]);
            }

            Bot.Instance.InitRandom(seed);
            GameState.Instance.Init(width, height, turntime, loadtime, viewradius2, attackradius2, spawnradius2, players);
        }

        static void ParseUpdate(List<string> input)
        {
            foreach (string line in input)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                string[] tokens = line.Split();

                if (tokens.Length >= 3)
                {
                    int row = int.Parse(tokens[1]);
                    int col = int.Parse(tokens[2]);

                    if (tokens[0].Equals("a"))
                        GameState.Instance.AddAnt(col, row, int.Parse(tokens[3]));
                    else if (tokens[0].Equals("f"))
                        GameState.Instance.AddFood(col, row);
                    else if (tokens[0].Equals("r"))
                        GameState.Instance.RemoveFood(col, row);
                    else if (tokens[0].Equals("w"))
                        GameState.Instance.AddWater(col, row);
                    else if (tokens[0].Equals("d"))
                        GameState.Instance.AddDeadAnt(col, row);
                    else if (tokens[0].Equals("h"))
                        GameState.Instance.AddHill(col, row, int.Parse(tokens[3]));
                }
            }
        }

        static void FinishTurn()
        {
            GameState.Instance.EndTurn();
            WriteLine(GO);
        }

        public static void CommitStep(Location loc, Direction dir)
        {
            WriteLine(string.Format("o {0} {1} {2}", loc.Y, loc.X, dir.ToChar()));
        }

        static string ReadLine()
        {
            return Console.In.ReadLine();
        }

        static void WriteLine(string s)
        {
            Console.Out.WriteLine(s);
        }
    }
}