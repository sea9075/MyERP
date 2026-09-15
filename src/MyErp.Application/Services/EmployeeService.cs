using MyErp.Application.Abstractions;
using MyErp.Application.Common;
using MyErp.Application.DTOs;
using MyErp.Domain.Entities;
using MyErp.Domain.Enums;

namespace MyErp.Application.Services;

public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetAllAsync(bool includeDeleted, CancellationToken ct = default);
    Task<EmployeeDto> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>同時建立登入帳號（User）與人資資料（Employee），見 Employee 類別的說明。</summary>
    Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, string currentUsername, CancellationToken ct = default);

    Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeRequest request, string currentUsername, CancellationToken ct = default);

    /// <summary>軟刪除＝離職：Employee.IsDeleted=true。刻意不連動刪除 User——離職後帳號還在，只是通常不會再登入。</summary>
    Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default);
}

public class EmployeeService(
    IEmployeeRepository employeeRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork)
    : IEmployeeService
{
    public async Task<List<EmployeeDto>> GetAllAsync(bool includeDeleted, CancellationToken ct = default)
    {
        var employees = await employeeRepository.GetAllAsync(includeDeleted, ct);
        return employees.Select(ToDto).ToList();
    }

    public async Task<EmployeeDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var employee = await employeeRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到員工 (Id={id})。");
        return ToDto(employee);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, string currentUsername, CancellationToken ct = default)
    {
        request.Username = request.Username.TrimRequired();
        request.DisplayName = request.DisplayName.TrimRequired();
        request.JobTitle = request.JobTitle.TrimOrNull();
        request.Phone = request.Phone.TrimOrNull();
        request.Address = request.Address.TrimOrNull();
        request.EmergencyContactName = request.EmergencyContactName.TrimOrNull();
        request.EmergencyContactPhone = request.EmergencyContactPhone.TrimOrNull();

        var department = ParseDepartment(request.Department);

        if (await userRepository.UsernameExistsAsync(request.Username, ct))
        {
            throw new BusinessRuleException($"帳號 '{request.Username}' 已經存在。");
        }

        Employee employee = null!;

        // 一次動作要寫兩張表（Users、Employees），任何一步失敗都要整個回滾，避免只建出帳號、
        // 沒有對應的人資資料（或反過來）這種一半一半的狀態。
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var user = new User
            {
                Username = request.Username,
                PasswordHash = PasswordHasher.Hash(request.Password),
                DisplayName = request.DisplayName,
                Department = department,
            };
            user.InitializeAudit(currentUsername);
            userRepository.Add(user);
            await unitOfWork.SaveChangesAsync(ct);

            employee = new Employee
            {
                UserId = user.Id,
                User = user,
                MonthlySalary = request.MonthlySalary,
                HireDate = request.HireDate.Date,
                JobTitle = request.JobTitle,
                Phone = request.Phone,
                Address = request.Address,
                EmergencyContactName = request.EmergencyContactName,
                EmergencyContactPhone = request.EmergencyContactPhone,
            };
            employee.InitializeAudit(currentUsername);
            employeeRepository.Add(employee);
            await unitOfWork.SaveChangesAsync(ct);
        }, ct);

        return ToDto(employee);
    }

    public async Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeRequest request, string currentUsername, CancellationToken ct = default)
    {
        var employee = await employeeRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到員工 (Id={id})。");

        if (employee.User is null)
        {
            // 理論上不會發生（UserId 是必填外鍵），寫出來是為了讓 nullable 警告消失、也方便未來除錯。
            throw new BusinessRuleException($"員工 (Id={id}) 缺少對應的登入帳號資料，請聯絡系統管理員。");
        }

        request.DisplayName = request.DisplayName.TrimRequired();
        request.JobTitle = request.JobTitle.TrimOrNull();
        request.Phone = request.Phone.TrimOrNull();
        request.Address = request.Address.TrimOrNull();
        request.EmergencyContactName = request.EmergencyContactName.TrimOrNull();
        request.EmergencyContactPhone = request.EmergencyContactPhone.TrimOrNull();

        var department = ParseDepartment(request.Department);

        employee.User.DisplayName = request.DisplayName;
        employee.User.Department = department;
        employee.User.TouchUpdated(currentUsername);

        employee.MonthlySalary = request.MonthlySalary;
        employee.HireDate = request.HireDate.Date;
        employee.JobTitle = request.JobTitle;
        employee.Phone = request.Phone;
        employee.Address = request.Address;
        employee.EmergencyContactName = request.EmergencyContactName;
        employee.EmergencyContactPhone = request.EmergencyContactPhone;
        employee.IsDeleted = request.IsDeleted;
        employee.TouchUpdated(currentUsername);

        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(employee);
    }

    public async Task DeleteAsync(int id, string currentUsername, CancellationToken ct = default)
    {
        var employee = await employeeRepository.GetByIdAsync(id, ct)
            ?? throw new BusinessRuleException($"找不到員工 (Id={id})。");

        employee.SoftDelete(currentUsername);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static Department ParseDepartment(string value)
    {
        if (!Enum.TryParse<Department>(value, ignoreCase: true, out var department))
        {
            throw new BusinessRuleException($"無效的部門 '{value}'，必須是 Product / HR / Manager / Admin 其中之一。");
        }

        return department;
    }

    private static EmployeeDto ToDto(Employee employee) => new()
    {
        Id = employee.Id,
        UserId = employee.UserId,
        Username = employee.User?.Username ?? string.Empty,
        DisplayName = employee.User?.DisplayName ?? string.Empty,
        Department = employee.User?.Department.ToString() ?? string.Empty,
        MonthlySalary = employee.MonthlySalary,
        HireDate = employee.HireDate,
        JobTitle = employee.JobTitle,
        Phone = employee.Phone,
        Address = employee.Address,
        EmergencyContactName = employee.EmergencyContactName,
        EmergencyContactPhone = employee.EmergencyContactPhone,
        CreatedAt = employee.CreatedAt,
        UpdatedAt = employee.UpdatedAt,
        CreatedBy = employee.CreatedBy,
        UpdatedBy = employee.UpdatedBy,
        IsDeleted = employee.IsDeleted,
    };
}
