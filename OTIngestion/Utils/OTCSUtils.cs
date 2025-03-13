using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json.Linq;
using DocumentFormat.OpenXml.Office2016.Excel;

namespace OTIngestion.Utils
{
    public class OTCSUtils
    {
        internal const int REST_TIMEOUT = 60000;
        //internal const int REST_TIMEOUT = Timeout.Infinite;

        public string _rest_url;
        public string _userName;
        public string _password;
        public static AuthResponse authResponse = new AuthResponse();
        
        public OTCSUtils()
        {
            OTConfig otConfig = OTConfig.GetOTConfig();
            _rest_url = otConfig.rest_url;
            _userName = otConfig.UserName;
            _password = otConfig.DecodedPassword;
        }

        public string GetAuthCode()
        {
            if (IsCSTicketExpired(authResponse))
            {
                NlogUtils.Logger.Info($"Retrieving OTCSTicket for the {_userName}");
                var client = new RestClient(_rest_url + "/api/v1/auth");

                var request = new RestRequest()
                {
                    Method = Method.Post,
                    AlwaysMultipartFormData = true
                };
                request.Timeout = REST_TIMEOUT;

                request.AddParameter("username", _userName);
                request.AddParameter("password", _password);
                RestResponse response = client.Execute(request);

                NlogUtils.Logger.Info($"OTCSTicket response: {response}");

                dynamic api = JObject.Parse(response.Content);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string otcsTicket = api.ticket;
                    authResponse.OTcsTicket = otcsTicket.Trim();
                    authResponse.TicketCreationTime = DateTime.Now;

                    try
                    {
                        if (response.Headers != null)
                        {
                            DateTime? date = null, expDate = null;

                            foreach (var headerItem in response.Headers)
                            {
                                if (headerItem.Name == "Date")
                                {
                                    date = Convert.ToDateTime(headerItem.Value);
                                }

                                if (headerItem.Name == "OTCSTicketExpires")
                                {
                                    expDate = Convert.ToDateTime(headerItem.Value);
                                }
                            }

                            if (date.HasValue && expDate.HasValue)
                            {
                                authResponse.TicketExpirationTime = DateTime.Now + (expDate.Value - date.Value) * 0.9;
                            }
                        }

                        NlogUtils.Logger.Info($"OTCSTicket expiration time: {authResponse.TicketExpirationTime}");
                    }
                    catch
                    { }
                }
                else
                {
                    if (api.error == null)
                    {
                        throw new Exception(api.Error.ToString());
                    }
                    else
                    {
                        throw new Exception(api.error.ToString());
                    }
                }
            }

            return authResponse.OTcsTicket;
        }

        public bool IsCSTicketExpired(AuthResponse authResponse)
        {
            bool ticketExpired = true;
            if (!string.IsNullOrEmpty(authResponse.OTcsTicket))
            {
                try
                {
                    if (authResponse.TicketExpirationTime < DateTime.Now)
                    {
                        string strUrl = _rest_url + "/api/v1/auth";
                        var client = new RestClient(strUrl);

                        //GetAuthCode
                        var request = new RestRequest()
                        {
                            Method = Method.Get
                        };

                        request.AddHeader("otcsticket", authResponse.OTcsTicket);
                        RestResponse response = client.Execute(request);

                        dynamic apiRespoonse = JObject.Parse(response.Content);
                        if (!string.IsNullOrWhiteSpace(apiRespoonse.data.name))
                        {
                            return false;
                        }

                        Console.WriteLine(response.Content);
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    return ticketExpired;
                }
            }
            return ticketExpired;
        }

        #region Rest Request

        internal RestClient GenerateRestClient(string apiUrl)
        {
            var options = new RestClientOptions(apiUrl)
            {
                ThrowOnAnyError = true,
                MaxTimeout = REST_TIMEOUT
            };
            var client = new RestClient(options);
            return client;
        }

        internal RestRequest GenerateRestRequest(RestSharp.Method method)
        {
            var request = new RestRequest
            {
                Method = method,
                AlwaysMultipartFormData = true,
                Timeout = REST_TIMEOUT
            };

            request.AddHeader("otcsticket", GetAuthCode());

            return request;
        }

        #endregion


    }

    public class AuthResponse
    {
        public string OTcsTicket { get; set; }
        public DateTime TicketCreationTime { get; set; }
        public DateTime TicketExpirationTime { get; set; }
    }
}