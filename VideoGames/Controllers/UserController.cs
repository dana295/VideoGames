using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Linq;
using VideoGames.Models;

public class UserController : Controller
{
    public ActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public ActionResult Login(string username, string password)
    {
        // VALIDĂRI personalizate
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Câmpurile nu pot fi goale.";
            return View();
        }

        if (password.Length < 5)
        {
            ViewBag.Error = "Parola trebuie să aibă minim 5 caractere.";
            return View();
        }

        // Autentificare normală
        string path = Server.MapPath("~/App_Data/Utilizatori.xml");
        XDocument doc = XDocument.Load(path);

        var utilizatori = doc.Descendants("utilizator")
            .Select(x => new User
            {
                Username = x.Element("username")?.Value,
                Parola = x.Element("parola")?.Value
            }).ToList();

        var user = utilizatori.FirstOrDefault(u => u.Username == username && u.Parola == password);

        if (user != null)
        {
            Session["Username"] = user.Username;
            return RedirectToAction("Index", "Game");
        }
        else
        {
            ViewBag.Error = "Date de autentificare invalide.";
            return View();
        }
    }

    public ActionResult Logout()
    {
        Session.Clear();
        return RedirectToAction("Login");
    }

    public ActionResult Profile()
    {
        if (Session["Username"] == null)
        {
            return RedirectToAction("Login");
        }

        string username = Session["Username"].ToString();
        string recenziiPath = Server.MapPath("~/App_Data/recenzii.xml");
        XDocument doc = XDocument.Load(recenziiPath);

        var recenziiUtilizator = doc.Root.Elements("Recenzie")
            .Where(r => (string)r.Element("Autor") == username)
            .Select(r => new Review
            {
                Autor = (string)r.Element("Autor"),
                Text = (string)r.Element("Text"),
                Titlu = (string)r.Element("Titlu"),
                Imagine = (string)r.Element("Imagine")
            }).ToList();

        return View(recenziiUtilizator);
    }
    [HttpPost]
    public ActionResult DeleteReview(string Titlu, string Text)
    {
        if (Session["Username"] == null)
            return RedirectToAction("Login");

        string username = Session["Username"].ToString();
        string recenziiPath = Server.MapPath("~/App_Data/recenzii.xml");
        XDocument doc = XDocument.Load(recenziiPath);

        var recenzie = doc.Root.Elements("Recenzie")
            .FirstOrDefault(r =>
                (string)r.Element("Titlu") == Titlu &&
                (string)r.Element("Autor") == username &&
                (string)r.Element("Text") == Text
            );

        if (recenzie != null)
        {
            recenzie.Remove();
            doc.Save(recenziiPath);
        }
        TempData["Message"] = "Recenzia a fost ștearsă cu succes!";
        return RedirectToAction("Profile");
    }
    [HttpPost]
    public ActionResult EditReview(string Titlu, string Text)
    {
        if (Session["Username"] == null)
            return RedirectToAction("Login");

        var review = new Review
        {
            Titlu = Titlu,
            Text = Text
        };

        return View(review); // View-ul va fi `EditReview.cshtml`
    }

    [HttpPost]
    public ActionResult SaveReview(string Titlu, string TextNou)
    {
        if (Session["Username"] == null)
            return RedirectToAction("Login");

        string username = Session["Username"].ToString();
        string recenziiPath = Server.MapPath("~/App_Data/recenzii.xml");
        XDocument doc = XDocument.Load(recenziiPath);

        var recenzie = doc.Root.Elements("Recenzie")
            .FirstOrDefault(r =>
                (string)r.Element("Titlu") == Titlu &&
                (string)r.Element("Autor") == username
            );

        if (recenzie != null)
        {
            recenzie.Element("Text").Value = TextNou;
            doc.Save(recenziiPath);
        }
        TempData["Message"] = "Recenzia a fost modificată cu succes!";
        return RedirectToAction("Profile");
    }

}
