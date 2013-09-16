using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

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
            using (var pipeStream = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None))
            {
                var process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = args[0],
                        Arguments = string.Join(" ", args.Skip(1).Select(EscapeArgument)) + " /noconsolelogger \"/logger:" + typeof(Program).Assembly.Location + ";" + pipeName + "\"",
                        UseShellExecute = false
                    }
                );

                pipeStream.WaitForConnection();

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

                process.WaitForExit();
                return process.ExitCode;
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
