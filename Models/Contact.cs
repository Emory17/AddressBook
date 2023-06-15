using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Security.Policy;

namespace AddressBook.Models
{
    public class Contact
    {
        //Primary Key
        public int Id { get; set; }

        //Foreign Key
        [Required]
        public string? AppUserId { get; set; }

        [Required]
        [Display(Name = "First Name")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and max {1} characters long.", MinimumLength = 2)]
        public string? FirstName { get; set; }
        [Required]
        [Display(Name = "Last Name")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and max {1} characters long.", MinimumLength = 2)]
        public string? LastName { get; set; }

        [NotMapped]
        public string? FullName { get { return $"{FirstName} {LastName}"; } }

        [Required]
        [DataType(DataType.Date)]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Birthday")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; } 

        public string? Address1 { get; set; }

        public string? Address2 { get; set; }

        public string? City { get; set; }

        public Enums.States State { get; set; }

        [Display(Name = "Zip Code")]
        [DataType(DataType.PostalCode)]
        public int ZipCode { get; set; }

        [Required]
        [Display(Name = "Email Address")]
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        [DataType(DataType.PhoneNumber)]
        public string? PhoneNumber { get; set; }

        // Image Properties
        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public byte[]? ImageData { get; set; }

        public string? ImageType { get; set; }

        // Navigation Properties
        public virtual AppUser? AppUser { get; set; }

        public virtual ICollection<Category> Categories { get; set; } = new HashSet<Category>();
    }
}
