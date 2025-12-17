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
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Opponent { get; set; } = string.Empty;
        
        public int CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }
    }
}
