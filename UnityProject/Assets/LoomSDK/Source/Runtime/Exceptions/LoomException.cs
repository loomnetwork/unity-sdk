﻿using System;

namespace Loom.Client
{
    /// <summary>
    /// Represents an error that is specific to Loom.
    /// </summary>
    public class LoomException : Exception
    {
        public LoomException()
        {
        }

        public LoomException(string message) : base(message)
        {
        }

        public LoomException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}