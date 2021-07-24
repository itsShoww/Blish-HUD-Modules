using System;
using System.Net;
using System.Threading.Tasks;
using Flurl.Http;
using Newtonsoft.Json;
using static Nekres.Kill_Proof_Module.KillProofModule;
namespace Nekres.Kill_Proof_Module.Utils
{
    internal static class TaskUtil
    {
        public static async Task<(bool, T)> GetJsonResponse<T>(string request)
        {
            try
            {
                var rawJson = await request.AllowHttpStatus(HttpStatusCode.NotFound).GetStringAsync();
                return (true, JsonConvert.DeserializeObject<T>(rawJson));
            }
            catch (FlurlHttpTimeoutException ex)
            {
                Logger.Warn(ex, $"Request '{request}' timed out.");
            }
            catch (FlurlHttpException ex)
            {
                Logger.Warn(ex, $"Request '{request}' was not successful.");
            }
            catch (JsonReaderException ex)
            {
                Logger.Warn(ex, $"Failed to read JSON response returned by request '{request}'.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Unexpected error while requesting '{request}'.");
            }
            return (false, default);
        }
    }
}
