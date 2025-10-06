using System.ComponentModel.DataAnnotations;

namespace StoryCAD.Models;

public class FilePathAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var path = value as string;
        if (string.IsNullOrWhiteSpace(path))
        {
            return new ValidationResult("File path is required.");
        }

        if (Path.GetInvalidPathChars().Any(path.Contains))
        {
            return new ValidationResult("File path contains invalid characters.");
        }

        // Optionally, check if file exists
        if (!File.Exists(path))
        {
            return new ValidationResult("File does not exist.");
        }

        return ValidationResult.Success;
    }
}
