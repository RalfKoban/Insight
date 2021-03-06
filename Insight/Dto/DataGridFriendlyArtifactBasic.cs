﻿namespace Insight.Dto
{
    /// <summary>
    /// DO NOT FORMAT
    /// </summary>
    public sealed class DataGridFriendlyArtifactBasic
    {
        public string Revision { get; set; }
        public string LocalPath { get; set; }
        public int Commits { get; set; }
        public int Committers { get; set; }
        public int WorkItems { get; set; }
        public int LOC { get; set; }
        public double Hotspot { get; internal set; }
        public int CodeAge_Days { get; set; }
    }
}