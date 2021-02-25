﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SQLite;
using TheRockPaperScissors.Server.Models;
using System.IO;

namespace TheRockPaperScissors.Server.Data
{
    public class Database
    {
        private static readonly string _dbName = "Client.db";
        private readonly string _connectionString = "Data Source = " + AppDomain.CurrentDomain.BaseDirectory + _dbName;

        public Database()
        {
            if (!File.Exists(_dbName))
            {
                Console.WriteLine("SOZDANIE BD");
                SQLiteConnection.CreateFile(_dbName);
                CreateTable();
            }
        }

        public void AddUser(User user)
        {
            using var connection = new SQLiteConnection(_connectionString);
            var commandString = "insert into [users]([login], [password]) " +
                "values(@login, @password)";
            using var command = new SQLiteCommand(commandString, connection);
            command.Parameters.AddWithValue("@login", user.Login);
            command.Parameters.AddWithValue("@password", user.Password);
            connection.Open();
            command.ExecuteNonQueryAsync();
            connection.Close();
        }

        public User GetUser(string login)
        {
            try
            {
                var table = new DataTable();
                var adapter = new SQLiteDataAdapter(
                    $@"select * from [users] where login='{login}'",
                    _connectionString);
                adapter.Fill(table);
                var row = table.AsEnumerable().FirstOrDefault() ?? throw new NullReferenceException();
                var user = new User()
                {
                    Login = row.Field<string>("login"),
                    Password = row.Field<string>("password")
                };
                return user;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string GetPublicStatistics()
        {
            var table = new DataTable();
            var adapter = new SQLiteDataAdapter(
                    @"select * from users order by wins descending 
                    where (wins + loses + draws) > 10;",
                    _connectionString);
            try
            {
                adapter.Fill(table);
            }
            catch (Exception)
            {
                return null;
            }
            return string.Join("| ",
                table.AsEnumerable().Select(row =>
                    row.Field<string>("login").PadRight(16, ' ') + " " + 
                    row.Field<int>("wins").ToString().PadRight(8, ' ') + " " +
                    row.Field<int>("loses").ToString()));
        }

        public string GetUserStatistics(string login)
        {
            var table = new DataTable();
            var adapter = new SQLiteDataAdapter(
                    @$"select * from users where login='{login}'",
                    _connectionString);
            try
            {
                adapter.Fill(table);
            }
            catch (Exception)
            {
                return null;
            }
            return string.Join("| ", table.AsEnumerable().First().ItemArray.Skip(2));
        }

        public IList<User> GetAllUsers()
        {
            IList<User> users = new List<User>();
            var table = new DataTable();
            var adapter = new SQLiteDataAdapter("select * from [users]", _connectionString);
            try
            {
                adapter.Fill(table);
            }
            catch (Exception)
            {
                return users;
            }
            table.AsEnumerable().ToList().ForEach(row =>
                users.Add(new User()
                {
                    Login = row.Field<string>("login"),
                    Password = row.Field<string>("password"),
                    Statistics = new Statistics()
                    {
                        Wins = row.Field<int>("wins"),
                        Loses = row.Field<int>("loses"),
                        Draws = row.Field<int>("draws"),
                        Rock = row.Field<int>("rock"),
                        Paper = row.Field<int>("paper"),
                        Scissors = row.Field<int>("scissors"),
                        Time = row.Field<string>("time")
                    }
                }));
            return users;
        }

        public void UpdateUser(User user)
        {
            using var connection = new SQLiteConnection()
            {
                ConnectionString = _connectionString,
            };
            connection.Open();
            using var command = new SQLiteCommand(connection)
            {
                CommandText =
                @$"update users set
                wins = {user.Statistics.Wins},
                loses = {user.Statistics.Loses},
                draws = {user.Statistics.Draws},
                rock = {user.Statistics.Rock},
                paper = {user.Statistics.Paper},
                scissors = {user.Statistics.Scissors},
                time = {user.Statistics.Time} 
                where login='{user.Login}';"
            };
            command.ExecuteNonQuery();
            connection.Close();
        }

        private void CreateTable()
        {
            using var connection = new SQLiteConnection(_connectionString);

            using var command = new SQLiteCommand(connection)
            {
                CommandText = @"create table [users](
                [id] integer primary key autoincrement not null,
                [login] char(100) not null,
                [password] char(100) not null,
                [wins] integer default 0,
                [loses] integer default 0,
                [draws] integer default 0,
                [rock] integer default 0,
                [paper] integer default 0,
                [scissors] integer default 0,
                [time] char(100) default null);",
                CommandType = CommandType.Text
            };
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }
    }
}