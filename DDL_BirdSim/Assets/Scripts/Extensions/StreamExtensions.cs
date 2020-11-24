namespace Easy.Common.Extensions
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// A set of extension methods for <see cref="Stream"/>.
    /// </summary>
    public static class StreamExtensions
    {
        private const char CR = '\r';
        private const char LF = '\n';
        private const char NULL = (char)0;
        
        /// <summary>
        /// Detects the text encoding for the given <paramref name="stream"/>.
        /// </summary>
        [DebuggerStepThrough]
        public static Encoding DetectEncoding(this Stream stream, Encoding defaultEncodingIfNoBOM)
        {
            Ensure.NotNull(stream, nameof(stream));
            
            var startPos = stream.Position;

            try
            {
                using (var reader = new StreamReader(stream, defaultEncodingIfNoBOM, true, 1, true))
                {
                    var _ = reader.Peek();
                    return reader.CurrentEncoding;
                }
            } finally
            {
                if (stream.CanSeek) { stream.Position = startPos; }
            }
        }

        /// <summary>
        /// Returns the number of lines in the given <paramref name="stream"/>.
        /// </summary>
        [DebuggerStepThrough]
        public static long CountLines(this Stream stream)
        {
            Ensure.NotNull(stream, nameof(stream));

            var lineCount = 0L;

            var byteBuffer = new byte[1024 * 1024];
            var detectedEOL = NULL;
            var currentChar = NULL;

            int bytesRead;
            while ((bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
            {
                for (var i = 0; i < bytesRead; i++)
                {
                    currentChar = (char)byteBuffer[i];

                    if (detectedEOL != NULL)
                    {
                        if (currentChar == detectedEOL)
                        {
                            lineCount++;
                        }
                    }
                    else if (currentChar == LF || currentChar == CR)
                    {
                        detectedEOL = currentChar;
                        lineCount++;
                    }
                }
            }

            if (currentChar != LF && currentChar != CR && currentChar != NULL)
            {
                lineCount++;
            }

            return lineCount;
        }
        
        
    }
}