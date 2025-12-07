using System.ComponentModel.DataAnnotations;

namespace KickRateServer.DTOs
{
    public class CreateGameDto
    {
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
        
        [Required]
        public int CreatedByUserId { get; set; }
    }
}