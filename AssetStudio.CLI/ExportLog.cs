using System;
using System.IO;
using System.Text;

namespace AssetStudio.CLI
{
    internal enum ExportLogStatus
    {
        Success,
        Skipped,
        Failed,
        Error,
    }

    internal sealed class ExportLogState
    {
        public bool HasExplicitResult;
        public ExportLogStatus Status;
        public string Message;
        public string ExportRoot;
        public string ExportTarget;
    }

    internal static class ExportLog
    {
        [ThreadStatic]
        private static ExportLogState _currentState;

        private static readonly object Sync = new object();
        private static StreamWriter _resultsWriter;
        private static StreamWriter _messagesWriter;

        public static string ResultsPath { get; private set; }
        public static string MessagesPath { get; private set; }
        public static bool IsInitialized => _resultsWriter != null;

        public static void Initialize(string outputRoot)
        {
            Directory.CreateDirectory(outputRoot);

            ResultsPath = Path.Combine(outputRoot, "assetstudio_export_log.csv");
            MessagesPath = Path.Combine(outputRoot, "assetstudio_export_messages.log");

            lock (Sync)
            {
                _resultsWriter = new StreamWriter(ResultsPath, false, new UTF8Encoding(false)) { AutoFlush = true };
                _messagesWriter = new StreamWriter(MessagesPath, false, new UTF8Encoding(false)) { AutoFlush = true };
                _resultsWriter.WriteLine("timestamp_utc,status,asset_type,asset_name,path_id,container,source_file,export_root,export_target,message");
            }

            Logger.MessageLogged += OnLoggerMessage;
        }

        public static void Shutdown()
        {
            Logger.MessageLogged -= OnLoggerMessage;

            lock (Sync)
            {
                _resultsWriter?.Dispose();
                _resultsWriter = null;

                _messagesWriter?.Dispose();
                _messagesWriter = null;
            }

            ResultsPath = null;
            MessagesPath = null;
            _currentState = null;
        }

        public static void BeginAsset(string exportRoot)
        {
            _currentState = new ExportLogState
            {
                ExportRoot = exportRoot,
                Message = "Exporter returned false without a specific reason."
            };
        }

        public static void MarkSuccess(string exportTarget = null, string message = null)
        {
            EnsureState();
            _currentState.HasExplicitResult = true;
            _currentState.Status = ExportLogStatus.Success;
            _currentState.ExportTarget = exportTarget ?? _currentState.ExportTarget;
            _currentState.Message = string.IsNullOrWhiteSpace(message) ? "Exported." : message;
        }

        public static void MarkSkipped(string reason, string exportTarget = null)
        {
            EnsureState();
            _currentState.HasExplicitResult = true;
            _currentState.Status = ExportLogStatus.Skipped;
            _currentState.ExportTarget = exportTarget ?? _currentState.ExportTarget;
            _currentState.Message = reason;
        }

        public static void MarkFailed(string reason, string exportTarget = null)
        {
            EnsureState();
            _currentState.HasExplicitResult = true;
            _currentState.Status = ExportLogStatus.Failed;
            _currentState.ExportTarget = exportTarget ?? _currentState.ExportTarget;
            _currentState.Message = reason;
        }

        public static void MarkError(Exception exception, string exportTarget = null)
        {
            EnsureState();
            _currentState.HasExplicitResult = true;
            _currentState.Status = ExportLogStatus.Error;
            _currentState.ExportTarget = exportTarget ?? _currentState.ExportTarget;
            _currentState.Message = exception.ToString();
        }

        public static ExportLogStatus Commit(AssetItem asset, bool exported)
        {
            var state = _currentState ?? new ExportLogState
            {
                Message = exported ? "Exported." : "Exporter returned false without a specific reason."
            };

            if (!state.HasExplicitResult)
            {
                state.Status = exported ? ExportLogStatus.Success : ExportLogStatus.Failed;
            }

            if (string.IsNullOrWhiteSpace(state.Message))
            {
                state.Message = state.Status == ExportLogStatus.Success
                    ? "Exported."
                    : "Exporter returned false without a specific reason.";
            }

            if (_resultsWriter != null)
            {
                var sourceFile = asset.SourceFile?.originalPath;
                if (string.IsNullOrEmpty(sourceFile))
                {
                    sourceFile = asset.SourceFile?.fileName;
                }

                lock (Sync)
                {
                    _resultsWriter?.WriteLine(string.Join(",",
                        EscapeCsv(DateTime.UtcNow.ToString("O")),
                        EscapeCsv(state.Status.ToString().ToLowerInvariant()),
                        EscapeCsv(asset.TypeString),
                        EscapeCsv(asset.Text),
                        EscapeCsv(asset.m_PathID.ToString()),
                        EscapeCsv(asset.Container),
                        EscapeCsv(sourceFile),
                        EscapeCsv(state.ExportRoot),
                        EscapeCsv(state.ExportTarget),
                        EscapeCsv(state.Message)));
                }
            }

            _currentState = null;
            return state.Status;
        }

        private static void OnLoggerMessage(LoggerEvent loggerEvent, string message)
        {
            if (_messagesWriter == null)
            {
                return;
            }

            lock (Sync)
            {
                _messagesWriter?.WriteLine($"[{DateTime.UtcNow:O}][{loggerEvent}] {message}");
            }
        }

        private static void EnsureState()
        {
            _currentState ??= new ExportLogState();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) == -1)
            {
                return value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}