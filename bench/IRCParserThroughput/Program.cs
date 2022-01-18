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
            long count = 0, maxTime = 0, minTime = 0, time = 0, total = 0,
                 maxNr = 0, minNr = 0;
            ReadResult result;
            string min = string.Empty, max = string.Empty;
            SequencePosition examined;

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

                //warm-up (guarantee first message is less than 128B)
                if(count == 0)
                {
                    if(IRCParser.TryParse
                    (
                        in sequence,
                        out message,
                        out _
                    ))
                    {
                        message.Dispose();
                    }
                }

                stopWatch.Start();
                if(IRCParser.TryParse
                (
                    in sequence,
                    out message,
                    out examined
                ))
                {
                    stopWatch.Stop();

                    using(message)
                    {
                        //Console.WriteLine(message.ToUtf8String().ToString());

                        time = stopWatch.ElapsedTicks;
                        total += time;
                        count++;

                        if(maxTime == 0
                            || time > maxTime)
                        {
                            maxTime = time;
                            max = message.ToUtf8String().ToString();
                            maxNr = count;
                        }

                        if(minTime == 0
                        || time < minTime)
                        {
                            minTime = time;
                            min = message.ToUtf8String().ToString();
                            minNr = count;
                        }

                        reader.AdvanceTo(examined);
                    }
                }
                else
                {
                    reader.AdvanceTo(sequence.Start, examined);
                }

                stopWatch.Reset();
            }

            reader.Complete();

            Console.WriteLine($"{count} messages processed");
            Console.WriteLine($"Average time: {total / count * 1000000 / Stopwatch.Frequency}µs");
            Console.WriteLine($"Max time: {maxTime * 1000000 / Stopwatch.Frequency}µs (#{maxNr} {max})");
            Console.WriteLine($"Min time: {minTime * 1000000 / Stopwatch.Frequency}µs (#{minNr} {min})");
            Console.WriteLine($"Total time: {total * 1000 / Stopwatch.Frequency}ms");
            Console.WriteLine($"Throughput: {count / (total / Stopwatch.Frequency)}msg/s");
        }
    }
}
