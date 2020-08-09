﻿using HouseofCat.RabbitMQ;
using HouseofCat.RabbitMQ.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Examples.RabbitMQ.DataProducer
{
    public static class Program
    {
        private static IRabbitService _rabbitService;

        public static long GlobalCount = 100_000;
        public static LogLevel LogLevel = LogLevel.Information;

        public static async Task Main()
        {
            await Console.Out.WriteLineAsync("Run a DataProducer demo... press any key to continue!").ConfigureAwait(false);
            Console.ReadKey(); // memory snapshot baseline

            // Create RabbitService and stage the messages in the queue
            await Console.Out.WriteLineAsync("Setting up RabbitService...").ConfigureAwait(false);
            await SetupAsync().ConfigureAwait(false);

            await Console.Out.WriteLineAsync("Making messages!").ConfigureAwait(false);
            await MakeDataAsync().ConfigureAwait(false);

            await Console.Out.WriteLineAsync("Finished publshing!").ConfigureAwait(false);
            Console.ReadKey(); // checking for memory leak after publishing (snapshots)

            await Console.Out.WriteLineAsync("Shutting down...").ConfigureAwait(false);
            await _rabbitService.ShutdownAsync(false);

            await Console.Out.WriteLineAsync("Shutdown!").ConfigureAwait(false);
            Console.ReadKey(); // checking for memory leak after shutdown (snapshots)
        }

        private static async Task SetupAsync()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(Program.LogLevel));

            _rabbitService = new RabbitService(
                "Config.json",
                "passwordforencryption",
                "saltforencryption",
                loggerFactory);

            await _rabbitService
                .Topologer
                .CreateQueueAsync("TestRabbitServiceQueue")
                .ConfigureAwait(false);
        }

        private static async Task MakeDataAsync()
        {
            var letterTemplate = new Letter("", "TestRabbitServiceQueue", null, new LetterMetadata());

            for (var i = 0L; i < GlobalCount; i++)
            {
                var letter = letterTemplate.Clone();
                letter.Body = JsonSerializer.SerializeToUtf8Bytes(new Message { StringMessage = $"Sensitive ReceivedLetter {i}", MessageId = i });
                letter.LetterId = (ulong)i;
                await _rabbitService
                    .Publisher
                    .PublishAsync(letter, true, true)
                    .ConfigureAwait(false);
            }
        }

        public class Message
        {
            public long MessageId { get; set; }
            public string StringMessage { get; set; }
        }
    }
}