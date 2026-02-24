using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MSMEDigitize.Core.Entities;
using MSMEDigitize.Core.Entities.AI;
using MSMEDigitize.Core.Entities.Banking;
using MSMEDigitize.Core.Entities.GST;
using MSMEDigitize.Core.Entities.Inventory;
using MSMEDigitize.Core.Entities.Invoicing;
using MSMEDigitize.Core.Entities.Payroll;
using MSMEDigitize.Core.Entities.Subscriptions;
using MSMEDigitize.Core.Entities.Tenants;
using MSMEDigitize.Core.Enums;
using MSMEDigitize.Core.Interfaces;
using MSMEDigitize.Core.Interfaces;

namespace MSMEDigitize.Web.Controllers;

[Authorize]
public class EmployeeController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IPayrollService _payrollService;
    private readonly ICurrentUserService _currentUser;

    public EmployeeController(IUnitOfWork uow, IPayrollService payroll, ICurrentUserService cu)
    { _uow = uow; _payrollService = payroll; _currentUser = cu; }
    private Guid TenantId => _currentUser.TenantId!.Value;

    public async Task<IActionResult> Index()
    {
        var employees = await _uow.Employees.Query()
            .Include(e => e.Department)
            .Include(e => e.Designation)
            .Where(e => e.TenantId == TenantId && e.IsActive)
            .OrderBy(e => e.FullName).ToListAsync();
        return View(employees);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = await _uow.Departments.FindAsync(d => d.TenantId == TenantId);
        ViewBag.Designations = await _uow.Designations.FindAsync(d => d.TenantId == TenantId);
        return View(new Employee());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee model)
    {
        if (!ModelState.IsValid) return View(model);
        model.TenantId = TenantId;
        var count = await _uow.Employees.CountAsync(e => e.TenantId == TenantId);
        model.EmployeeCode = $"EMP{(count + 1):D4}";
        await _uow.Employees.AddAsync(model);
        await _uow.SaveChangesAsync();
        TempData["Success"] = "Employee added!";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> ProcessPayroll(int month, int year)
    {
        if (month == 0) { month = DateTime.Now.Month; year = DateTime.Now.Year; }
        var payrolls = await _payrollService.ProcessPayrollAsync(TenantId, month, year);
        var summary = await _payrollService.GetPayrollSummaryAsync(TenantId, month, year);
        ViewBag.Month = month; ViewBag.Year = year;
        return View((payrolls, summary));
    }

    public async Task<IActionResult> Attendance(int month, int year)
    {
        if (month == 0) { month = DateTime.Now.Month; year = DateTime.Now.Year; }
        var employees = await _uow.Employees.FindAsync(e => e.TenantId == TenantId && e.IsActive);
        var attendances = await _uow.Attendances.FindAsync(
            a => a.TenantId == TenantId && a.Date.Month == month && a.Date.Year == year);
        ViewBag.Month = month; ViewBag.Year = year;
        return View((employees, attendances.ToList()));
    }

    [HttpPost]
    public async Task<IActionResult> MarkAttendance(Guid employeeId, DateTime date, AttendanceStatus status)
    {
        var existing = await _uow.Attendances.FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == date.Date);
        if (existing != null) { existing.Status = status; await _uow.Attendances.UpdateAsync(existing); }
        else await _uow.Attendances.AddAsync(new Attendance { TenantId = TenantId, EmployeeId = employeeId, Date = date.Date, Status = status });
        await _uow.SaveChangesAsync();
        return Ok();
    }
}