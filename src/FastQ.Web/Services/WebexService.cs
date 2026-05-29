
using FastQ.Data.Db;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using Microsoft.Ajax.Utilities;
using NLog;
using System;
using System.CodeDom;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FastQ.Web.Services
{
    public class WebexMeeting
    {
        public string BaseUrl { get; set; }
        public string HostUrl { get; set; }
        public string GuestUrl { get; set; }

        public string ApiError { get; set; }
    }


    public class WebexService
    {
        private static readonly Logger _logger = NLog.LogManager.GetLogger("FastQWebexSVC");
        private static readonly HttpClient client = new HttpClient();
        private static readonly string env = ConfigurationManager.AppSettings["Environment"];
        private readonly IWebexRepository _webexrepo;
        private static Data.Entities.WebexApiSettings _webexapi;


        public WebexService()
           : this(
               DbRepositoryFactory.CreateWebexRepository())
        {
        }
        public WebexService(IWebexRepository webexrepo)
        {
            _webexrepo = webexrepo;
        }
        
        // PReddy: This call has been moved to Windows Scheduled Task
        //private async Task<HttpResponseMessage> CreateG2GMeeting(string title, DateTime start)
        //{
        //    var meetingData = new
        //    {
        //        title = title,
        //        start = start.ToUniversalTime().AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ"),
        //        end = start.ToUniversalTime().AddDays(30).AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
        //        // Essential G2G scheduling options
        //        schedulingOptions = new
        //        {
        //            enabledJoinBeforeHost = true,
        //            joinBeforeHostMinutes = 5
        //        },
        //        // Required for G2G to bypass standard host locks
        //        unlockedMeetingJoinSecurity = "allowJoin"
        //    };

        //    var jsonBody = JsonSerializer.Serialize(meetingData);
        //    _logger.Info($"CreateG2GMeeting Request: {jsonBody}");

        //    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        //    var response = await client.PostAsync(webexUrl, content);
        //    var jsonResponse = await response.Content.ReadAsStringAsync();
        //    if (response.IsSuccessStatusCode)
        //    {
        //        _logger.Info($"CreateG2GMeeting Response: {jsonResponse}");
        //    }
        //    else
        //    {
        //        _logger.Error("CreateG2GMeeting Error: " + (int)response.StatusCode + " " + jsonResponse);

        //    }
        //    return response;
        //}

        public async Task<WebexMeeting> LaunchStartLink(string srcType, long srcId)
        {
            
            //Ex: returns: https://ocfl.webex.com/.../StartMeeting?meetingid=013f0afb7cb74a1cb4b9243940fcb40b           
            WebexMeeting omeeting = new WebexMeeting();
            var user = (new AuthService()).GetCurrentUser();
            
            //Get Webex-meetingId from Appointment.HostURL-field           
            var srcdata = _webexrepo.GetSourceDetails(srcType, srcId);

            if (srcdata == null) return omeeting;   //no-data-found

            if (srcdata.WebexMeetingId.StartsWith("https://") )
            {
                //someone has set the meeting URL manually, so use it for Admin/Staff/Host
                omeeting = new WebexMeeting() { HostUrl = srcdata.WebexMeetingId };
            }
            else 
            {
                _webexapi = _webexrepo.GetApiSettings(env);

                //use the WebexMeetingId to create Join-Links
                var response = await CreateJoinLinks(srcdata);

                // Refresh access_token if expired & then re-try.
                // NOTE: For PRD Instant Connect use: BOT Token (valid for 100 years)
                if ((int)response.StatusCode == 401)
                {
                    (_webexapi.Access_Token, _webexapi.Refresh_Token) = await GetTokensRefresh();
                    response = await CreateJoinLinks(srcdata);
                }

                if (response.IsSuccessStatusCode)
                {
                    var responsedata = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                        omeeting = WebexService.BuildMeetingUrls(responsedata);
                    else
                        omeeting.ApiError = responsedata;
                }
            }
            return omeeting;
        }


        public async Task<HttpResponseMessage> CreateJoinLinks(Data.Entities.WebexFastQRecord appt)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _webexapi.Access_Token);
            client.DefaultRequestHeaders.Add("Accept", "application/json;charset=UTF-8");

            _logger.Info("Generating Meeting Links...");
            
            var currentUser = new AuthService().GetCurrentUser();
            var payload = new
            {
                meetingId = appt.WebexMeetingId,
                joinDirectly = false,
                email = currentUser == null ? appt.EmailAddress: currentUser.Email,
                displayName = currentUser == null ? appt.CustomerName : $"{currentUser.FirstName} {currentUser.LastName}",
                expiration = 60
            };

            string jsonBody = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{_webexapi.Base_Url}/join", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.Info($"GenerateLinks.Response: {jsonResponse}");
            }
            else
            {
                _logger.Error("GenerateLinks Error: " + (int)response.StatusCode + " " + jsonResponse);
            }

            _webexrepo.LogWebexRequestToDB(appt.SrcType, appt.SrcId, $"{_webexapi.Base_Url}join", jsonBody, (int)response.StatusCode, jsonResponse, new AuthService().GetLoggedInWindowsUser());

            return response;
            // The response contains:
            // "joinLink": For standard guests
            // "startLink": For the user who will act as the host
        }

        public static WebexMeeting BuildMeetingUrls(string json)
        {
            JsonNode meetingNode = JsonNode.Parse(json);
            string baseurl = meetingNode["joinLink"].ToString();
            string hosturl = meetingNode["startLink"].ToString();
            string guesturl = meetingNode["joinLink"].ToString();
            return new WebexMeeting
            {
                BaseUrl = (string)baseurl,
                HostUrl = hosturl,
                GuestUrl = guesturl
            };
        }

        public async Task<(string, string)> GetTokensRefresh()
        {
            _logger.Info ("function : get_token_refresh()");

            var url = string.IsNullOrWhiteSpace(_webexapi.Refresh_Url) ? "https://webexapis.com/v1/access_token": _webexapi.Refresh_Url;
            var payload = new StringContent(
                $"grant_type=refresh_token&client_id={_webexapi.Client_Id}&client_secret={_webexapi.Client_Secret}&refresh_token={_webexapi.Refresh_Token}",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
            );

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = payload;
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            var newAccessToken = root.GetProperty("access_token").GetString();
            var newRefreshToken = root.GetProperty("refresh_token").GetString();

            _logger.Info ("Token returned in refresh result : " + newAccessToken);
            _logger.Info("Refresh Token returned in refresh result : " + newRefreshToken);

            return (newAccessToken, newRefreshToken);
        }

    }
       
}