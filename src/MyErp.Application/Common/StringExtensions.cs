namespace MyErp.Application.Common;

/// <summary>
/// 統一處理使用者輸入字串的小工具：新增/更新資料前先修剪頭尾空白，避免資料庫存進一堆
/// 使用者不小心多打的空白字元（例如 " 可樂"），也讓查重比對（Sku/Barcode/Name 等）更準確、
/// 不會因為多一個空白就被誤判成不重複。
/// </summary>
public static class StringExtensions
{
    /// <summary>必填欄位：修剪頭尾空白。</summary>
    public static string TrimRequired(this string value) => value.Trim();

    /// <summary>選填欄位：修剪頭尾空白，剪完是空字串就回傳 null（存 null 比存一堆空白乾淨）。</summary>
    public static string? TrimOrNull(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
