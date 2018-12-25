using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DocuSignDemo.Models
{
    public class PayloadData
    {
        public string DocumentBase64 { get; set; }

        public string UserNameFile { get; set; }
    }
}