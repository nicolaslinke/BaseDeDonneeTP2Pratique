﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Principal;
using WrapUpBilleterie.Data;
using WrapUpBilleterie.ViewModels;
using WrapUpBilleterie.Models;

namespace WrapUpBilleterie.Controllers
{
    public class ClientsController : Controller
    {
        readonly R22_BilleterieContext _context;
        public ClientsController(R22_BilleterieContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["PrenomNom"] = "visiteur";
            IIdentity? identite = HttpContext.User.Identity;
            if(identite != null && identite.IsAuthenticated)
            {
                string courriel = HttpContext.User.FindFirstValue(ClaimTypes.Name);
                Client? client = await _context.Clients.FirstOrDefaultAsync(x => x.Courriel == courriel);
                if (client != null)
                {
                    // Pour dire "Bonjour X" sur l'index
                    ViewData["PrenomNom"] = client.Prenom + " " + client.Nom;
                }
            }
            return View();
        }

        // Inscription en requête get
        public IActionResult Inscription()
        {
            return View();
        }

        // Inscription en requête post
        [HttpPost]
        public async Task<IActionResult> Inscription(InscriptionViewModel ivm)
        {
            // A COMPLETER LORS DE L'ETAPE 1
            bool existeDeja = await _context.Clients.AnyAsync(x => x.Courriel == ivm.Courriel);
            if (existeDeja)
            {
                ModelState.AddModelError("Courriel", "Ce courriel est déjà pris.");
                return View(ivm);
            }
            string query = "EXEC Clients.USP_CreerClient @Nom, @Prenom, @Courriel, @MotDePasse";
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter{ParameterName = "@Nom", Value = ivm.Nom},
                new SqlParameter{ParameterName = "@Prenom", Value = ivm.Prenom},
                new SqlParameter{ParameterName = "@Courriel", Value = ivm.Courriel},
                new SqlParameter{ParameterName = "@MotDePasse", Value = ivm.MotDePasse}
            };
            try
            {
                await _context.Database.ExecuteSqlRawAsync(query, parameters.ToArray());
            }
            catch(Exception)
            {
                ModelState.AddModelError("", "Une erreur est survenue. Veuillez réessayez.");
                return View(ivm);
            }

            return RedirectToAction("Connexion", "Utilisateurs");
        }

        public IActionResult Connexion()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Connexion(ConnexionViewModel cvm)
        {
            // A COMPLETER LORS DE L'ÉTAPE 1
            string query = "EXEC Clients.USP_AuthClient @Courriel, @MotDePasse";
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter{ParameterName = "@Courriel", Value = cvm.Courriel},
                new SqlParameter{ParameterName = "@MotDePasse", Value = cvm.MotDePasse}
            };
            Client? client = (await _context.Clients.FromSqlRaw(query, parameters.ToArray()).ToListAsync()).FirstOrDefault();
            if(client == null)
            {
                ModelState.AddModelError("", "Courriel ou mot de passe invalide");
                return View(cvm);
            }

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, client.ClientId.ToString()),
                new Claim(ClaimTypes.Name, client.Courriel)
            };

            ClaimsIdentity identite = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            ClaimsPrincipal principal = new ClaimsPrincipal(identite);
            await HttpContext.SignInAsync(principal);

            return RedirectToAction("Index", "Clients");
        }

        [HttpGet]
        public async Task<IActionResult> Deconnexion()
        {
            // Cette ligne mange le cookie 🍪 Slurp
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index");
        }

    }
}
