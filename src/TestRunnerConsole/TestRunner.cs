using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Threading;
using TitaniumProxy;


namespace TestRunnerConsole
{
    public class TestRunner
    {
        private const string _proxyHost = "http://localhost:8000";
        private IWebDriver _webDriver;
        private static ProxyTestController controller;

        public void Setup()
        {
            controller = new ProxyTestController();
            Console.WriteLine(">> Setting up proxy server");
            controller.StartProxy();
            Proxy seleniumProxy = AttachSeleniumToProxy();
            Console.WriteLine(">> Proxy server started");
            Console.WriteLine(">> Getting web driver...");
            GetWebDriver(seleniumProxy);
            Console.WriteLine(">> Created web driver...");
        }

        public void RunTests()
        {
            Setup();
            Thread.Sleep(2000);
            Console.WriteLine("Running test...");
            _webDriver.Navigate().GoToUrl("https://www.google.co.za");
            Console.WriteLine("Test finished");
            Thread.Sleep(5000);
            TearDown();
        }

        private OpenQA.Selenium.Proxy AttachSeleniumToProxy()
        {
            var seleniumProxy = new OpenQA.Selenium.Proxy()
            {
                HttpProxy = _proxyHost,
                SslProxy = _proxyHost,
                FtpProxy = _proxyHost
            };
            return seleniumProxy;
        }

        private void GetWebDriver(OpenQA.Selenium.Proxy seleniumProxy = null)
        {
            var options = new ChromeOptions { Proxy = seleniumProxy };
            _webDriver = new ChromeDriver(options);
        }

        public void TearDown()
        {
            _webDriver.Dispose();
            controller.Stop();
        }


    }
}
