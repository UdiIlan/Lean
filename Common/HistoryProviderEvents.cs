﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using QuantConnect.Interfaces;

namespace QuantConnect
{
    /// <summary>
    /// Event arguments for the <see cref="IHistoryProvider.InvalidConfigurationDetected"/> event
    /// </summary>
    public sealed class InvalidConfigurationDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidConfigurationDetectedEventArgs"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        public InvalidConfigurationDetectedEventArgs(string message)
        {
            Message = message;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="IHistoryProvider.NumericalPrecisionLimited"/> event
    /// </summary>
    public sealed class NumericalPrecisionLimitedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumericalPrecisionLimitedEventArgs"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        public NumericalPrecisionLimitedEventArgs(string message)
        {
            Message = message;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="IHistoryProvider.DownloadFailed"/> event
    /// </summary>
    public sealed class DownloadFailedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the error stack trace
        /// </summary>
        public string StackTrace { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadFailedEventArgs"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="stackTrace">The error stack trace</param>
        public DownloadFailedEventArgs(string message, string stackTrace = "")
        {
            Message = message;
            StackTrace = stackTrace;
        }
    }

    /// <summary>
    /// Event arguments for the <see cref="IHistoryProvider.ReaderErrorDetected"/> event
    /// </summary>
    public sealed class ReaderErrorDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the error stack trace
        /// </summary>
        public string StackTrace { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderErrorDetectedEventArgs"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="stackTrace">The error stack trace</param>
        public ReaderErrorDetectedEventArgs(string message, string stackTrace = "")
        {
            Message = message;
            StackTrace = stackTrace;
        }
    }
}
