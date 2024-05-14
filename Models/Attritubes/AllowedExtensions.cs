using Common.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Models.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AllowedExtensions : ValidationAttribute
    {
        // MB
        private readonly int? _fileSize;
        private readonly List<string>? _extensions;

        public AllowedExtensions(UploadType fileType)
        {
            (int? fileSize, List<string>? extensions) result = fileType switch
            {
                UploadType.Excel => (null, new() { ".csv", ".xlsx" }),
                UploadType.Image => (2, new() { ".png", ".jpg", ".jpeg" }),
                _ => (null, null),
            };

            _fileSize = result.fileSize;
            _extensions = result.extensions;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (_extensions is null || value is null)
                return ValidationResult.Success;

            if (!IsList(value))
            {
                var file = (value as IFormFile)!;

                if (!CheckFileExtension(file))
                    return new ValidationResult(ExtensionErrorMessage);

                if (!CheckFileSize(file))
                    return new ValidationResult(FileSizeErrorMessage);

                return ValidationResult.Success;
            }

            var files = (value as IEnumerable<IFormFile>)!;
            foreach (var file in files)
            {
                if (!CheckFileExtension(file))
                    return new ValidationResult(ExtensionErrorMessage);

                if (!CheckFileSize(file))
                    return new ValidationResult(FileSizeErrorMessage);
            }

            return ValidationResult.Success;
        }

        private bool IsList(object obj)
        {
            var type = obj.GetType();

            return obj is IEnumerable<IFormFile>
                && type.IsGenericType
                && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        private bool CheckFileExtension(IFormFile file) => _extensions!.Contains(Path.GetExtension(file.FileName).ToLower());

        private bool CheckFileSize(IFormFile file) => _fileSize.HasValue ? file.Length <= _fileSize * 1024 * 1024 : true;

        private string ExtensionErrorMessage => $"僅支援上傳 {string.Join(", ", _extensions!)} 格式";
        private string FileSizeErrorMessage => "檔案大小超過限制";
    }
}
