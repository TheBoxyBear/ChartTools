﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using ChartTools.SystemExtensions;

namespace ChartTools.IO
{
    /// <summary>
    /// Provides methods for rading and writing files based on the extension
    /// </summary>
    internal static class ExtensionHandler
    {
        /// <summary>
        /// Reads a file using the method that matches the extension.
        /// </summary>
        /// <param name="path">Path of the file to read</param>
        /// <param name="readers">Array of tuples representing the supported extensions</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FileNotFoundException"/>
        internal static void Read(string path, params (string extension, Action<string> readMethod)[] readers)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException();

            string extension = Path.GetExtension(path);
            (string extension, Action<string> readMethod) reader = readers.FirstOrDefault(r => r.extension == extension);

            if (reader == default)
                throw GetException(extension, readers.Select(r => r.extension));

            reader.readMethod(path);
        }
        /// <summary>
        /// Reads a file using the method that matches the extension.
        /// </summary>
        /// <param name="path">Path of the file to read</param>
        /// <param name="readers">Array of tuples representing the supported extensions</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FileNotFoundException"/>
        internal static T Read<T>(string path, params (string extension, Func<string, T> readMethod)[] readers)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException();

            string extension = Path.GetExtension(path);
            (string extension, Func<string, T> readMethod) reader = readers.FirstOrDefault(r => r.extension == extension);

            return reader == default ? throw GetException(extension, readers.Select(r => r.extension)) : reader.readMethod(path);
        }

        /// <summary>
        /// Writes an object to a file using the method that matches the extension.
        /// </summary>
        /// <param name="path">Path of the file to write</param>
        /// <param name="item">Item to write</param>
        /// <param name="writers">Array of tupples representing the supported extensions</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="FileNotFoundException"/>
        internal static void Write<T>(string path, T item, params (string extension, Action<string, T> writeMethod)[] writers)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException();

            string extension = Path.GetExtension(path);
            (string extension, Action<string, T> writeMethod) writer = writers.FirstOrDefault(w => w.extension == extension);

            if (writer == default)
                throw GetException(extension, writers.Select(w => w.extension));

            writer.writeMethod(path, item);
        }

        /// <summary>
        /// Gets the exception to throw if the extension has no method that handles it.
        /// </summary>
        /// <returns>Instance of <see cref="Exception"/> to throw</returns>
        private static Exception GetException(string extension, IEnumerable<string> supportedExtensions) => new ArgumentException($"\"{extension}\" is not a supported extension. File must be {supportedExtensions.VerbalEnumerate("or")}.");
    }
}
