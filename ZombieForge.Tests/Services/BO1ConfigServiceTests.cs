using System;
using System.IO;
using System.Text;
using Xunit;
using ZombieForge.Services;

namespace ZombieForge.Tests.Services
{
    public class BO1ConfigServiceTests
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        [Fact]
        public void Load_SkipsCommentedLines_AndParsesQuotedBindCommands()
        {
            string filePath = CreateTempPath();
            try
            {
                File.WriteAllText(
                    filePath,
                    "// seta cg_fov_default \"999\"\n" +
                    "/ bind F \"should_be_ignored\"\n" +
                    "seta cg_fov_default \"90\"\n" +
                    "bind F \"vstr foo ; say \\\"hi\\\"\"\n",
                    Utf8NoBom);

                var config = BO1ConfigService.Load(filePath);

                Assert.Equal("90", config.Dvars["cg_fov_default"]);
                Assert.Equal("vstr foo ; say \"hi\"", config.Binds["F"]);
            }
            finally
            {
                DeleteIfExists(filePath);
            }
        }

        [Fact]
        public void Save_PreservesLineEndings_AndRemovesBindLinesWithoutBlanking()
        {
            string filePath = CreateTempPath();
            try
            {
                File.WriteAllText(
                    filePath,
                    "seta cg_fov_default \"65\"\n" +
                    "bind F \"say hi\"\n" +
                    "seta com_maxfps \"85\"\n",
                    Utf8NoBom);

                var config = BO1ConfigService.Load(filePath);
                config.Dvars["cg_fov_default"] = "100";
                config.RemovedBindKeys.Add("F");

                BO1ConfigService.Save(filePath, config);
                string saved = File.ReadAllText(filePath, Utf8NoBom);

                Assert.Contains("seta cg_fov_default \"100\"\n", saved);
                Assert.DoesNotContain("bind F", saved);
                Assert.DoesNotContain("\r\n", saved);
                Assert.EndsWith("\n", saved, StringComparison.Ordinal);
            }
            finally
            {
                DeleteIfExists(filePath);
            }
        }

        [Fact]
        public void Load_SkipsMalformedDirective_WithUnterminatedQuotedToken()
        {
            string filePath = CreateTempPath();
            try
            {
                File.WriteAllText(
                    filePath,
                    "seta cg_fov_default \"90\n" +
                    "seta cg_fov_default \"100\"\n",
                    Utf8NoBom);

                var config = BO1ConfigService.Load(filePath);

                Assert.Equal("100", config.Dvars["cg_fov_default"]);
            }
            finally
            {
                DeleteIfExists(filePath);
            }
        }

        private static string CreateTempPath()
            => Path.Combine(Path.GetTempPath(), $"ZombieForge.Tests.{Guid.NewGuid():N}.cfg");

        private static void DeleteIfExists(string filePath)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
