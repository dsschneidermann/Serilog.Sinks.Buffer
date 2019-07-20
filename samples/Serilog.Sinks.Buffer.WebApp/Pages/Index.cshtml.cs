// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Serilog.Sinks.Buffer.WebApp.Pages
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            Log.Debug(
                "--> Debugging my request <--\nHeaders: {@HttpRequestHeaders}\nBody: {@HttpRequestBody}",
                HttpContext.Request.Headers.ToDictionary(kv => kv.Key, kv => kv.Value),
                new StreamReader(HttpContext.Request.Body).ReadToEnd()
            );

            throw new Exception("My exception to cause a dump of the ASP.NET Core log");
        }
    }
}
