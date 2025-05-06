using System.ComponentModel.DataAnnotations;

namespace TiketixAPI.Models.DTO
{
    public class TransactionDTO
    {
        [Required]
        public int userID {  get; set; }

        [Required]
        public int scheduleID { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one seat must be selected.")]
        public List<String> seats { get; set; }
    }
}
