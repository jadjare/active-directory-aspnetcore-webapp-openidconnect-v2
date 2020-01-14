﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApp_OpenIDConnect_DotNet.Models;

namespace WebApp_OpenIDConnect_DotNet.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        public IActionResult Index()
        {
            ViewData["User"] = HttpContext.User;
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }

        [Authorize("InAuthorizedGroup")]
        public IActionResult TestAuth()
        {
            ViewData["User"] = HttpContext.User;
            ViewData["TestAuthResult"] = "Authorised via custom requirement for the 'InAuthorizedGroup' policy authorised by Group Id";
            return View("Index");
        }
    }
}