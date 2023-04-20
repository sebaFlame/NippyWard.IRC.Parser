using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using NippyWard.Text;
using NippyWard.IRC.Parser;
using NippyWard.IRC.Parser.Tokens;

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
            Stream stream = File.OpenRead(_Filename);

            using MemoryStream memory = new MemoryStream();
            byte[] buffer = new byte[8192];
            int length = 0;

            //first read the log completely into memory to reduce latency
            using (stream)
            {
                while ((length = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await memory.WriteAsync(buffer, 0, length);
                }
            }

            //reset position
            memory.Position = 0;

            PipeReader reader = PipeReader.Create
            (
                memory,
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

            try
            {
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
                    //also creates token pool
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
                                max = message.ToUtf8String().ToString().TrimEnd();
                                maxNr = count;
                            }

                            if(minTime == 0
                            || time < minTime)
                            {
                                minTime = time;
                                min = message.ToUtf8String().ToString().TrimEnd();
                                minNr = count;
                            }

                            //message has been processed
                            reader.AdvanceTo(examined);
                        }
                    }
                    else
                    {
                        //ensure more date becomes available
                        reader.AdvanceTo(sequence.Start, examined);
                    }

                    stopWatch.Reset();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            reader.Complete();

            Console.WriteLine($"{count} messages processed");

            if(count > 0)
            {
                Console.WriteLine($"Average time: {total / count * 1000000 / Stopwatch.Frequency}µs");
                Console.WriteLine($"Max time: {maxTime * 1000000 / Stopwatch.Frequency}µs (#{maxNr} {max})");
                Console.WriteLine($"Min time: {minTime * 1000000 / Stopwatch.Frequency}µs (#{minNr} {min})");
                Console.WriteLine($"Total time: {total * 1000 / Stopwatch.Frequency}ms");
                Console.WriteLine($"Throughput: {count / (total * 1000.0 / Stopwatch.Frequency) * 1000:0}msg/s");
                Console.WriteLine($"Max pool size: {Token.PooledTokens}");
            }
        }
    }
}
