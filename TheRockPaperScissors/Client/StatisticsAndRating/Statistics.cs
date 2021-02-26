﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TheRockPaperScissors.Client.Menu;
using TheRockPaperScissors.Client.Services;

namespace TheRockPaperScissors.Client.StatisticsAndRating
{
    public class Statistics
    {
        private readonly StatisticsService _statisticsService = new StatisticsService();
        private readonly MenuDesign _menuDesign = new MenuDesign();

        public async Task LoadStatistics(string login)
        {
            Console.Clear();
            _menuDesign.WriteHeader("statistics");
            string statistics = await _statisticsService.GetStatistics(login);
            string[] stats = statistics.Replace("\"", "").Split("|");
            string[] headers = new string[] { "Name", "Wins", "Draws", "Loses", "Rock", "Paper", "Scissors" };

            _menuDesign.WriteInColor(" " + stats[0] + "\n", ConsoleColor.Cyan);
            for (int i = 1; i < stats.Length - 1; i++)
                Console.WriteLine( " " + headers[i] + " " + stats[i]);

            _menuDesign.WriteInColor(" Press any key to go back >> ", ConsoleColor.Cyan);
            Console.ReadKey();
        }

        public async Task LoadRating()
        {
            Console.Clear();
            _menuDesign.WriteHeader("rating");
            string rating = await _statisticsService.GetRating();
            Console.WriteLine(rating);
            _menuDesign.WriteInColor(" Press any key to go back >> ", ConsoleColor.Cyan);
            Console.ReadKey();
        }
    }
}
