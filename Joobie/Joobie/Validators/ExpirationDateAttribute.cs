using Joobie.Models.JobModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Joobie.Validators
{
    public class ExpirationDateAttribute : ValidationAttribute
    {
        public ExpirationDateAttribute()
        {
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var jobItem = (Job)validationContext.ObjectInstance;
            if (jobItem.ExpirationDate is null)
            {
                return new ValidationResult(GetErrorMessage1());
            }else if (jobItem.AddedDate is null)
            {
                var dateExpLocal = jobItem.ExpirationDate;
                if (DateTime.Compare((DateTime)dateExpLocal, DateTime.Now) <0)
                {
                    return new ValidationResult(GetErrorMessage3());
                }
                return ValidationResult.Success;
            }
            var dateExp = jobItem.ExpirationDate;
            var dateAdd = jobItem.AddedDate;
            int res = DateTime.Compare((DateTime)jobItem.ExpirationDate, (DateTime)jobItem.AddedDate);
            if (res <= 0)
            {
                return new ValidationResult(GetErrorMessage2());
            }
            return ValidationResult.Success;
        }
        private string GetErrorMessage1()
        {
            return $"Wybierz datę";
        }
        private string GetErrorMessage2()
        {
            return $"Data wygaśnięcia musi być późniejsza, niż data dodania";
        }
        private string GetErrorMessage3()
        {
            return $"Data wygaśnięcia musi być późniejsza, niż data dzisiejsza";
        }
    }
}