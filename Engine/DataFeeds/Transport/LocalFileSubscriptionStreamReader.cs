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
 *
*/

using System.IO;
using Ionic.Zip;
using System.IO.Compression;
using QuantConnect.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System;

namespace QuantConnect.Lean.Engine.DataFeeds.Transport
{
    /// <summary>
    /// Represents a stream reader capable of reading lines from disk
    /// </summary>
    public class LocalFileSubscriptionStreamReader : IStreamReader
    {
        private StreamReader _streamReader;
        private readonly ZipFile _zipFile;
        private IDataFileCacheProvider _dataFileCacheProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileSubscriptionStreamReader"/> class.
        /// </summary>
        /// <param name="source">The local file to be read</param>
        /// <param name="entryName">Specifies the zip entry to be opened. Leave null if not applicable,
        /// or to open the first zip entry found regardless of name</param>
        public LocalFileSubscriptionStreamReader(IDataFileCacheProvider dataFileCacheProvider, string source, DateTime date, string entryName = null)
        {
            // unzip if necessary
            //var stream = source.GetExtension() == ".zip"
            //    ? Compression.UnzipBaseStream(source, entryName)
            //    : new FileStream(source, FileMode.Open, FileAccess.Read);
            //_streamReader = new StreamReader(stream);

            _streamReader = new StreamReader(dataFileCacheProvider.Fetch(source, date, entryName));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileSubscriptionStreamReader"/> class.
        /// </summary>
        /// <param name="source">The local file to be read</param>
        /// <param name="entryName">Specifies the zip entry to be opened. Leave null if not applicable,
        /// <param name="startingPosition">The starting position in the local file to be read</param>
        /// or to open the first zip entry found regardless of name</param>
        public LocalFileSubscriptionStreamReader(IDataFileCacheProvider dataFileCacheProvider, string source, string entryName, DateTime date, long startingPosition)
        {
            _dataFileCacheProvider = dataFileCacheProvider;

            // unzip if necessary
            _streamReader = source.GetExtension() == ".zip"
                ? Compression.Unzip(source, entryName, out _zipFile)
                : new StreamReader(source);

            if (startingPosition != 0)
            {
                _streamReader.BaseStream.Seek(startingPosition, SeekOrigin.Begin);
            }

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalFileSubscriptionStreamReader"/> class.
        /// </summary>
        /// <param name="zipFile">The local zip archive to be read</param>
        /// <param name="entryName">Specifies the zip entry to be opened. Leave null if not applicable,
        /// or to open the first zip entry found regardless of name</param>
        public LocalFileSubscriptionStreamReader(ZipFile zipFile, string entryName = null)
        {
            _zipFile = zipFile;
            var entry = _zipFile.Entries.FirstOrDefault(x => entryName == null || string.Compare(x.FileName, entryName, StringComparison.OrdinalIgnoreCase) == 0);
            if (entry != null)
            {
                var stream = new MemoryStream();
                entry.OpenReader().CopyTo(stream);
                stream.Position = 0;
                _streamReader = new StreamReader(stream);
            }
        }

        /// <summary>
        /// Returns the list of zip entries if local file stream reader is reading zip archive
        /// </summary>
        public IEnumerable<string> EntryFileNames
        {
            get
            {
                return _zipFile != null ? _zipFile.Entries.Select(x => x.FileName).ToList() : Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Gets <see cref="SubscriptionTransportMedium.LocalFile"/>
        /// </summary>
        public SubscriptionTransportMedium TransportMedium
        {
            get { return SubscriptionTransportMedium.LocalFile; }
        }

        /// <summary>
        /// Gets whether or not there's more data to be read in the stream
        /// </summary>
        public bool EndOfStream
        {
            get { return _streamReader == null || _streamReader.EndOfStream; }
        }

        /// <summary>
        /// Gets the next line/batch of content from the stream 
        /// </summary>
        public string ReadLine()
        {
            return _streamReader.ReadLine();
        }

        /// <summary>
        /// Disposes of the stream
        /// </summary>
        public void Dispose()
        {
            if (_streamReader != null)
            {
                _streamReader.Dispose();
                _streamReader = null;
            }
        }
    }
}