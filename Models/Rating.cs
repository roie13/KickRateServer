using System.ComponentModel.DataAnnotations;

namespace KickRateServer.Models
{
    public class Rating
    {
        [Key]
        public int Id { get; set; }
        
        // מי נתן את הדירוג
        [Required]
        public int RaterUserId { get; set; }
        public User? RaterUser { get; set; }
        
        // למי ניתן הדירוג
        [Required]
        public int RatedUserId { get; set; }
        public User? RatedUser { get; set; }
        
        // כמה כוכבים (1-5)
        [Required]
        [Range(1, 5)]
        public int Stars { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}