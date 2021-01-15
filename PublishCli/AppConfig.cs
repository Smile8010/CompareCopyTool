using Microsoft.Extensions.Configuration;
using PublishCli.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PublishCli
{
    public static class AppConfig
    {
        public static IConfiguration ConfigurationRoot { get; set; }



        public static PublishOptions PublishOptions => ConfigurationRoot.GetSection("PublishOptions").Get<PublishOptions>();
    }
}
