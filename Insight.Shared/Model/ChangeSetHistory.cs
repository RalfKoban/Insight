﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Insight.Shared.Model
{
    [Serializable]
    public sealed class ChangeSetHistory
    {
        public ChangeSetHistory(List<ChangeSet> changeSets)
        {
            ChangeSets = changeSets;
        }

        public List<ChangeSet> ChangeSets { get; }

        public void CleanupHistory()
        {
            CleanupHistory(this);
        }

        /// <summary>
        ///     Returns a flat summary of all artifacts found in the commit history.
        /// </summary>
        public List<Artifact> GetArtifactSummary(IFilter filter, HashSet<string> metricFiles)
        {
            // Item id -> artifact
            var artifacts = new Dictionary<string, Artifact>();

            var set = new HashSet<string>();

            // Files we already know we skip are not checked again!
            var ignore = new HashSet<string>();

            foreach (var changeset in ChangeSets)
            {
                if (changeset.WorkItems.Count >= Thresholds.MaxWorkItemsPerCommitForSummary)
                {
                    // Ignore monster merges.
                    // Note: We may lose files for the summary when the last merge with many work items contains a final rename.
                    // Maybe write a warning or make further analyses.
                    continue;
                }

                Debug.Assert(set.Add(changeset.Id)); // Change set appears only once
                foreach (var item in changeset.Items)
                {
                    // The first time we see a file (id) it is the latest version of the file.
                    // Either add it to the summary or ignore list.
                    var id = item.Id;
                    if (ignore.Contains(id))
                    {
                        // Files we already know to be skipped are not checked again! (Performance)
                        continue;
                    }

                    if (filter != null && !filter.IsAccepted(item.LocalPath))
                    {
                        ignore.Add(id);
                        continue;
                    }

                    if (!artifacts.ContainsKey(id))
                    {
                        // The changeset where we see the item the first time is the latest revision!

                        if (!Exists(item, metricFiles))
                        {
                            // Because I delete now no longer tracked files from the history we should never end up here!
                            // However in git it is possible because we may see a commit with a modify after a delete.

                            // This may still happen because we skip large merges that may contain a final rename.
                            // So we have a code metric but still believe that the file is at its former location

                            // TODO show as warning!
                            Trace.WriteLine($"Ignored file: '{item.LocalPath}'. It should exist. Possible cause: Ignored commit with too much work items containing a final rename.");
                            ignore.Add(id);
                            continue;
                        }

                        artifacts[id] = CreateArtifact(changeset, item);
                    }
                    else
                    {
                        // Changesets seen first are expected so have newer dates.
                        Debug.Assert(artifacts[id].Date >= changeset.Date);
                    }

                    var artifact = artifacts[id];

                    // Aggregate information from earlier commits (for example number of commits etc)
                    // TODO ApplyTeams(teamClassifier, artifact, changeset);
                    ApplyCommits(artifact);
                    ApplyCommitters(artifact, changeset);
                    ApplyWorkItems(artifact, changeset);
                }
            }

            // Remove entries that exist on hard disk but are removed form TFS!
            // Flatten the structure and return only the artifacts.
            return artifacts.Where(pair => !pair.Value.IsDeleted).Select(pair => pair.Value).ToList();
        }

        private static void ApplyCommits(Artifact artifact)
        {
            artifact.Commits = artifact.Commits + 1;
        }

        private static void ApplyCommitters(Artifact artifact, ChangeSet changeset)
        {
            artifact.Committers.Add(changeset.Committer);
        }

        private static void ApplyTeams(ITeamClassifier teamClassifier, Artifact artifact, ChangeSet changeset)
        {
            if (teamClassifier != null)
            {
                // If a classifier is given use it ...
                artifact.Teams.Add(teamClassifier.GetAssociatedTeam(changeset.Committer, changeset.Date));
            }
        }

        private static void ApplyWorkItems(Artifact artifact, ChangeSet changeset)
        {
            foreach (var workitem in changeset.WorkItems)
            {
                artifact.WorkItems.Add(workitem);
            }
        }

        private static Artifact CreateArtifact(ChangeSet cs, ChangeItem item)
        {
            Debug.Assert(item.LocalPath != null);
            var artifact = new Artifact
                           {
                                   Id = item.Id,
                                   LocalPath = item.LocalPath,
                                   ServerPath = item.ServerPath,
                                   Commits = 0,

                                   // Item used to create sets the revision (latest)
                                   Revision = cs.Id,

                                   // Assume first item is latest revision. If this is deleted the item should no longer be on hard disk.
                                   IsDeleted = item.IsDelete(),

                                   Date = cs.Date
                           };

            return artifact;
        }

        /// <summary>
        /// Removes all files that were deleted and are no longer available
        /// </summary>
        private void CleanupHistory(ChangeSetHistory history)
        {
            var deletedIds = history.ChangeSets
                                    .SelectMany(set => set.Items)
                                    .Where(item => item.IsDelete())
                                    .Select(item => item.Id);

            var deletedIdsHash = new HashSet<string>(deletedIds);

            foreach (var set in history.ChangeSets)
            {
                set.Items.RemoveAll(item => deletedIdsHash.Contains(item.Id));
            }

            // Delete empty commits
            var changeSetsCopy = history.ChangeSets.ToList();
            foreach (var changeSet in changeSetsCopy)
            {
                if (!changeSet.Items.Any())
                {
                    history.ChangeSets.Remove(changeSet);
                }
            }
        }

        private bool Exists(ChangeItem item, HashSet<string> localFiles)
        {
            if (localFiles != null && localFiles.Any())
            {
                // Performance optimization
                return localFiles.Contains(item.LocalPath.ToLowerInvariant());
            }

            return item.Exists();
        }

    }
}