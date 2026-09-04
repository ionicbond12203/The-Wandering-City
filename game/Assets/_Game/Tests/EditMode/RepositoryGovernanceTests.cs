using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace WanderingCity.Tests
{
    // These are document contracts, not an assertion of production or artistic quality.
    public sealed class RepositoryGovernanceTests
    {
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        static readonly string[] Documents =
        {
            "AGENTS.md", "docs/AAA_VISION.md", "docs/AAA_ENGINEERING.md",
            "docs/AAA_QUALITY_BAR.md", "docs/AAA_ROADMAP.md", "docs/TECH_DEBT.md",
            "docs/REPOSITORY_ARCHITECTURE_ASSESSMENT.md"
        };
        static readonly string[] Fields =
        {
            "Dependencies", "Player outcome", "Non-goals", "Technical acceptance",
            "Visual/gameplay acceptance", "Performance criteria", "Tests",
            "Build validation", "Rollback boundary"
        };

        static List<string> RoadmapErrors(string markdown)
        {
            var errors = new List<string>();
            var milestones = Regex.Matches(markdown, @"^## (G\d{2}) — [^\r\n]+\r?\n([\s\S]*?)(?=^## |\z)", RegexOptions.Multiline);
            if (milestones.Count == 0) errors.Add("No milestones");
            var seen = new HashSet<string>();
            foreach (Match milestone in milestones)
            {
                string id = milestone.Groups[1].Value, body = milestone.Groups[2].Value;
                foreach (string field in Fields)
                {
                    var values = Regex.Matches(body, "^- " + Regex.Escape(field) + @":([^\r\n]*)", RegexOptions.Multiline);
                    if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0].Groups[1].Value))
                        errors.Add(id + " missing/duplicate field: " + field);
                }
                var dependency = Regex.Match(body, @"^- Dependencies:([^\r\n]*)", RegexOptions.Multiline);
                if (dependency.Success)
                {
                    string value = dependency.Groups[1].Value.Trim();
                    if (value == "none")
                    {
                        if (milestones[0] != milestone) errors.Add(id + " disconnected milestone");
                    }
                    else
                    {
                        foreach (string entry in value.Split(','))
                        {
                            string dep = entry.Trim();
                            if (!Regex.IsMatch(dep, @"^G\d{2}$") || !seen.Contains(dep) || dep == id)
                                errors.Add(id + " invalid/forward dependency: " + dep);
                        }
                    }
                }
                if (!seen.Add(id)) errors.Add(id + " duplicate milestone");
            }
            return errors;
        }

        [Test]
        public void GovernanceDocumentsAndLocalLinksResolveInsideRepository()
        {
            foreach (string relative in Documents)
            {
                string file = Path.Combine(Root, relative);
                Assert.That(File.Exists(file), Is.True, relative);
                string markdown = File.ReadAllText(file);
                Assert.That(markdown.Trim().Length, Is.GreaterThan(0), relative);
                foreach (Match link in Regex.Matches(markdown, @"\[[^\]]+\]\(([^)]+)\)"))
                {
                    string target = link.Groups[1].Value.Split('#')[0];
                    if (target.Length == 0 || Uri.TryCreate(target, UriKind.Absolute, out _)) continue;
                    string resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file), target));
                    Assert.That(resolved.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True, target);
                    Assert.That(File.Exists(resolved), Is.True, relative + " -> " + target);
                }
            }
        }

        [Test]
        public void RoadmapHasCompleteAcceptanceAndEarlierDependencies()
        {
            var errors = RoadmapErrors(File.ReadAllText(Path.Combine(Root, "docs/AAA_ROADMAP.md")));
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        static string Fixture(string id, string dependency)
        {
            return "## " + id + " — Fixture\n" + string.Join("\n", Fields.Select(field =>
                "- " + field + ": " + (field == "Dependencies" ? dependency : "Concrete acceptance"))) + "\n";
        }

        [Test]
        public void RoadmapValidatorAcceptsBranchingOrderedDependencies()
        {
            Assert.That(RoadmapErrors(Fixture("G00", "none") + Fixture("G01", "G00") +
                Fixture("G02", "G00") + Fixture("G03", "G01, G02")), Is.Empty);
        }

        [TestCase("missing")]
        [TestCase("blank")]
        [TestCase("duplicate-field")]
        [TestCase("forward")]
        [TestCase("cycle")]
        [TestCase("duplicate-id")]
        [TestCase("disconnected")]
        public void RoadmapValidatorRejectsBrokenAcceptanceOrDependencies(string defect)
        {
            string sample = Fixture("G00", "none") + Fixture("G01", "G00");
            switch (defect)
            {
                case "missing": sample = sample.Replace("- Tests: Concrete acceptance\n", ""); break;
                case "blank": sample = sample.Replace("- Tests: Concrete acceptance", "- Tests: "); break;
                case "duplicate-field": sample += "- Tests: Duplicate\n"; break;
                case "forward": sample = Fixture("G00", "G01") + Fixture("G01", "none"); break;
                case "cycle": sample = Fixture("G00", "none") + Fixture("G01", "G01"); break;
                case "duplicate-id": sample += Fixture("G01", "G00"); break;
                case "disconnected": sample += Fixture("G02", "none"); break;
            }
            Assert.That(RoadmapErrors(sample), Is.Not.Empty, defect);
        }

        [Test]
        public void EngineeringDefinesEveryRequiredSystemBoundary()
        {
            string document = File.ReadAllText(Path.Combine(Root, "docs/AAA_ENGINEERING.md"));
            string[] systems = { "World Streaming", "World State", "Save System", "AI Simulation LOD",
                "Navigation", "Character", "Traversal", "Combat", "Animation", "Quest", "Interaction",
                "Inventory", "Audio", "Weather", "Rendering", "Asset Management", "Content Authoring",
                "LOD/HLOD strategy", "Performance QA", "Visual QA" };
            foreach (string system in systems)
            {
                var row = Regex.Match(document, @"^\| " + Regex.Escape(system) + @" \|([^\r\n]+)", RegexOptions.Multiline);
                Assert.That(row.Success, Is.True, system);
                var cells = row.Groups[1].Value.Split('|').Take(3).ToArray();
                Assert.That(cells.Length, Is.EqualTo(3), system);
                Assert.That(cells.All(cell => !string.IsNullOrWhiteSpace(cell)), Is.True, system);
            }
        }
    }
}
