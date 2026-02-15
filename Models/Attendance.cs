using System.ComponentModel.DataAnnotations;

namespace KickRateServer.Models
{
    public class Attendance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GameId { get; set; }
        public Game? Game { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public bool IsAttending { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}