using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using System.Numerics;

namespace PuzzleWorker
{
    [Function("setNewAnswer")]
    public class NewAnswerFunction : FunctionMessage
    {
        [Parameter("string", "word", 1)]
        public string Answer { get; set; }

        [Parameter("uint64", "id", 2)]
        public BigInteger ID { get; set; }
    }
}
