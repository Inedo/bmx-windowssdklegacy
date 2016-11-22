using System.Collections.Generic;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.SuggestionProviders
{
    public sealed class TargetPlatformSuggestionProvider : ISuggestionProvider
    {
        public Task<IEnumerable<string>> GetSuggestionsAsync(IComponentConfiguration config)
        {
            var values = (IEnumerable<string>)new[] { "AnyCPU", "x86", "x64", "Win32" };
            return Task.FromResult(values);
        }
    }
}
