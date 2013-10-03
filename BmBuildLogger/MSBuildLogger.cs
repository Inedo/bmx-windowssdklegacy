using System.IO;
using System.IO.Pipes;
using System.Text;
using Microsoft.Build.Framework;

namespace Inedo.BmBuildLogger
{
    /// <summary>
    /// Custom MSBuild logger for BuildMaster.
    /// </summary>
    public sealed class MSBuildLogger : ILogger
    {
        private BinaryWriter writer;
        private object lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="MSBuildLogger"/> class.
        /// </summary>
        public MSBuildLogger()
        {
        }

        /// <summary>
        /// Gets or sets the user-defined parameters of the logger.
        /// </summary>
        public string Parameters { get; set; }
        /// <summary>
        /// Gets or sets the level of detail to show in the event log.
        /// </summary>
        public LoggerVerbosity Verbosity { get; set; }

        /// <summary>
        /// Subscribes loggers to specific events. This method is called when the logger is registered with the build engine, before any events are raised.
        /// </summary>
        /// <param name="eventSource">The events available to loggers.</param>
        public void Initialize(IEventSource eventSource)
        {
            var pipeStream = new NamedPipeClientStream(".", this.Parameters, PipeDirection.Out, PipeOptions.Asynchronous);
            pipeStream.Connect();
            this.writer = new BinaryWriter(pipeStream, Encoding.UTF8);

            eventSource.MessageRaised += (s, e) =>
            {
                if (e.Importance <= MessageImportance.Normal)
                    this.LogMessage(0, e.SenderName + ": " + e.Message);
            };

            eventSource.ProjectStarted += (s, e) => this.LogMessage(1, "Building " + e.Message);
            eventSource.ProjectFinished += (s, e) => this.LogMessage(1, e.Message);
            eventSource.WarningRaised += (s, e) => this.LogMessage(2, string.Format("{0}({1},{2}): warning {3}: {4}", e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message));
            eventSource.ErrorRaised += (s, e) => this.LogMessage(3, string.Format("{0}({1},{2}): error {3}: {4}", e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message));
        }
        /// <summary>
        /// Releases the resources allocated to the logger at the time of initialization or during the build. This method is called when the logger is unregistered from the engine, after all events are raised. A host of MSBuild typically unregisters loggers immediately before quitting.
        /// </summary>
        public void Shutdown()
        {
            lock (this.lockObject)
            {
                this.writer.Close();
            }
        }

        private void LogMessage(int level, string message)
        {
            lock (this.lockObject)
            {
                this.writer.Write((byte)level);
                this.writer.Write(message ?? string.Empty);
                this.writer.Flush();
            }
        }
    }
}