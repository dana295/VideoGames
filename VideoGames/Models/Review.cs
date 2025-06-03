using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace VideoGames.Models
{
    public class Review
    {
        public string Titlu {  get; set; }
        public string Autor { get; set; }
        public string Text { get; set; }
        public string Imagine { get; set; }
    }
}
