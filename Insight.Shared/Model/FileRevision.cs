﻿using System;

namespace Insight.Shared.Model
{
    [Serializable]
    public sealed class FileRevision
    {
        public FileRevision(string localFile, string changesetId, DateTime date, string cachePath)
        {
            ChangesetId = changesetId;
            Date = date;
            CachePath = cachePath;
            LocalFile = localFile;
        }

        /// <summary>
        ///     Cache path, where the file is downloaded
        /// </summary>
        public string CachePath { get; set; }

        public string ChangesetId { get; }
        public DateTime Date { get; }
        public string LocalFile { get; }
    }
}