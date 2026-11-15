using System.ComponentModel.DataAnnotations;

namespace CinemaBD.Web.Models;

public sealed class ChangePasswordViewModel
{
    public bool IsAdmin { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(12, ErrorMessage = "Mật khẩu mới phải có ít nhất 12 ký tự.")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "Mật khẩu mới phải gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
