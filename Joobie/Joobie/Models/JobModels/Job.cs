using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Joobie.Validators;

namespace Joobie.Models.JobModels
{
    public class Job
    {
        [Key]
        public long Id { get; set; }

        [Display(Name = "Nazwa")]
        [Required(ErrorMessage = "Nazwa jest wymagana")]
        public string Name { get; set; }

        public Boolean isActive { get; set; } = true;

        [Required(ErrorMessage = "Pole wymagane")]
        [Display(Name = "Opis")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Pole wymagane")]
        [Display(Name = "Lokalizacja")]
        public string Localization { get; set; }

        [Required(ErrorMessage = "Pole wymagane")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Data dodania")]
        [AddedDate]
        public DateTime? AddedDate { get; set; }

        [Required(ErrorMessage = "Pole wymagane")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Termin wygaśnięcia")]
        [ExpirationDate]
        public DateTime? ExpirationDate { get; set; }


        [Required(ErrorMessage ="Pole wymagane")]
        [Display(Name = "Wynagrodzenie")]
        public string Salary { get; set; }

        [Required]
        [Display(Name = "Kategoria")]
        public byte CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        [Display(Name = "Kategoria")]
        public virtual Category Category { get; set; }

        [Required]
        [Display(Name = "Rodzaj kontraktu")]
        public byte TypeOfContractId { get; set; }

        [ForeignKey("TypeOfContractId")]
        [Display(Name = "Rodzaj kontraktu")]
        public virtual TypeOfContract TypeOfContract { get; set; }

        [Required]
        [Display(Name = "Godziny pracy")]
        public byte WorkingHoursId { get; set; }

        [ForeignKey("WorkingHoursId")]
        [Display(Name = "Godziny pracy")]
        public virtual WorkingHours WorkingHours { get; set; }

        [Display(Name = "Użytkownik")]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        [Display(Name = "Firma")]
        public virtual ApplicationUser ApplicationUser { get; set; }

        public ICollection<CVJobApplicationUser> CVJobApplicationUser { get; } = new List<CVJobApplicationUser>();

    }
}
