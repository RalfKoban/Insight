﻿using System.Linq;

using Insight;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    internal class ProjectTests
    {
        [Test]
        public void NormalizeFileExtensions()
        {
            var proj = new Project();
            proj.ExtensionsToInclude = "Xml, .cs; CS   ; JAVA ";

            var normalized = proj.GetNormalizedFileExtensions().ToList();

            // Distinct values.
            // , or ; are accepted as separator.
            // Trimming and ToLower
            Assert.AreEqual(3, normalized.Count);
            Assert.AreEqual(".xml", normalized[0]);
            Assert.AreEqual(".cs", normalized[1]);
            Assert.AreEqual(".java", normalized[2]);
        }

        [Test]
        public void NormalizeFileExtensions_EmptyOrNull()
        {
            var proj = new Project();
            proj.ExtensionsToInclude = "";

            // empty
            var normalized = proj.GetNormalizedFileExtensions().ToList();
            Assert.AreEqual(0, normalized.Count);

            // null
            proj.ExtensionsToInclude = null;
            normalized = proj.GetNormalizedFileExtensions().ToList();
            Assert.AreEqual(0, normalized.Count);
        }
    }
}