﻿using System;

namespace Loom.Client
{
    /// <summary>
    /// Represents an EVM-related error.
    /// </summary>
    public class EvmException : LoomException
    {
        public EvmException()
        {
        }

        public EvmException(string message) : base(message)
        {
        }

        public EvmException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}