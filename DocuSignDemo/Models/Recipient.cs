using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace DocusignDemo.Models
{
    public partial class Recipient
    {
        public string Name
        {
            get;
            set;
        }
        public string Email
        {
            get;
            set;
        }
        public string Description
        {
            get;
            set;
        }
    }
}