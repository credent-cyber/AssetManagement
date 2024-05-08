using Newtonsoft.Json;

namespace AssetManagement.Server
{
    using Microsoft.Identity.Client;
    using MySqlX.XDevAPI;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class SharePointService
    {
        private readonly string _tenantId = "cf92019c-152d-42f6-bbcc-0cf96e6b0108";
        private readonly string _clientId = "ab34deba-f294-4103-995f-0f93546acb2e";
        private readonly string _clientSecret = "qMk8Q~VKszyYNQJET3LB5_qbBeETAoV-SBEPYbZr";
        private readonly string _siteUrl = "https://credentinfotec.sharepoint.com/sites/demopoc";
        private readonly string _listName = "TestEMPM";
        private readonly HttpClient _httpClient;

        public SharePointService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task InsertListItem()
        {
            string accessToken = await GetAccessToken();

            string requestUrl = $"{_siteUrl}/_api/web/lists/GetByTitle('{_listName}')/items";

            // Prepare the JSON payload with the fields to be inserted
            var content = new StringContent(JsonSerializer.Serialize(new
            {
                __metadata = new { type = "SP.Data.TestEMPMListItem" },
                EmployeeName = "employeeName",
                //Gender = gender,
                //Designation = designation,
                //IsCMSAdmin = isCMSAdmin,
                //DOB = dob,
                //Location = location,
                //DateOfJoining = dateOfJoining,
                //EmployeeID = employeeID,
                //Photo = photo,
                //Status = status,
                //Department = department,
                //Email = email,
                //ManagerID = managerID,
                //Manager = manager,
                //ManagerEmail = managerEmail,
                //MonthNo = monthNo,
                //LeaveBalanceWF = leaveBalanceWF,
                //HOD = hod,
                //EmployeeType = employeeType,
                //isAdmin = isAdmin,
                //isDualDept = isDualDept,
                //Phoneno = phoneNo,
                //Branch = branch,
                //IsEngineer = isEngineer,
                //AadhaarCard = aadhaarCard,
                //PanCard = panCard,
                //IsFinance = isFinance,
                //isHigherAuthority = isHigherAuthority,
                //Country = country,
                //Currency = currency,
                //isTKAdmin = isTKAdmin,
                //LMManagerID = lmManagerID,
                //LMManagerName = lmManagerName,
                //LMManagerEmail = lmManagerEmail,
                //LM = lm,
                //EMS = ems,
                //CMS = cms,
                //SitePermission = sitePermission
            }));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;odata=verbose");

            // Attach the access token to the request headers
            _httpClient.DefaultRequestHeaders.Add("Authorization",$"Bearer { accessToken}");

            // Send the POST request to insert the list item
            HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, content);

            // Check if the request was successful
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error inserting list item: {response.ReasonPhrase}");
            }
        }

        public async Task UpdateListItem(int itemId, string updatedValue)
        {
            await InsertListItem();

            string accessToken = await GetAccessToken();
            string requestUrl = $"{_siteUrl}/_api/web/lists/GetByTitle('{_listName}')/items({itemId})";
            var content = new StringContent(JsonSerializer.Serialize(new
            {
                __metadata = new { type = "SP.Data.TestEMPMListItem" },
                EmployeeName = updatedValue
            }));
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;odata=verbose");

            // Attach the access token to the request headers
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Send the PATCH request to update the list item
            HttpResponseMessage response = await _httpClient.PatchAsync(requestUrl, content);

            // Check if the request was successful
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error updating list item: {response.ReasonPhrase}");
            }
        }

        private async Task<string> GetAccessToken()
        {
            //var tokenEndpoint = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token";
            //var requestBody = $"client_id={Uri.EscapeDataString(_clientId)}" +
            //                  $"&client_secret={Uri.EscapeDataString(_clientSecret)}" +
            //                  $"&resource=https://credentinfotec.sharepoint.com" +
            //                  $"&grant_type=client_credentials";

            //var requestContent = new StringContent(requestBody, Encoding.UTF8, "application/x-www-form-urlencoded");

            //using (var response = await _httpClient.PostAsync(tokenEndpoint, requestContent))
            //{
            //    if (response.IsSuccessStatusCode)
            //    {
            //        var responseContent = await response.Content.ReadAsStringAsync();
            //        dynamic tokenResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseContent);
            //        return tokenResponse.access_token;
            //    }
            //}
            //return null;
            string resource = "https://credentinfotec.sharepoint.com";
            string authority = $"https://login.microsoftonline.com/{_tenantId}"; 
            string tokenEndpoint = $"{authority}/oauth2/token";
            var tokenRequest = new Dictionary<string, string> { { "client_id", _clientId }, { "client_secret", _clientSecret }, { "grant_type", "client_credentials" }, { "resource", resource } }; 
            var requestContent = new FormUrlEncodedContent(tokenRequest); var response = await _httpClient.PostAsync(tokenEndpoint, requestContent); 
            if (response.IsSuccessStatusCode) 
            { 
                var tokenResponse = await response.Content.ReadAsStringAsync();
                var json = Newtonsoft.Json.JsonConvert.DeserializeObject<TokenResponse>(tokenResponse);
                var resp = json.AccessToken;
                return resp;
            }
            else { throw new Exception("Failed to acquire access token."); }
        }
    }

}

public class TokenResponse { [JsonProperty("token_type")] public string TokenType { get; set; } [JsonProperty("expires_in")] public int ExpiresIn { get; set; } [JsonProperty("ext_expires_in")] public int ExtendedExpiresIn { get; set; } [JsonProperty("expires_on")] public long ExpiresOn { get; set; } [JsonProperty("not_before")] public long NotBefore { get; set; } [JsonProperty("resource")] public string Resource { get; set; } [JsonProperty("access_token")] public string AccessToken { get; set; } }