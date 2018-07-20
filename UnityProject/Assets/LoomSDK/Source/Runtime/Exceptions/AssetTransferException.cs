﻿namespace Loom.Client
{
    /// <summary>
    /// Represents an asset transfer error.
    /// </summary>
    public class AssetTransferException : LoomException
    {
        public AssetTransferException()
        {
        }

        public AssetTransferException(string message) : base(message)
        {
        }
    }
}