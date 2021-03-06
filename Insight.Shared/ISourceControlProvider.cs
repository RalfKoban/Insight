﻿using System;
using System.Collections.Generic;

using Insight.Shared.Model;
using Insight.Shared.VersionControl;

namespace Insight.Shared
{
    public interface ISourceControlProvider
    {
        Dictionary<string, uint> CalculateDeveloperWork(string localFile);

        /// <summary>
        /// Returns path to the cached file
        /// </summary>
        List<FileRevision> ExportFileHistory(string localFile);

        /// <summary>
        /// Returns a hash set of all server paths currently tracked by the svn.
        /// </summary>
        HashSet<string> GetAllTrackedFiles();

        // TODO use in the other two!
        void Initialize(string projectBase, string cachePath, IFilter fileFilter, string workItemRegex);
        ChangeSetHistory QueryChangeSetHistory();

        List<WarningMessage> Warnings { get; }

        /// <summary>
        /// Read the history from the source control provider and store it offline in the file system.
        /// Work data is can be very slow to calculate, especially for svn. Therefore it is optional.
        /// </summary>
        void UpdateCache(IProgress progress, bool includeWorkData);

        /// <summary>
        /// Work data is optional.
        /// Returns null if the work data is not cached.
        /// </summary>
        Dictionary<string, Contribution> QueryContribution();
    }
}