using System;
using System.Text;
using Microsoft.Build.Framework;

namespace Inedo.BmBuildLogger
{
    public sealed class MSBuildLogger : ILogger
    {
        private static readonly UTF8Encoding UTF8 = new UTF8Encoding(false);

        public string Parameters { get; set; }
        public LoggerVerbosity Verbosity { get; set; }

        public void Initialize(IEventSource eventSource)
        {
            if (Verbosity > LoggerVerbosity.Quiet) eventSource.MessageRaised += HandleMessageRaised;

            eventSource.ProjectStarted += HandleProjectStarted;
            eventSource.ProjectFinished += HandleProjectFinished;
            eventSource.WarningRaised += HandleWarningRaised;
            eventSource.ErrorRaised += HandleErrorRaised;
        }
        public void Shutdown()
        {
        }

        private static void HandleMessageRaised(object sender, BuildMessageEventArgs e)
        {
            if (e.Importance <= MessageImportance.Normal)
                LogMessage(0, e.SenderName + ": " + e.Message);
        }
        private static void HandleProjectStarted(object sender, ProjectStartedEventArgs e) => LogMessage(10, "Building " + e.Message);
        private static void HandleProjectFinished(object sender, ProjectFinishedEventArgs e) => LogMessage(10, e.Message);
        private static void HandleWarningRaised(object sender, BuildWarningEventArgs e)
        {
            LogMessage(20, $"{e.File}({e.LineNumber},{e.ColumnNumber}): warning {e.Code}: {e.Message}");
        }
        private static void HandleErrorRaised(object sender, BuildErrorEventArgs e)
        {
            LogMessage(30, $"{e.File}({e.LineNumber},{e.ColumnNumber}): error {e.Code}: {e.Message}");
        }

        private static void LogMessage(byte level, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                // The whole base64 conversion is kind of rubbish, but otherwise console output
                // can have weird issues with text encoding and newlines in log messages, so
                // it's just more reliable this way.
                var text = message.Trim();
                int byteCount = UTF8.GetByteCount(text);
                var bytes = new byte[byteCount + 1];
                bytes[0] = level;
                UTF8.GetBytes(text, 0, text.Length, bytes, 1);
                Console.WriteLine("<BM>" + Convert.ToBase64String(bytes));
            }
        }
    }
}