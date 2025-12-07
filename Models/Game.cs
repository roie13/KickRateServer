using System.ComponentModel.DataAnnotations;

namespace KickRateServer.Models
{
    public class Game
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public DateOnly GameDate { get; set; }
        
        [Required]
        public TimeOnly GameTime { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Opponent { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
    }
}