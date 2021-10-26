using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
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

        public Worker(ILogger<Worker> logger, IOptions<PuzzleContractConfig> puzzleOptions,
            IConfiguration config)
        {
            this.logger = logger;
            this.puzzleConfig = puzzleOptions.Value;
            this.infuraEndpoint = config.GetValue<string>("INFURA_ENDPOINT");

            var key = config.GetValue<string>("PRIVATE_KEY");
            var account = new Account(key);
            web3 = new Web3(account, infuraEndpoint);
            puzzleContract = web3.Eth.GetContract(puzzleConfig.ABI, puzzleConfig.Address);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var isPaused = await puzzleContract.GetFunction("isPaused").CallAsync<bool>();
                logger.LogInformation($"Puzzle state: " + (isPaused ? "PAUSED" : "RUNNING"));
                await Task.Delay(30000, stoppingToken);
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
    }
}
