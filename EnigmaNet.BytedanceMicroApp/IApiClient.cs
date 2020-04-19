using System;
using System.Threading.Tasks;
using EnigmaNet.BytedanceMicroApp.Models;

namespace EnigmaNet.BytedanceMicroApp
{
    public interface IApiClient
    {
        Task<JsCode2SessionResult> JsCode2SessionAsync(string appId, string secret, string code, string anonymousCode);
    }
}
