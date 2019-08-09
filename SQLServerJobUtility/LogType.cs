namespace SQLServerJobUtility
{
    using System;

    /// <summary>
    /// The Log Type Enum
    /// </summary>
    public enum LogType : int
    {
        /// <summary>
        /// The success Log Type
        /// </summary>
        Success = 0,

        /// <summary>
        /// The failure Log Type
        /// </summary>
        Failure = 1,

        /// <summary>
        /// The info Log Type
        /// </summary>
        Info = 2
    }
}