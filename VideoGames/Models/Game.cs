using System;
using System.ComponentModel.DataAnnotations;

namespace VideoGames.Models
{
    public class Game
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Genre { get; set; }

        [Required]
        public string ImageUrl { get; set; }

        [Required]
        public DateTime ReleaseDate { get; set; }

        [Required]
        public string Description { get; set; }
    }
}
