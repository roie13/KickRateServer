using System.ComponentModel.DataAnnotations;

namespace KickRateServer.DTOs
{
    public class CreateGameDto
    {
        [Required]
        public string GameDate { get; set; } = string.Empty;

        [Required]
        public string GameTime { get; set; } = string.Empty;

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
