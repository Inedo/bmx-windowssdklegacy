using System.Collections.Generic;
using System.Threading.Tasks;
using Inedo.Extensibility;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk.SuggestionProviders
{
    public sealed class BuildConfigurationSuggestionProvider : ISuggestionProvider
    {
        public Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var values = (IEnumerable<string>)new[] { "Release", "Debug" };
            return Task.FromResult(values);
        }
    }
}
