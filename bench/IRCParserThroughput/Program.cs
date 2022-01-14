using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using ThePlague.Model.Core.Text;
using ThePlague.IRC.Parser;
using ThePlague.IRC.Parser.Tokens;

namespace IRCParserThroughput
{
    public class Program
    {
        private static string _Filename = "raw.log";

        static async Task Main(string[] args)
        {
#if DEBUG
            Console.WriteLine($"Please attach a debugger to process {Environment.ProcessId}...");
            while(!Debugger.IsAttached)
            {
                Thread.Sleep(100);
            }
#endif

            Stopwatch stopWatch = new Stopwatch();
            using Stream stream = File.OpenRead(_Filename);
            PipeReader reader = PipeReader.Create
            (
                stream,
                new StreamPipeReaderOptions(null, 512, 128, true)
            );

            ValueTask<ReadResult> vt;
            ReadOnlySequence<byte> sequence;
            Token message;
            long averageTime = 0, maxTime = 0, minTime = 0, time = 0, total = 0;
            ReadResult result;
            string min = string.Empty, max = string.Empty;

            while(true)
            {
                if(!reader.TryRead(out result))
                {
                    vt = reader.ReadAsync();

                    if(!vt.IsCompletedSuccessfully)
                    {
                        result = await vt;
                    }
                    else
                    {
                        result = vt.Result;
                    }
                }

                if(result.IsCompleted)
                {
                    break;
                }

                sequence = result.Buffer;

                stopWatch.Start();
                if(IRCParser.TryParse
                (
                    in sequence,
                    out message
                ))
                {
                    stopWatch.Stop();

                    //Console.WriteLine(message.ToUtf8String().ToString());

                    time = stopWatch.ElapsedTicks;
                    total += time;

                    if(averageTime == 0)
                    {
                        averageTime = time;
                    }
                    else
                    {
                        averageTime += time;
                        averageTime /= 2;
                    }

                    if(maxTime == 0
                        || time > maxTime)
                    {
                        maxTime = time;
                        max = message.ToUtf8String().ToString();
                    }

                    if(minTime == 0
                       || time < minTime)
                    {
                        minTime = time;
                        min = message.ToUtf8String().ToString();
                    }

                    reader.AdvanceTo(message.Sequence.End);
                }
                else
                {
                    reader.AdvanceTo(sequence.Start, sequence.End);
                }

                stopWatch.Reset();
            }

            reader.Complete();

            Console.WriteLine($"Average: {averageTime * 1000000 / Stopwatch.Frequency}µs");
            Console.WriteLine($"Max: {maxTime * 1000000 / Stopwatch.Frequency}µs ({max})");
            Console.WriteLine($"Min: {minTime * 1000000 / Stopwatch.Frequency}µs ({min})");
            Console.WriteLine($"Total: {total * 1000 / Stopwatch.Frequency}ms");
        }
    }
}
