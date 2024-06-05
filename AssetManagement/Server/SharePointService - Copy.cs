
//using AssetManagement.Dto;
//using Azure.Identity;
//using Microsoft.Graph;
//using Microsoft.Identity.Client;

//namespace AssetManagement.Server
//{
//    public class SharePointService
//    {
//        private readonly string tenantId = "cf92019c-152d-42f6-bbcc-0cf96e6b0108";
//        private readonly string clientId = "ab34deba-f294-4103-995f-0f93546acb2e";
//        private readonly string clientSecret = "9B48Q~xXW4s73XNcUBMAxcb2hX2xzJMUYjwFGa6B";
//        string siteid = "Sharepoint Demo";
//        string root = "https://credentinfotec.sharepoint.com";
//        private readonly string siteUrl = "https://credentinfotec.sharepoint.com/sites/demopoc";
//        string[] scopes = new[] { "https://graph.microsoft.com/.default" };
//        GraphServiceClient graphClient;
//        public SharePointService()
//        {
//            var options = new ClientSecretCredentialOptions()
//            {
//                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
//            };
//            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
//            .Create(clientId)
//            .WithClientSecret(clientSecret)
//            .WithTenantId(tenantId)
//            .Build();
//            var authResult = confidentialClientApplication.AcquireTokenForClient(scopes).ExecuteAsync().Result;
//            var authProvider = new DelegateAuthenticationProvider(req =>
//            {
//                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.AccessToken);
//                return Task.FromResult(0);
//            });
//            graphClient = new GraphServiceClient(authProvider);
//        }

//        public async Task GetSitesAsync()
//        {
//            var sites = new List<Site>();
//            var res = await graphClient.Sites["root"].SiteWithPath("/sites/demopoc").Request().GetAsync();

//            int ind = 1;
//            var s = res;
//            //foreach (var s in res.Value)
//            {
//                //if(s.Name.Contains(siteid))
//                {
//                    Console.WriteLine($"{ind++}. {s.Name}::{s.Id}[{s.WebUrl}]");
//                }
//            }
//        }


//        public async Task GetSiteListsAsync()
//        {
//            var siteLists = await graphClient.Sites["root"].SiteWithPath("/sites/demopoc").Lists.Request().GetAsync();

//            int index = 1;
//            foreach (var list in siteLists)
//            {
//                Console.WriteLine($"{index++}. {list.DisplayName} - {list.Id}");
//            }
//        }

//        public async Task InsertDataIntoListAsync()
//        {
//            var listId = "42d95848-2f80-4133-bd98-73489f635755"; // ID of the TestEMPM list

//            // Create a new ListItem object with dummy data
//            var newItem = new Microsoft.Graph.ListItem
//            {
//                Fields = new FieldValueSet
//                {
//                    AdditionalData = new Dictionary<string, object>
//                    {
//                        { "Title", "Dummy Title" },
//                        { "Gender", "Male" },
//                        { "Designation", "Software Engineer" },
//                        { "DOB", new DateTime(1990, 5, 15) },
//                        { "Location", "City" },
//                        { "Date_x0020_of_x0020_joining", new DateTime(2018, 10, 1) },
//                        { "Department", "IT" },
//                        { "Status", "Active" },
//                        { "EmployeeName", "John Doe" },
//                        { "EmployeeID", "EMP001" },
//                        { "Email", "john.doe@example.com" },
//                        { "ManagerID", "MGR001" },
//                        { "ManagerEmail", "manager@example.com" },

//                    }
//                }
//            };

//            try
//            {
//                var createdItem = await graphClient.Sites["root"].SiteWithPath("/sites/demopoc").Lists[listId].Items.Request().AddAsync(newItem);
//                Console.WriteLine($"Dummy item inserted successfully with ID: {createdItem.Id}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error inserting dummy item: {ex.Message}");
//            }
//        }

//        public async Task<SharepointListUpdate> InsertOrUpdateListItemAsync(SharepointListUpdate request)
//        {
//            var listId = "42d95848-2f80-4133-bd98-73489f635755"; // ID of the TestEMPM list

//            try
//            {
//                // Check if an item with the provided email exists in the list
//                var items = await graphClient.Sites["root"].SiteWithPath("/sites/demopoc").Lists[listId].Items
//                    .Request()
//                    .Filter($"Email eq '{request.Email}'")
//                    .GetAsync();

//                if (items.Count == 1)
//                {
//                    // Item with the email exists, update it
//                    var itemToUpdate = items[0];

//                    // Update fields if needed
//                    //itemToUpdate.Fields.Add("LastModified", DateTime.UtcNow.ToString());

//                    await graphClient.Sites["root"].SiteWithPath("/sites/demopoc").Lists[listId].Items[itemToUpdate.Id]
//                        .Request()
//                        .UpdateAsync(itemToUpdate);

//                    Console.WriteLine($"Item with email '{request.Email}' updated successfully.");
//                }
//                else
//                {
//                    var newItem = new Microsoft.Graph.ListItem
//                    {
//                        Fields = new FieldValueSet
//                        {
//                            AdditionalData = new Dictionary<string, object>
//                            {
//                                { "Gender", "" },
//                                { "Designation", request.Designation },
//                                { "DOB", request.DOB },
//                                { "Location", request.Location },
//                                { "Date_x0020_of_x0020_joining", request.DateOfJoining },
//                                { "Department", request.Department },
//                                { "Status", request.Status },
//                                { "EmployeeName", request.EmployeeName },
//                                { "EmployeeID", request.EmployeeID },
//                                { "Email", request.Email },
//                                { "ManagerID", request.ManagerID },
//                                { "ManagerEmail", request.ManagerEmail },
//                                { "Created", DateTime.UtcNow.ToString() },
//                            }
//                        }
//                    };

//                    var createdItem = await graphClient.Sites["root"].SiteWithPath("/sites/demopoc").Lists[listId].Items.Request().AddAsync(newItem);
//                    Console.WriteLine($"New item inserted with email '{request.Email}'.");
//                }
//                return request;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error inserting or updating item: {ex.Message}");
//                return null;
//            }
//        }
//    }
//}
