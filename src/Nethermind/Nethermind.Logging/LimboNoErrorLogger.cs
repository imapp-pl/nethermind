// SPDX-FileCopyrightText: 2025 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using System;
using System.Threading;

namespace Nethermind.Logging
{
    /// <summary>
    /// LimboLogs redirects logs to nowhere (limbo) and it should be always used in tests as it guarantees that
    /// we test any potential issues with the log message construction.
    /// Imagine that we have a construction like if(_logger.IsTrace) _logger.Trace("somethingThatIsNull.ToString()")
    /// This would not be tested until we switched the logger to Trace level and this, in turn,
    /// would slow down the tests and increase memory construction due to the log files generation.
    /// Instead we use LimboLogs that returns a logger that always causes the log message to be created and so we can
    /// detect somethingThatIsNull.ToString() throwing an error.
    /// </summary>
    public class LimboNoErrorLogger : InterfaceLogger
    {
        private static LimboNoErrorLogger _instance;

        public static ILogger Instance
        {
            get { return new(LazyInitializer.EnsureInitialized(ref _instance, static () => new LimboNoErrorLogger())); }
        }

        public void Info(string text)
        {
        }

        public void Warn(string text)
        {
        }

        public void Debug(string text)
        {
        }

        public void Trace(string text)
        {
        }

        public void Error(string text, Exception ex = null)
        {
            Console.Error.WriteLine(text);
            Console.Error.WriteLine(ex);
            throw new Exception(text, ex);
        }

        public bool IsInfo => true;
        public bool IsWarn => true;
        public bool IsDebug => true;
        public bool IsTrace => true;
        public bool IsError => true;
    }
}
