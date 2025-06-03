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
        // Afișează lista de jocuri cu filtrare după gen și căutare după titlu
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

            if (!string.IsNullOrEmpty(gen))
            {
                jocuri = jocuri.Where(j => j.Genre.Split(',').Select(g => g.Trim()).Contains(gen));
            }

            if (!string.IsNullOrEmpty(search))
            {
                jocuri = jocuri.Where(j => j.Title.ToLower().Contains(search.ToLower()));
            }

            var jocuriSortate = jocuri.ToList();

            ViewBag.GenSelectat = gen;
            ViewBag.Genuri = genuri;

            return View(jocuriSortate);
        }

        // Afișează detaliile unui joc și recenziile lui
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

        // Postează o recenzie nouă, cu sau fără imagine
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
