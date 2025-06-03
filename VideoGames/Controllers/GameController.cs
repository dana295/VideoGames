using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using VideoGames.Models;
using System.IO;

namespace VideoGames.Controllers
{
    public class GameController : Controller
    {
        public ActionResult Index(string gen, string search)
        {
            if (Session["Username"] == null)
            {
                return RedirectToAction("Login", "User");
            }

            string filePath = Server.MapPath("~/App_Data/jocuri.xml");
            XDocument xml = XDocument.Load(filePath);

            var jocuri = xml.Root.Elements("Joc")
                .Select(x => new Game
                {
                    Title = (string)x.Element("Titlu"),
                    Genre = (string)x.Element("Gen"),
                    ReleaseDate = DateTime.Parse((string)x.Element("Data")),
                    Description = (string)x.Element("Descriere"),
                    ImageUrl = (string)x.Element("Imagine")
                });

            var genuri = jocuri
                .SelectMany(j => j.Genre.Split(','))
                .Select(g => g.Trim())
                .Distinct()
                .OrderBy(g => g)
                .ToList();

            // Aplică filtrul de gen dacă este setat
            if (!string.IsNullOrEmpty(gen))
            {
                jocuri = jocuri.Where(j => j.Genre.Split(',').Select(g => g.Trim()).Contains(gen));
            }
            //filtrare dupa titlu
            if (!string.IsNullOrEmpty(search))
            {
                jocuri = jocuri.Where(j => j.Title.ToLower().Contains(search.ToLower()));
            }


            // Citire accesări per utilizator
            string username = Session["Username"]?.ToString();
            Dictionary<string, int> accesari = new Dictionary<string, int>();

            if (username != null)
            {
                string accesePath = Server.MapPath("~/App_Data/accesari.xml");
                if (System.IO.File.Exists(accesePath))
                {
                    XDocument acceseXml = XDocument.Load(accesePath);
                    var userNode = acceseXml.Root.Elements("Utilizator")
                        .FirstOrDefault(u => (string)u.Attribute("nume") == username);

                    if (userNode != null)
                    {
                        accesari = userNode.Elements("Joc")
                            .ToDictionary(
                                j => (string)j.Attribute("nume"),
                                j => int.Parse(j.Attribute("vizite").Value)
                            );
                    }
                }
            }

            // Sortează jocurile în funcție de accesări (descrescător)
            var jocuriSortate = jocuri
                .OrderByDescending(j => accesari.ContainsKey(j.Title) ? accesari[j.Title] : 0)
                .ToList();

            ViewBag.GenSelectat = gen;
            ViewBag.Genuri = genuri;

            return View(jocuriSortate);
        }


        public ActionResult Details(string titlu)
        {
            string Normalize(string input) => input.Trim().ToLowerInvariant();
            string filePath = Server.MapPath("~/App_Data/jocuri.xml");
            XDocument xml = XDocument.Load(filePath);
            string titluNormalizat = Normalize(titlu);

            var joc = xml.Root.Elements("Joc")
                .Select(x => new Game
                {
                    Title = (string)x.Element("Titlu"),
                    Genre = (string)x.Element("Gen"),
                    ReleaseDate = DateTime.Parse((string)x.Element("Data")),
                    Description = (string)x.Element("Descriere"),
                    ImageUrl = (string)x.Element("Imagine")
                })
                .FirstOrDefault(j => Normalize(j.Title) == titluNormalizat);

            if (joc == null)
                return HttpNotFound();

            //  CONTORIZARE ACCESĂRI
            string username = Session["Username"]?.ToString();
            if (username != null)
            {
                string accesariPath = Server.MapPath("~/App_Data/accesari.xml");
                XDocument acceseDoc;
                if (System.IO.File.Exists(accesariPath))
                {
                    acceseDoc = XDocument.Load(accesariPath);
                }
                else
                {
                    acceseDoc = new XDocument(new XElement("Accesari"));
                }

                var userNode = acceseDoc.Root.Elements("Utilizator")
                    .FirstOrDefault(u => (string)u.Attribute("nume") == username);

                if (userNode == null)
                {
                    userNode = new XElement("Utilizator", new XAttribute("nume", username));
                    acceseDoc.Root.Add(userNode);
                }
                var jocNode = userNode.Elements("Joc")
                    .FirstOrDefault(j => (string)j.Attribute("nume") == joc.Title);

                if (jocNode == null)
                {
                    jocNode = new XElement("Joc", new XAttribute("nume", joc.Title), new XAttribute("vizite", 1));
                    userNode.Add(jocNode);
                }
                else
                {
                    int vizite = int.Parse(jocNode.Attribute("vizite").Value);
                    jocNode.SetAttributeValue("vizite", vizite + 1);
                }
                acceseDoc.Save(accesariPath);
            }

            // Recenzii
            string recenziiPath = Server.MapPath("~/App_Data/recenzii.xml");
            XDocument recenziiXml = XDocument.Load(recenziiPath);

            var recenzii = recenziiXml.Root.Elements("Recenzie")
                .Where(r => Normalize((string)r.Element("Titlu")) == Normalize(joc.Title))
                .Select(r => new Review
                {
                    Autor = (string)r.Element("Autor"),
                    Text = (string)r.Element("Text"),
                    Imagine = (string)r.Element("Imagine")

                }).ToList();

            ViewBag.Recenzii = recenzii;

            return View(joc);
        }


        public ActionResult PostReview(string Titlu, string Autor, string Text, HttpPostedFileBase Imagine)
        {
            if (string.IsNullOrWhiteSpace(Titlu) || string.IsNullOrWhiteSpace(Autor) || string.IsNullOrWhiteSpace(Text))
                return new HttpStatusCodeResult(400, "Date invalide");

            string imaginePath = null;

            if (Imagine != null && Imagine.ContentLength > 0)
            {
                var fileName = Path.GetFileName(Imagine.FileName);
                string folder = Server.MapPath("~/images/uploads/");
                Directory.CreateDirectory(folder);
                string fullPath = Path.Combine(folder, fileName);
                Imagine.SaveAs(fullPath);
                imaginePath = "/images/uploads/" + fileName;
            }

            string recenziiPath = Server.MapPath("~/App_Data/recenzii.xml");
            XDocument doc = System.IO.File.Exists(recenziiPath)
                ? XDocument.Load(recenziiPath)
                : new XDocument(new XElement("Recenzii"));

            XElement recenzieNoua = new XElement("Recenzie",
                new XElement("Titlu", Titlu.Trim()),
                new XElement("Autor", Autor.Trim()),
                new XElement("Text", Text.Trim())
            );

            if (imaginePath != null)
                recenzieNoua.Add(new XElement("Imagine", imaginePath));

            doc.Root.Add(recenzieNoua);
            doc.Save(recenziiPath);

            TempData["ReviewSuccess"] = "Recenzia a fost trimisă cu succes!";
            return RedirectToAction("Details", new { titlu = Titlu });
        }

    }
}
