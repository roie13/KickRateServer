   using System.ComponentModel.DataAnnotations;

   namespace KickRateServer.Models
   {
       public class User
       {
           [Key]
           public int Id { get; set; }
           
           [Required]
           [MaxLength(50)]
           public string Username { get; set; } = string.Empty;
           
           [Required]
           public string PasswordHash { get; set; } = string.Empty;
           
           public bool IsAdmin { get; set; } = false;
           
           public DateTime CreatedAt { get; set; } = DateTime.Now;
           
           public ICollection<Game> GamesCreated { get; set; } = new List<Game>();
       }
   }
