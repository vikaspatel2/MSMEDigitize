using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MSMEDigitize.Infrastructure.Services;

public class PayrollService : IPayrollService
{
    private readonly AppDbContext _db;
    public PayrollService(AppDbContext db) { _db = db; }

    public async Task<Result<object>> ProcessPayrollAsync(Guid tenantId, int month, int year, CancellationToken ct = default)
    {
        var employees = await _db.Set<Employee>()
            .Where(e => e.TenantId == tenantId && e.Status != MSMEDigitize.Core.Enums.EmployeeStatus.Terminated)
            .ToListAsync(ct);

        foreach (var emp in employees)
        {
            var exists = await _db.Set<Payslip>()
                .AnyAsync(p => p.EmployeeId == emp.Id && p.Month == month && p.Year == year, ct);
            if (exists) continue;

            var pf = emp.BasicSalary * 0.12m;
            var gross = emp.GrossSalary > 0 ? emp.GrossSalary : emp.BasicSalary + emp.HRA + emp.SpecialAllowance;
            // Professional Tax slab (standard India): 0 if salary < 15000, else 200/month
            var pt = emp.PTApplicable && gross >= 15000 ? 200m : 0m;
            var deductions = pf * 2 + pt;
            var net = gross - deductions;

            _db.Set<Payslip>().Add(new Payslip
            {
                TenantId = tenantId,
                EmployeeId = emp.Id,
                Month = month,
                Year = year,
                BasicSalary = emp.BasicSalary,
                HRA = emp.HRA,
                SpecialAllowance = emp.SpecialAllowance,
                ConveyanceAllowance = emp.ConveyanceAllowance,
                MedicalAllowance = emp.MedicalAllowance,
                GrossSalary = gross,
                PFEmployee = pf,
                PFEmployer = pf,
                TotalDeductions = deductions,
                NetSalary = net,
                WorkingDays = DateTime.DaysInMonth(year, month),
                PaidDays = DateTime.DaysInMonth(year, month),
                Status = MSMEDigitize.Core.Enums.PayrollStatus.Draft
            });
        }

        await _db.SaveChangesAsync(ct);
        var payslips = await _db.Set<Payslip>()
            .Include(p => p.Employee)
            .Where(p => p.TenantId == tenantId && p.Month == month && p.Year == year)
            .ToListAsync(ct);

        return Result<object>.Success(payslips);
    }

    public async Task<object?> GetPayrollSummaryAsync(Guid tenantId, int month, int year, CancellationToken ct = default)
    {
        var payslips = await _db.Set<Payslip>()
            .Where(p => p.TenantId == tenantId && p.Month == month && p.Year == year)
            .ToListAsync(ct);

        return new
        {
            TotalEmployees = payslips.Count,
            TotalGross = payslips.Sum(p => p.GrossSalary),
            TotalDeductions = payslips.Sum(p => p.TotalDeductions),
            TotalNetPay = payslips.Sum(p => p.NetSalary)
        };
    }

    public async Task<Result<byte[]>> GeneratePayslipAsync(Guid employeeId, int month, int year, CancellationToken ct = default)
    {
        // Stub - return empty PDF bytes
        return Result<byte[]>.Success(Array.Empty<byte>());
    }

    public async Task<Result<bool>> MarkAttendanceAsync(Guid tenantId, Guid employeeId, DateTime date, AttendanceStatus status, CancellationToken ct = default)
    {
        var existing = await _db.Set<Attendance>()
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == date.Date, ct);

        if (existing != null)
            existing.Status = status;
        else
            _db.Set<Attendance>().Add(new Attendance
            {
                TenantId = tenantId,
                EmployeeId = employeeId,
                Date = date.Date,
                Status = status
            });

        await _db.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
