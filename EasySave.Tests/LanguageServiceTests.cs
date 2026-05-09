using EasySave.ViewModels.Services;
using Xunit;

namespace EasySave.Tests
{
    /// <summary>
    /// Tests for LanguageService – pure in-memory, no file system needed.
    /// </summary>
    public class LanguageServiceTests
    {
        private static LanguageService BuildService(string lang = "en")
        {
            var svc = new LanguageService();
            string json = lang == "fr"
                ? """{"Hello":"Bonjour","AppName":"EasySaveFR"}"""
                : """{"Hello":"Hello","AppName":"EasySave"}""";
            svc.LoadFromJson(json);
            return svc;
        }

        [Fact]
        public void Get_KnownKey_ReturnsTranslation()
        {
            var svc = BuildService("en");
            Assert.Equal("Hello", svc.Get("Hello"));
        }

        [Fact]
        public void Get_FrenchKey_ReturnsCorrectTranslation()
        {
            var svc = BuildService("fr");
            Assert.Equal("Bonjour", svc.Get("Hello"));
        }

        [Fact]
        public void Get_UnknownKey_ReturnsKeyItself()
        {
            // Spec: missing key → return the key name, never throw.
            var svc = BuildService();
            Assert.Equal("NonExistentKey", svc.Get("NonExistentKey"));
        }

        [Fact]
        public void Get_EmptyKey_ReturnsEmptyString()
        {
            var svc = BuildService();
            Assert.Equal(string.Empty, svc.Get(string.Empty));
        }

        [Fact]
        public void LoadFromJson_EmptyJson_DoesNotThrow()
        {
            var svc = new LanguageService();
            svc.LoadFromJson("{}");
            Assert.Equal("AnyKey", svc.Get("AnyKey")); // fallback to key
        }

        [Fact]
        public void SwitchLanguage_UpdatesTranslations()
        {
            var svc = BuildService("en");
            Assert.Equal("Hello", svc.Get("Hello"));

            svc.LoadFromJson("""{"Hello":"Bonjour"}""");
            Assert.Equal("Bonjour", svc.Get("Hello"));
        }
    }
}
