﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TheRockPaperScissors.Client.Game.Enums;

namespace TheRockPaperScissors.Client.Services
{
    public interface IMoveService
    {
        public Task MakeMove(Guid token, Move move);
    }
}
