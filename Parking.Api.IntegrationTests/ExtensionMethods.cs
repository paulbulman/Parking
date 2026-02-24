namespace Parking.Api.IntegrationTests;

using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Helpers;

public static class ExtensionMethods
{
    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string requestUri, T request, CancellationToken cancellationToken) =>
        await client.PostAsync(requestUri, CreateRequestContent(request), cancellationToken);

    public static async Task<HttpResponseMessage> PatchAsJsonAsync<T>(this HttpClient client, string requestUri, T request, CancellationToken cancellationToken) =>
        await client.PatchAsync(requestUri, CreateRequestContent(request), cancellationToken);

    public static async Task<T> DeserializeAsType<T>(this HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonHelpers.Deserialize<T>(responseContent);
    }

    private static StringContent CreateRequestContent<T>(T request) =>
        new StringContent(JsonHelpers.Serialize(request), Encoding.UTF8, "application/json");
}