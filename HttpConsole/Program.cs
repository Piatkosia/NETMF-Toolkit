﻿using System;
using System.Collections.Generic;
using System.Text;
using MSchwarz.Net.Web;
using System.IO;
using MSchwarz.IO;
using MSchwarz.Net.Dns;

namespace HttpConsole
{
	class Program
	{
		static void Main(string[] args)
		{

            DnsResolver dns = new DnsResolver();
            dns.LoadNetworkConfiguration();

            DnsResponse res = dns.Resolve(new DnsRequest(new Question("microsoft.com", DnsType.MX, DnsClass.IN)));



            foreach (Answer a in res.Answers)
            {
                MXRecord mx = a.Record as MXRecord;

                if (mx != null)
                    Console.WriteLine(mx.Preference + " " + mx.DomainName);
            }
            return;




            using (HttpServer http = new HttpServer(new MyHttpHandler(Path.Combine(Environment.CurrentDirectory, "..\\..\\root"))))
            {
                http.OnLogAccess += new HttpServer.LogAccessHandler(http_OnLogAccess);
                http.Start();

                Console.ReadLine();
                Console.WriteLine("Shutting down http server...");
            }

            Console.WriteLine("Done.");
		}

        static void http_OnLogAccess(LogAccess data)
        {
            Console.WriteLine(data.ClientIP + "\t" + data.RawUri + "\t" + data.Method + "\t" + data.Duration + " msec\t" + data.BytesReceived + " bytes\t" + data.BytesSent + " bytes");
            Console.WriteLine(data.UserAgent);
            if(data.HttpReferer != null) Console.WriteLine(data.HttpReferer);

            try
            {
                DnsResolver dns = new DnsResolver();
                dns.LoadNetworkConfiguration();

                DnsResponse res = dns.Resolve(new DnsRequest(new Question(data.ClientIP.ToString(), DnsType.PTR, DnsClass.IN)));

               

                if (res.Answers.Count > 0)
                    Console.WriteLine(res.Answers[0].ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("------------------------------------------------------------");
        }
	}

    class MyHttpHandler : IHttpHandler
    {
        private string _rootFolder;

        public MyHttpHandler(string rootFolder)
        {
            _rootFolder = rootFolder;
        }

        #region IHttpHandler Members

        public void ProcessRequest(HttpContext context)
        {
            if (!String.IsNullOrEmpty(_rootFolder))
            {
                string filename = Path.Combine(_rootFolder, context.Request.RawUrl.Replace("/", "\\").Substring(1));
                if (File.Exists(filename))
                {
                    if (Path.GetExtension(filename) == ".htm")
                        context.Response.ContentType = "text/html; charset=UTF-8";
                    else if (Path.GetExtension(filename) == ".jpg")
                        context.Response.ContentType = "image/jpeg";

                    context.Response.Write(File.ReadAllBytes(filename));
                    return;
                }
            }

            switch(context.Request.Path)
            {
                case "/test":
                    context.Response.Redirect("/test.aspx");
                    break;

                case "/test2.aspx":
                    context.Response.ContentType = "text/html; charset=UTF-8";
                    context.Response.Write("<html><head><title></title></head><body>" + Encoding.UTF8.GetString(context.Request.Body) + "</body></html>");
                    break;

                case "/cookie":
                    context.Response.ContentType = "text/html; charset=UTF-8";
                    context.Response.Write("<html><head><title></title></head><body>");

                    if (context.Request.Cookies.Length > 0)
                    {
                        foreach (HttpCookie c in context.Request.Cookies)
                            context.Response.WriteLine("Cookie " + c.Name + " = " + c.Value + "<br/>");
                    }

                    HttpCookie cookie = new HttpCookie("test", DateTime.Now.ToString());
                    cookie.Expires = DateTime.Now.AddDays(2);
                    context.Response.SetCookie(cookie);
                    context.Response.WriteLine("</body></html>");

                    break;

                case "/test.aspx":
                    context.Response.ContentType = "text/html; charset=UTF-8";
                    context.Response.Write("<html><head><title></title><script type=\"text/javascript\" src=\"/scripts/test.js\"></script></head><body><form action=\"/test2.aspx\" method=\"post\"><input type=\"text\" id=\"txtbox1\" name=\"txtbox1\"/><input type=\"submit\" value=\"Post\"/></form></body></html>");
                    break;

                case "/scripts/test.js":
                    context.Response.ContentType = "text/javascript";
                    context.Response.Write(@"
var c = 0;
function test() {
    var x = window.ActiveXObject ? new ActiveXObject(""Microsoft.XMLHTTP"") : new XMLHttpRequest();
    x.onreadystatechange = function() {
        if(x.readyState == 4) {
            document.getElementById('txtbox1').value = x.responseText;
            if(++c < 5)
                setTimeout(test, 1);
        }
    }
    x.open(""POST"", ""/test.ajax?x="" + c, true);
    x.send("""" + c);
}
setTimeout(test, 1);
");
                    break;

                case "/test.ajax":
                    context.Response.Write("ajax = " + Encoding.UTF8.GetString(context.Request.Body));
                    break;

                default:
                    context.Response.ContentType = "text/html; charset=UTF-8";
                    context.Response.Write("<html><head><title>Control My World - How to switch lights on and heating off?</title></head><body>");
                    
                    
                    context.Response.Write("<h1>Welcome to my .NET Micro Framework web server</h1><p>This demo server is running on a Tahoe-II board using XBee modules to communicate with XBee sensors from Digi.</p><p>On my device the current date is " + DateTime.Now + "</b><p><b>RawUrl: " + context.Request.RawUrl + "</b><br/>" + context.Request.GetHeaderValue("User-Agent") + "</p>");

                    if (context.Request.Params != null && context.Request.Params.Length > 0)
                    {
                        foreach (HttpParameter p in context.Request.Params)
                            context.Response.Write(p.Name + " = " + p.Value + "<br/>");
                    }

                    if (context.Request.Body != null)
                    {
                        context.Response.Write("<h3>Received Bytes:</h3>");
                        context.Response.Write(context.Request.Body);
                        context.Response.Write("<hr size=1/>");
                    }

                    context.Response.Write(@"<p><a href=""index.htm"">Demo (files on SD card)</a><br/>
<a href=""test"">Redirect, AJAX and Form Test</a><br/>
<a href=""cookie"">Cookie Test</a><br/></p>
<hr size=1/>
<p>Any feedback welcome: <a href=""http://weblogs.asp.net/mschwarz/contact.aspx"">contact</a>
<a href=""http://michael-schwarz.blogspot.com/"">My Blog</a> <a href=""http://weblogs.asp.net/mschwarz/"">My Blog (en)</a><br/>
<a href=""http://www.control-my-world.com/"">Control My World</a></p>
</body></html>");
                    break;
            }
        }

        #endregion
    }

}
