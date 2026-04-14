// ReSharper disable RedundantUsingDirective
// ReSharper disable CheckNamespace

//The following are the minimum .Net libraries to make this work, however,  if using the Jube.Sandbox,  this will be shown,  as the project file is set to not default any assemblies and will be a like for like compile.
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

//All assemblies are passed on the basis of what the Jube.App context is using, so nearly all libraries are supported.
using Newtonsoft.Json.Linq;

//The following are Jube libraries that contain the context and Inline Script attributes.
using Jube.Engine.Attributes;
using Jube.Engine.Attributes.Events;
using Jube.Engine.Attributes.Properties;
using Jube.Engine.EntityAnalysisModelInvoke.Context;
using Jube.Engine.Interfaces;

public class Example : IInlineScript//Class entry point is available in table configuration.
{
    //Attributes overlap with the same options available in Request XPath
    [Cache(CacheIndexId = 1001)]
    [ReportTable]    //ReportTable is evaluated on recall.
    [ResponsePayload]//ResponsePayload is evaluated in model synchronization and used in model response preparation.
    [Latitude]       //Latitude is evaluated in model synchronization and is used during model activation watcher dispatch.
    [Longitude]      //Longitude is evaluated in model synchronization and is used during model activation watcher dispatch.
    [SearchKey(SearchKeyTtlInterval = "h",
        SearchKeyTtlIntervalValue = 1,
        SearchKeyFetchLimit = 100,
        SearchKeyCache = false,
        SearchKeyCacheInterval = "d",
        SearchKeyCacheValue = 1,
        SearchKeyCacheSample = true,
        SearchKeyCacheFetchLimit = 100,
        SearchKeyCacheTtlInterval = "h",
        SearchKeyCacheTtlValue = 1)]                      //SearchKey ensures that this is exposed in for aggregation in both the background engine.
    public string? UserAgent { get; set; } = string.Empty;//Public properties are available for processing, being analogous,  when taken together with attributes, to a Request XPath entry

    [ActivationRuleOverrideEvent(Guid = "bc81ff60-3254-4f1a-9003-ecae5e114142", Priority = 1)]
    [PayloadEvent]
    public async Task<bool> ExecuteAsync(Context context)//Method entry point.  The Context object gives access to all resources that would otherwise be available during invocation.
    {
        //Example HTTP Call with the fetching of data from the context.
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
        client.DefaultRequestHeaders.Add("IP", context.EntityAnalysisModelInstanceEntryPayload.Payload["IP"]);//Here we are looking at the context, and while we are only extracting data here,  full access to all application resources are available in this context,  such as logging.

        var response = await client.GetStringAsync("https://postman-echo.com/get");//Get the data from remote using the standard HTTP client.

        //Example Parse and transpose to payload.
        var jObject = JObject.Parse(response);//The response stream is processed using Newtonsoft

 #pragma warning disable CS8600// Converting null literal or possible null value to non-nullable type.
        var userAgent = (string)jObject["headers"]?["user-agent"] ?? "Unknown";
 #pragma warning restore CS8600// Converting null literal or possible null value to non-nullable type.

        UserAgent = userAgent;

        return true;//Behavior depends upon the event,  but in all cases properties will only be extracted to payload on return true;
    }
}
