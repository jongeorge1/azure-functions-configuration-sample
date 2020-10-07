namespace ListConfiguration
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.FileProviders;

    public class Function
    {
        private readonly IConfiguration configuration;

        public Function(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [FunctionName("Function1")]
        public Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "show-default-config")] HttpRequest req)
        {
            // The supplied IConfiguration is actually an IConfigurationRoot...
            var configRoot = this.configuration as IConfigurationRoot;

            var results = configRoot.Providers.SelectMany((x, i) => this.ExtractProperties(x, "Root", i)).ToDictionary(x => x.Key, x => x.Value);
            return Task.FromResult<IActionResult>(new OkObjectResult(results));
        }

        private Dictionary<string, Dictionary<string, string>> ExtractProperties(IConfigurationProvider provider, string namePrefix, int index)
        {
            if (provider is ChainedConfigurationProvider chainedProvider)
            {
                // This is a type of provider that wraps a previously existing ConfigurationRoot which may itslef have
                // multiple providers. It doesn't expose the wrapped ConfigurationRoot, but for this sample we will
                // dig it out using reflection.
                var sourceConfigurationField = typeof(ChainedConfigurationProvider).GetField("_config", BindingFlags.NonPublic | BindingFlags.Instance);
                ConfigurationRoot sourceConfiguration = (ConfigurationRoot)sourceConfigurationField.GetValue(chainedProvider);
                return sourceConfiguration.Providers.SelectMany((x, i) => this.ExtractProperties(x, $"{namePrefix} - {index} - {provider.GetType().FullName}", i)).ToDictionary(x => x.Key, x => x.Value);
            }

            // The other standard providers are all based on ConfigurationProvider. There's no built in way to enumerate
            // them, but we can cheat and use reflection for the purposes of this sample.
            var results = new Dictionary<string, Dictionary<string, string>>();
            var dataProperty = typeof(ConfigurationProvider).GetProperty("Data", BindingFlags.NonPublic | BindingFlags.Instance);
            var values = (Dictionary<string, string>)dataProperty.GetMethod?.Invoke(provider, null);
            results.Add($"{namePrefix} - {index} - {provider.GetType().FullName}", values);
    
            return results;
        }
    }
}
