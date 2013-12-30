using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inedo.BmBuildLogger
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: {0} <msbuild.exe> [msbuild args]", Path.GetFileName(typeof(Program).Assembly.Location));
                return 1;
            }

            var pipeName = Guid.NewGuid().ToString("N");
            using (var pipeStream = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                string arguments = string.Join(" ", args.Skip(1).Select(EscapeArgument)) + " \"/logger:" + typeof(Program).Assembly.Location + ";" + pipeName + "\"";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = args[0],
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    }
                };

                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();

                var waitForConnectionTask = Task.Factory.FromAsync(pipeStream.BeginWaitForConnection, pipeStream.EndWaitForConnection, null);

                Console.WriteLine("Starting process: {0} {1}", args[0], arguments);

                process.Start();
                var processTask = Task.Factory.StartNew(
                    () =>
                    {
                        Task.Factory.StartNew(() => ReadProcessData(process.StandardError, stdErrBuffer), TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent);
                        Task.Factory.StartNew(() => ReadProcessData(process.StandardOutput, stdOutBuffer), TaskCreationOptions.LongRunning | TaskCreationOptions.AttachedToParent);
                    },
                    TaskCreationOptions.LongRunning
                );

                int index = Task.WaitAny(new[] { processTask, waitForConnectionTask }, 5000);
                if (index == 0)
                {
                    // process terminated before pipe connection
                    Console.Write(stdOutBuffer.ToString());
                    Console.Error.Write(stdErrBuffer.ToString());
                    return process.ExitCode;
                }
                else if (index == 1)
                {
                    // pipe connected
                    var reader = new BinaryReader(pipeStream, Encoding.UTF8);
                    try
                    {
                        while (true)
                        {
                            byte logLevel = reader.ReadByte();
                            var message = reader.ReadString();
                            switch (logLevel)
                            {
                                case 0:
                                    Console.WriteLine(message);
                                    break;

                                case 1:
                                    Console.WriteLine("!<BM>Info|" + message);
                                    break;

                                case 2:
                                    Console.WriteLine("!<BM>Warning|" + message);
                                    break;

                                case 3:
                                    Console.Error.WriteLine(message);
                                    break;
                            }
                        }
                    }
                    catch
                    {
                    }

                    processTask.Wait();
                    return process.ExitCode;
                }
                else
                {
                    // timeout connecting to pipe
                    try { process.Kill(); }
                    catch { }
                    Console.Write(stdOutBuffer.ToString());
                    Console.Error.Write(stdErrBuffer.ToString());
                    return -1;
                }
            }
        }

        private static void ReadProcessData(TextReader reader, StringBuilder buffer)
        {
            lock (buffer)
            {
                var text = reader.ReadLine();
                while (text != null)
                {
                    buffer.AppendLine(text);
                    text = reader.ReadLine();
                }
            }
        }

        private static string EscapeArgument(string argument)
        {
            if (argument.EndsWith("\\"))
                argument += "\\";

            return "\"" + argument + "\"";
        }
    }
}
