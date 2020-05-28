using Joobie.Models.JobModels;
using System;
using System.ComponentModel.DataAnnotations;

namespace Joobie.Validators
{
    public class AddedDateAttribute : ValidationAttribute
    {

        public AddedDateAttribute()
        {
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var jobItem = (Job)validationContext.ObjectInstance;
            if(jobItem.AddedDate is null)
            {
                return new ValidationResult(GetErrorMessage());
            }
            else
            {
                var date = (DateTime)jobItem.AddedDate;
                date = date.AddDays(1);
                if (date < DateTime.Now)
                {
                    return new ValidationResult(GetErrorMessage2());
                }
            }



            return ValidationResult.Success;
        }

        private string GetErrorMessage()
        {
            return $"Wybierz datę";
        }

        private string GetErrorMessage2()
        {
            return $"Nieprawidłowa data dodania";
        }
    }
}