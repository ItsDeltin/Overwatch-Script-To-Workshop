using System;

namespace DS.Analysis.Files
{
    interface IFileDependent
    {
        /// <summary>Called when the depended ScriptFile is retrieved.</summary>
        /// <param name="file">The new ScriptFile. May be null on error.</param>
        /// <param name="exception">
        /// The error that caused <paramref name="file"/> to be null.
        /// <para>ArgumentException</para>
        /// <para>ArgumentNullException</para>
        /// <para>PathTooLongException</para>
        /// <para>DirectoryNotFoundException</para>
        /// <para>IOException</para>
        /// <para>UnauthorizedAccessException</para>
        /// <para>FileNotFoundException</para>
        /// <para>NotSupportedException</para>
        /// <para>System.Security.SecurityException</para>
        /// </param>
        void SetFile(ScriptFile file, Exception exception);
    }
}