using System.ComponentModel.DataAnnotations;

namespace KickRateServer.Models
{
    public class Game
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateTime GameDate { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Opponent { get; set; } = string.Empty;
        
        // ✅ תוצאות משחק
        public int? GoalsFor { get; set; }
        public int? GoalsAgainst { get; set; }
        
        [MaxLength(20)]
        public string? Result { get; set; } // "win", "draw", "loss"
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
    }
}
