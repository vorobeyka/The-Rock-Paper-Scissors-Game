﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TheRockPaperScissors.Server.Enums;
using TheRockPaperScissors.Server.Exceptions;
using TheRockPaperScissors.Server.Models;
using TheRockPaperScissors.Server.Services;

namespace TheRockPaperScissors.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GameController : ControllerBase
    {
        private readonly ILogger<GameController> _logger;
        private readonly ISeriesStorage _seriesStorage;
        private readonly IUsersStorage _users;

        public GameController(
            ISeriesStorage seriesStorage,
            IUsersStorage users,
            ILogger<GameController> logger)
        {
            _seriesStorage = seriesStorage;
            _users = users;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> Game(
            [FromServices] ISeriesService series,
            [FromBody] Game game)
        {
            var userId = Guid.Parse(game.UserId);

            _logger.LogInformation($"Connected to game user with id {userId}");
            if (!await _users.ContainAsync(userId)) return NotFound($"Not found user with token {userId}");
            
            var openSeries = await _seriesStorage.GetAsync(storage =>
                storage.FirstOrDefault(s => s.SecondId == null && s.Type == game.Type && s.GameId == game.GameId));
            var foundSeries = openSeries != null;

            if (!foundSeries) 
                openSeries = series;

            try
            {
                openSeries.SetProperties(game);
            }
            catch (SeriesException)
            {
                _logger.LogInformation($"User with id {userId} can't connect to game");
                return BadRequest("Invalid game id or game have maximum users");
            }

            if (!foundSeries) 
                await _seriesStorage.AddAsync(openSeries);

            return Ok(openSeries.GameId);
        }

        [HttpGet("start/{token}")]
        public async Task<ActionResult> Start([FromRoute(Name = "token")] string token)
        {
            _logger.LogInformation($"User with id {token} start's series");
            var id = Guid.Parse(token);
            var series = await _seriesStorage.GetAsync(storage =>
                storage.FirstOrDefault(series => series.IsRegisteredId(id)));

            var time = 0;

            while (series.SecondId == null && time < 300)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                time++;
            }

            if (time == 300) return NotFound("Time is out! No one connected to you");
            _logger.LogInformation($"Start {series.Type} series");
            return Ok(true);
        }

        [HttpPost("round")]
        public async Task<ActionResult> Round(
            [FromBody] Round round,
            [FromServices] IRoundService roundService)
        {
            var id = Guid.Parse(round.Id);
            _logger.LogInformation($"User with id {id} connected to round");
            var series = await _seriesStorage.GetByIdAsync(id);
            var openRound = await series.GetOpenRoundAsync() ?? await series.AddRoundAsync(roundService);

            if (!openRound.AddMove(id, round.Move)) 
                return BadRequest("Can't add move");

            return Ok();
        }

        [HttpGet("roundResult/{token}")]
        public async Task<ActionResult<string>> GetRoundResult([FromRoute(Name = "token")] string token)
        {
            var id = Guid.Parse(token);
            _logger.LogInformation($"User with id {id} gets the result of round");
            var game = await _seriesStorage.GetByIdAsync(id);
            var round = await game.GetLastRoundAsync();
            var user = await _users.GetAsync(id);
            var result = await round.GetResultAsync(id, user.Statistics, game.Type);

            if (string.IsNullOrEmpty(result) ||
                game.Timer.IsOutTime() ||
                round.Timer.IsOutTime() ||
                (game.Type == GameType.Training && game.RoundCount == 3))
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet("seriesResult/{token}")]
        public async Task<ActionResult<string>> GetSeriesResult(
            [FromRoute(Name = "token")] string token,
            [FromServices] IDatabaseService databaseService)
        {
            var id = Guid.Parse(token);
            _logger.LogInformation($"User with id {token} gets the result of series");
            var series = await _seriesStorage.GetByIdAsync(id);
            var user = await _users.GetAsync(id);
            var result = series.GetResult(id);

            if (series.Type != GameType.Training)
            {
                user.Statistics.UpdateTime(series.Timer.GetTime());
                await databaseService.UpdateUserAsync(user);
            }

            if (id == series.FirstId) 
                series.FirstId = null;
            else 
                series.SecondId = null;

            if (series.FirstId == null && series.SecondId == null
                || series.Type == GameType.Training)
            {
                await _seriesStorage.RemoveAsync(series);
            }

            return Ok(result);
        }
    }
}
