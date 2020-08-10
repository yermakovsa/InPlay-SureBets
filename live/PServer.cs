using live.Data;
using live.Functions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.Network;


namespace live
{
    public class PServer
    {
        private static ProxyServer proxyServer;

        public PServer()
        {
            proxyServer = new ProxyServer();
        }

        public void Start()
        {
            proxyServer.BeforeResponse += OnResponse;
            proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;
            proxyServer.CertificateManager.CertificateEngine = CertificateEngine.BouncyCastleFast;
            proxyServer.EnableConnectionPool = false;
            proxyServer.ThreadPoolWorkerThread = 512;
            //proxyServer.ThreadPoolWorkerThread = Environment.ProcessorCount * 2000;
            var explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Loopback, 8000, true);
            //proxyServer.EnableConnectionPool = true;
            explicitEndPoint.BeforeTunnelConnectRequest += OnBeforeTunnelConnectRequest;
            proxyServer.AddEndPoint(explicitEndPoint);
            proxyServer.Start();
        }

        public void Stop()
        {
            proxyServer.BeforeResponse -= OnResponse;
            proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
            proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;
            proxyServer.Stop();
        }
        public static void SetExternalProxy(string IPadress, int port)
        {
            //proxyServer.setExternalProxy(IPaddress, Convert.ToInt32(portnumber));
            proxyServer.UpStreamHttpProxy = new ExternalProxy() { HostName = IPadress, Port = port };
            proxyServer.UpStreamHttpsProxy = new ExternalProxy() { HostName = IPadress, Port = port };
        }
        private async Task OnBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
            if ((!e.HttpClient.Request.Url.Contains("ero.betfair.com") && !e.HttpClient.Request.Url.Contains("ips.betfair.com")
                && !e.HttpClient.Request.Url.Contains("was.betfair.com")) || !Betfair.start) e.DecryptSsl = false;
        }
        public async Task OnResponse(object sender, SessionEventArgs e)
        {
            if (e.HttpClient.Response.StatusCode == 200 && (e.HttpClient.Request.Method == "GET" || e.HttpClient.Request.Method == "POST"))
            {
                string url = e.HttpClient.Request.Url;
                if (url.Contains("ero.betfair.com") && url.Contains("bymarket") && !url.Contains("RUNNER_DESCRIPTION"))
                {
                    Betfair.Parse(await e.GetResponseBodyAsString(), url, e.HttpClient.Request.Headers.GetAllHeaders(), false);
                }
                else if (url.Contains("eventTimeline"))
                {
                    Betfair.ParseTimeLine(await e.GetResponseBodyAsString(), url);
                }
                else if (url.Contains("was.betfair.com"))
                {
                    Betfair.walletHeaders = e.HttpClient.Request.Headers.GetAllHeaders();
                    Betfair.walletURL = url;
                }
            }
        }
        public Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
        {
            if (e.SslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                e.IsValid = true;

            return Task.CompletedTask;
        }

        public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
        {
            return Task.CompletedTask;
        }
    }
}
