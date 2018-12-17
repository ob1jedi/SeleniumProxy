using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Models;
using TitaniumProxy.Contracts.Enums;
using TitaniumProxy.Models;
using TitaniumProxy.Services;

namespace TitaniumProxy
{
    public class ProxyTestController
    {
        private readonly SemaphoreSlim @lock = new SemaphoreSlim(1);
        private readonly ProxyServer proxyServer;
        private ExplicitProxyEndPoint explicitEndPoint;
        private IEnumerable<ProxyRule> rules;
        public ProxyTestController(IEnumerable<ProxyRule> rules = null)
        {
            rules = rules ?? new List<ProxyRule>();
            proxyServer = new ProxyServer();
            proxyServer.ExceptionFunc = async exception =>
            {
                if (exception is ProxyHttpException phex)
                {
                    await WriteToConsole(exception.Message + ": " + phex.InnerException?.Message, ConsoleColor.Red);
                }
                else
                {
                    await WriteToConsole(exception.Message, ConsoleColor.Red);
                }
            };
            proxyServer.ForwardToUpstreamGateway = true;
            proxyServer.CertificateManager.SaveFakeCertificates = true;
        }

        public void StartProxy()
        {
            proxyServer.BeforeRequest += OnRequest;
            proxyServer.BeforeResponse += OnResponse;
            proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;
            explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000);
            explicitEndPoint.BeforeTunnelConnectRequest += OnBeforeTunnelConnectRequest;
            explicitEndPoint.BeforeTunnelConnectResponse += OnBeforeTunnelConnectResponse;
            proxyServer.AddEndPoint(explicitEndPoint);
            proxyServer.Start();
            foreach (var endPoint in proxyServer.ProxyEndPoints)
            {
                Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ", endPoint.GetType().Name,
                    endPoint.IpAddress, endPoint.Port);
            }
        }

        public void Stop()
        {
            explicitEndPoint.BeforeTunnelConnectRequest -= OnBeforeTunnelConnectRequest;
            explicitEndPoint.BeforeTunnelConnectResponse -= OnBeforeTunnelConnectResponse;

            proxyServer.BeforeRequest -= OnRequest;
            proxyServer.BeforeResponse -= OnResponse;
            proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;

            proxyServer.Stop();

            // remove the generated certificates
            //proxyServer.CertificateManager.RemoveTrustedRootCertificates();
        }

        private async Task OnBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
            //string hostname = e.HttpClient.Request.RequestUri.Host;
            //await WriteToConsole("Tunnel to: " + hostname);

            //if (hostname.Contains("dropbox.com"))
            //{
            //    // Exclude Https addresses you don't want to proxy
            //    // Useful for clients that use certificate pinning
            //    // for example dropbox.com
            //    e.DecryptSsl = false;
            //}
        }

        private Task OnBeforeTunnelConnectResponse(object sender, TunnelConnectSessionEventArgs e)
        {
            return Task.FromResult(false);
        }


        // intecept & cancel redirect or update requests
        private async Task OnRequest(object sender, SessionEventArgs e)
        {
            await WriteToConsole("Request: " + e.WebSession.Request.Host);
            e = await EvaluateRulesAndApplyTransformations(EventEnum.request, e);
            await WriteToConsole("------------------");
        }

        private async Task OnResponse(object sender, SessionEventArgs e)
        {
            await WriteToConsole("RESPONSE to: " + e.WebSession.Request.Host);
            e = await EvaluateRulesAndApplyTransformations(EventEnum.response, e);
            await WriteToConsole("==================");
            //string requestUri = "http://197.234.158.50:3060/device/gsm1/121300/next/";
            //if (e.WebSession.Request.RequestUri.ToString() == requestUri)
            //{
            //    if (e.WebSession.Request.Method == "GET" || e.WebSession.Request.Method == "POST")
            //    {
            //        if (e.WebSession.Response.StatusCode == (int)HttpStatusCode.OK)
            //        {
            //            if (e.WebSession.Response.ContentType != null && e.WebSession.Response.ContentType.Trim().ToLower().Contains("application/json"))
            //            {
            //                string body = await e.GetResponseBodyAsString();
            //                await WriteToConsole(body);
            //                body = "{\"other\":\"potato\"}";
            //                e.SetResponseBodyString(body);
            //            }
            //        }
            //    }
            //}
        }

        // Modify response
        private async Task MultipartRequestPartSent(object sender, MultipartRequestPartSentEventArgs e)
        {
            //var session = (SessionEventArgs)sender;
            //await WriteToConsole("Multipart form data headers:");
            //foreach (var header in e.Headers)
            //{
            //    await WriteToConsole(header.ToString());
            //}
        }

        private async Task<SessionEventArgs> EvaluateRulesAndApplyTransformations(EventEnum onEvent, SessionEventArgs e)
        {
            var worldState = await BuildWorldState(onEvent, e);
            var rules = GetRulesFromFile();
            var passedRules = ProxyRuleEvaluator.EvaluateRulesAndReturnPassedRules(rules, worldState);
            await ApplyTransformations(passedRules, e);
            return e;
        }

        private IEnumerable<ProxyRule> GetRulesFromFile()
        {
            var rulesPath = $@"{Directory.GetCurrentDirectory()}\ProxyRules\ProxyRules.json";
            var jsonString = File.ReadAllText(rulesPath);
            var ruleCollection = JsonConvert.DeserializeObject<RuleCollection>(jsonString);
            return ruleCollection.Rules;
        }

        private async Task ApplyTransformations(IEnumerable<ProxyRule> rules, SessionEventArgs e)
        {
            foreach (var rule in rules)
            {
                await WriteToConsole("Applied Rule: " + rule.Name, ConsoleColor.Cyan);
                foreach (var trans in rule.ThenSet) { 
                    switch (trans.Part)
                    {
                        case PartEnum.Url:
                            e.Redirect(trans.Value);
                            break;
                        case PartEnum.Body:
                            if (trans.On == EventEnum.request) e.SetRequestBodyString(trans.Value);
                            if (trans.On == EventEnum.response) e.SetResponseBodyString(trans.Value);
                            break;
                    }
                }
            }
        }

        private async Task<IEnumerable<Assertion>> BuildWorldState(EventEnum onEvent, SessionEventArgs e)
        {
            var state = new List<Assertion>();
            state.Add(new Assertion
            {
                Part = PartEnum.Url,
                Op = OpEnum.Equals,
                Value = e.WebSession.Request.RequestUri.ToString()
            });
            state.Add(new Assertion
            {
                Part = PartEnum.Headers,
                Op = OpEnum.Equals,
                Value = (onEvent == EventEnum.request) ? e.WebSession.Request.HeaderText : e.WebSession.Response.HeaderText
            });
            state.Add(new Assertion
            {
                Part = PartEnum.StatusCode,
                Op = OpEnum.Equals,
                Value = (onEvent == EventEnum.request) ? null : e.WebSession.Response.StatusCode.ToString()
            });     
            state.Add(await GetBodyAsAssertion(onEvent, e));
            return state;
        }

        private static async Task<Assertion> GetBodyAsAssertion(EventEnum onEvent, SessionEventArgs e)
        {
            var body = new Assertion
            {
                Part = PartEnum.Body,
                Op = OpEnum.Equals,
            };
            if (onEvent == EventEnum.response)
                body.Value = e.WebSession.Response.HasBody ? await e.GetResponseBodyAsString() : null;
            if (onEvent == EventEnum.request)
                body.Value = e.WebSession.Request.HasBody ? await e.GetRequestBodyAsString() : null;
            return body;
        }

        /// <summary>
        ///     Allows overriding default certificate validation logic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            // set IsValid to true/false based on Certificate Errors
            if (e.SslPolicyErrors == SslPolicyErrors.None)
            {
                e.IsValid = true;
            }

            return Task.FromResult(0);
        }

        /// <summary>
        ///     Allows overriding default client certificate selection logic during mutual authentication
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
        {
            // set e.clientCertificate to override

            return Task.FromResult(0);
        }

        private async Task WriteToConsole(string message, ConsoleColor color = ConsoleColor.White)
        {
            await @lock.WaitAsync();
            //if (color == ConsoleColor.Red)
            //{
            ConsoleColor existing = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = existing;
            //}
            //else
            //{
            //    Console.WriteLine(message);
            //}
            @lock.Release();
        }

        ///// <summary>
        ///// User data object as defined by user.
        ///// User data can be set to each SessionEventArgs.HttpClient.UserData property
        ///// </summary>
        //public class CustomUserData
        //{
        //    public HeaderCollection RequestHeaders { get; set; }
        //    public byte[] RequestBody { get; set; }
        //    public string RequestBodyString { get; set; }
        //}
    }
}
