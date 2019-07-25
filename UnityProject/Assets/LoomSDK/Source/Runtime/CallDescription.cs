namespace Loom.Client
{
    /// <summary>
    /// Stores description of the initial call.
    /// </summary>
    public struct CallDescription
    {
        /// <summary>
        /// The name of the original method that was called.
        /// </summary>
        public string CalledMethodName { get; }

        /// <summary>
        /// Whether this call is static.
        /// </summary>
        public bool IsStatic { get; }

        public CallDescription(string calledMethodName, bool isStatic)
        {
            CalledMethodName = calledMethodName;
            IsStatic = isStatic;
        }

        public override string ToString()
        {
            return $"{nameof(CalledMethodName)}: {CalledMethodName}, {nameof(IsStatic)}: {IsStatic}";
        }
    }
}
