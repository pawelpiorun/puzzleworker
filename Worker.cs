using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PuzzleWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly PuzzleContractConfig puzzleConfig;
        private readonly string infuraEndpoint;
        private readonly Web3 web3;
        private readonly Contract puzzleContract;
        private readonly Random rand = new Random();
        private readonly string managerKey;

        public Worker(ILogger<Worker> logger, IOptions<PuzzleContractConfig> puzzleOptions,
            IConfiguration config)
        {
            this.logger = logger;
            this.puzzleConfig = puzzleOptions.Value;
            this.infuraEndpoint = config.GetValue<string>("INFURA_ENDPOINT");

            var key = config.GetValue<string>("PRIVATE_KEY");
            var account = new Account(key);
            managerKey = account.PublicKey;
            web3 = new Web3(account, infuraEndpoint);

            puzzleContract = web3.Eth.GetContract(puzzleConfig.ABI, puzzleConfig.Address);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var isPaused = await puzzleContract.GetFunction("isPaused").CallAsync<bool>();
                    logger.LogInformation($"Puzzle state: " + (isPaused ? "PAUSED" : "RUNNING"));

                    if (isPaused)
                    {
                        var next = rand.Next() % 10;
                        var newPuzzle = puzzles.ElementAt(next);
                        logger.LogInformation($"Starting new puzzle ID: {newPuzzle.Key}");

                        var newPrize = rand.Next() % 50 / (double) 1000;
                        if (newPrize < 0.001) newPrize = 0.001d;
                        var newPrizeWei = Web3.Convert.ToWei(newPrize, Nethereum.Util.UnitConversion.EthUnit.Ether);

                        var func = puzzleContract.GetFunction("setNewAnswer");
                        var input = func.CreateTransactionInput(managerKey, newPuzzle.Value, newPuzzle.Key);
                        var transactionHandler = web3.Eth.GetContractTransactionHandler<NewAnswerFunction>();

                        var newAnswer = new NewAnswerFunction() { Answer = newPuzzle.Value, ID = newPuzzle.Key, AmountToSend = newPrizeWei };
                        newAnswer.Gas = 1000000;
                        newAnswer.GasPrice = Web3.Convert.ToWei("50", Nethereum.Util.UnitConversion.EthUnit.Gwei);

                        await transactionHandler.SendRequestAndWaitForReceiptAsync(puzzleConfig.Address, newAnswer);

                        logger.LogDebug($"{newPuzzle.Key}: {newPuzzle.Value}");
                    }
                }
                catch (Exception x)
                {
                    logger.LogCritical(x, null);
                }
                await Task.Delay(20000, stoppingToken);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Worker starting", DateTimeOffset.Now);
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Worker stopping", DateTimeOffset.Now);
            return base.StopAsync(cancellationToken);
        }

        private static Dictionary<int, string> puzzles => new Dictionary<int, string>()
        {
            { 239, "573" },
            { 218, "8" },
            { 247, "342" },
            { 226, "81" },
            { 252, "6" },
            { 65, "11" },
            { 255, "24" },
            { 170, "733" },
            { 185, "3679" },
            { 127, "90" }
        };
    }
}
