using FastQ.Web.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FastQ.Web.Controllers
{
    [FQAuthorizeUser()]
    public class BaseController : Controller
    {
        // Using this BaseController to inherit FQAuthorization
    }
}