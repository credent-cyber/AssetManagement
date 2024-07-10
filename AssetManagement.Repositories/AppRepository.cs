using AssetManagement.DataContext;
using AssetManagement.DataContext.Migrations;
using AssetManagement.Dto;
using AssetManagement.Dto.Dashboard;
using AssetManagement.Dto.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Linq.Expressions;
using System.Threading.Tasks.Dataflow;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AssetManagement.Repositories
{
    public class AppRepository : BaseRepository, IAppRepository
    {
        public AppDbContext AppDbCxt { get; set; }

        public AppRepository(ILogger<AppRepository> logger, AppDbContext appContext) : base(logger)
        {
            AppDbCxt = appContext;
        }

        #region Details
        public async Task<Details> GetDetailsById(int id)
        {
            Details result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = AppDbCxt.Details.FirstOrDefault(o => o.Id == id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return await Task.FromResult(result);
        }

        public async Task<IEnumerable<Details>> GetAllDetails()
        {
            IEnumerable<Details> result = null;

            result = AppDbCxt.Details.ToList();
            return result;
        }
        public async Task<Details> UpsertDetails(Details details)
        {
            Details result = null;

            if (details == null)
                throw new ArgumentNullException("Invalid Details data");

            if (details.Id > 0)
            {


                AppDbCxt.Details.Update(details);

            }
            else
            {
                AppDbCxt.Details.Add(details);
            }

            AppDbCxt.SaveChanges();

            return details;
        }
        #endregion

        #region Company
        public async Task<Company> GetCompanyById(int id)
        {
            Company result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = AppDbCxt.Company.FirstOrDefault(o => o.Id == id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return result;
        }

        public async Task<IEnumerable<Company>> GetAllCompany()
        {
            IEnumerable<Company> result;

            result = await AppDbCxt.Company.ToListAsync();

            return result;
        }
        public async Task<ApiResponse<Company>> UpsertCompanyAsync(Company data)
        {
            var result = new ApiResponse<Company>();

            try
            {

                if (data.Id > 0)
                {
                    AppDbCxt.Company.Update(data);
                }
                else
                {
                    var companies = await AppDbCxt.Company.ToListAsync();
                    if (companies.Any(o => o.PanNumber == data.PanNumber && o.CompanyCode != data.CompanyCode) && data.PanNumber != "")
                    {
                        result.IsSuccess = false;
                        result.Message = "Duplicate PAN Number found!";
                        return result;
                    }
                    if (companies.Any(o => o.GSTNNumber == data.GSTNNumber && o.CompanyCode != data.CompanyCode) && data.GSTNNumber != "")
                    {
                        result.IsSuccess = false;
                        result.Message = "Duplicate GST Number found!";
                        return result;
                    }
                    if (companies.Any(o => o.TANNumber == data.TANNumber && o.CompanyCode != data.CompanyCode) && data.TANNumber != "")
                    {
                        result.IsSuccess = false;
                        result.Message = "Duplicate TAN Number found!";
                        return result;
                    }
                    if (companies.Any(o => o.IncorporationNumber == data.IncorporationNumber && o.CompanyCode != data.CompanyCode) && data.IncorporationNumber != "")
                    {
                        result.IsSuccess = false;
                        result.Message = "Duplicate Incorporation Number found!";
                        return result;
                    }
                    // Check if company code already exists
                    var existingCompany = companies.FirstOrDefault(c => c.CompanyCode == data.CompanyCode);
                    if (existingCompany != null)
                    {
                        result.Message = $"Duplicate company code: {data.CompanyCode}";
                        return result;
                    }
                    //AppDbCxt.Entry(existingAsset).State = EntityState.Detached; //can be used to detach tracking
                    AppDbCxt.Company.Add(data);
                }

                AppDbCxt.SaveChanges();

                result.Result = data;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }


        public async Task<(bool, string)> DeleteCompanyById(Company data)
        {
            Company result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = await AppDbCxt.Company.FindAsync(data.Id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (result != null)
            {

                if (result.EmployeeEngazedCount > 0 && result.AssetEngazedCount > 0)
                {
                    return (false, "Company can't be delete, beacuse it has Assets and Employees!");
                }
                else if (result.AssetEngazedCount > 0)
                {
                    return (false, "Company can't be delete, beacuse it has Assets!");
                }
                else if (result.EmployeeEngazedCount > 0)
                {
                    return (false, "Company can't be delete, beacuse it has Employees!");
                }
                else
                {
                    AppDbCxt.Company.Remove(result);
                    await AppDbCxt.SaveChangesAsync();
                    return (true, "Company deleted successfully.");
                }

            }

            return (false, "Company not found.");
        }

        public async Task<List<SubOffice>> UpsertSubOffice(List<SubOffice> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                throw new ArgumentNullException(nameof(dtos), "Invalid SubOffice data");
            }
            var results = dtos;

            var existingQuestions = AppDbCxt.SubOffice.Include(tq => tq.Zoffice)
              .Where(tq => tq.CompanyId == dtos.First().CompanyId).ToList();
            foreach (var existingQuestion in existingQuestions)
            {
                if (!dtos.Any(dto => dto.Id == existingQuestion.Id))
                {
                    AppDbCxt.SubOffice.Remove(existingQuestion);
                }
            }
            foreach (var dto in dtos)
            {
                if (dto.Id > 0)
                {
                    var existingSubOffice = await AppDbCxt.SubOffice.Include(tq => tq.Zoffice)
                                            .FirstOrDefaultAsync(tq => tq.Id == dto.Id);
                    if (existingSubOffice == null)
                    {
                        throw new ArgumentException($"No SubOffice found with ID {dto.Id}", nameof(dtos));
                    }

                    existingSubOffice.CompanyId = dto.CompanyId;
                    existingSubOffice.Number = dto.Number;
                    existingSubOffice.subName = dto.subName;
                    existingSubOffice.subAddress = dto.subAddress;
                    existingSubOffice.subCont = dto.subCont;
                    existingSubOffice.subContPerson = dto.subContPerson;
                    existingSubOffice.SubOficeZone = dto.SubOficeZone;

                    foreach (var answer in existingSubOffice.Zoffice.ToList())
                    {
                        if (!dto.Zoffice.Any(a => a.Id == answer.Id))
                        {
                            AppDbCxt.Zoffice.Remove(answer);
                        }
                    }

                    foreach (var ZofficeDto in dto.Zoffice)
                    {
                        var existingZonalOffice = existingSubOffice.Zoffice
                                                .FirstOrDefault(a => a.Id == ZofficeDto.Id);
                        if (existingZonalOffice == null)
                        {
                            var newAnswer = new Zoffice
                            {
                                SubOfficeId = existingSubOffice.Id,
                                Number = ZofficeDto.Number,
                                zoName = ZofficeDto.zoName,
                                zoAddress = ZofficeDto.zoAddress,
                                zoCont = ZofficeDto.zoCont,
                                zoContPerson = ZofficeDto.zoContPerson
                            };
                            AppDbCxt.Zoffice.Add(newAnswer);
                        }
                        else
                        {
                            existingZonalOffice.Number = ZofficeDto.Number;
                            existingZonalOffice.zoName = ZofficeDto.zoName;
                            existingZonalOffice.zoAddress = ZofficeDto.zoAddress;
                            existingZonalOffice.zoCont = ZofficeDto.zoCont;
                            existingZonalOffice.zoContPerson = ZofficeDto.zoContPerson;
                        }
                    }
                }
                else
                {
                    var newSubOffice = new SubOffice
                    {
                        CompanyId = dto.CompanyId,
                        Number = dto.Number,
                        subName = dto.subName,
                        subAddress = dto.subAddress,
                        subCont = dto.subCont,
                        subContPerson = dto.subContPerson,
                        SubOficeZone = dto.SubOficeZone
                    };
                    AppDbCxt.SubOffice.Add(newSubOffice);
                    await AppDbCxt.SaveChangesAsync();

                    foreach (var ZofficeDto in dto.Zoffice)
                    {
                        var newZoffice = new Zoffice
                        {


                            SubOfficeId = newSubOffice.Id,
                            Number = ZofficeDto.Number,
                            zoName = ZofficeDto.zoName,
                            zoAddress = ZofficeDto.zoAddress,
                            zoCont = ZofficeDto.zoCont,
                            zoContPerson = ZofficeDto.zoContPerson
                        };
                        AppDbCxt.Zoffice.Add(newZoffice);
                    }
                }
                try
                {
                    await AppDbCxt.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            return results;
        }

        public async Task<IEnumerable<SubOffice>> GetSubOfficeById(int id)
        {
            IEnumerable<SubOffice> result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = AppDbCxt.SubOffice.Where(o => o.CompanyId == id).Include(o => o.Zoffice).OrderBy(o => o.Number);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return result;
        }
        #endregion

        #region ZoneArea
        public async Task<ZoneArea> GetZoneAreaById(int id)
        {
            ZoneArea result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = AppDbCxt.ZoneArea.FirstOrDefault(o => o.Id == id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return result;
        }

        public async Task<IEnumerable<ZoneArea>> GetAllZoneArea()
        {
            IEnumerable<ZoneArea> result = null;

            result = AppDbCxt.ZoneArea.ToList();
            return result;
        }
        public async Task<List<ZoneArea>> UpsertZoneArea(List<ZoneArea> data)
        {
            List<ZoneArea> result = null;

            if (data.Count() == 0)
                return result;

            foreach (ZoneArea area in data)
            {
                var existing = await AppDbCxt.ZoneArea.FirstOrDefaultAsync(c => c.ZoneName.ToUpper() == area.ZoneName.ToUpper());
                if (existing != null)
                {
                    continue;
                }
                AppDbCxt.ZoneArea.Add(area);
            }

            AppDbCxt.SaveChanges();

            return data;
        }
        #endregion

        #region Employee
        public async Task<Employee> GetEmployeeById(int id)
        {
            Employee result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = AppDbCxt.Employee.FirstOrDefault(o => o.Id == id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return result;
        }

        public async Task<IEnumerable<Employee>> GetAllEmployee()
        {
            try
            {
                var employees = await AppDbCxt.Employee.ToListAsync();
                return employees;
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Error occurred while retrieving employees.");
                return null;
            }
        }

        public async Task<ApiResponse<Employee>> UpsertEmployeeAsync(Employee data)
        {
            var result = new ApiResponse<Employee>();
            try
            {

                if (data.Id > 0)
                {
                    AppDbCxt.Employee.Update(data);
                }
                else
                {
                    // Check if company code already exists
                    var existingEmployee = await AppDbCxt.Employee.FirstOrDefaultAsync(c => c.EmailId == data.EmailId && c.EmployeeId == data.EmployeeId);
                    if (existingEmployee != null)
                    {
                        result.Message = $"Duplicate Employee EmailId: {data.EmailId}";
                        return result;
                    }
                    var timestamp = DateTime.Now;
                    var key = $"{data.EmployeeId}-{data.EmailId}-{timestamp}".ComputeMd5Hash().ToLower();
                    var returnUrl = $"{data.BaseUrl}/emp/{key}";

                    //AppDbCxt.Entry(existingAsset).State = EntityState.Detached; //can be used to detach tracking
                    data.ReturnUrl = returnUrl;
                    data.SecurityStamp = key;
                    AppDbCxt.Employee.Add(data);

                    Company company = await AppDbCxt.Company.Where(o => o.CompanyCode == data.CompanyCode).FirstAsync();
                    company.EmployeeEngazedCount += 1;
                    AppDbCxt.Company.Update(company);

                    var OnboardEmployee = await AppDbCxt.EmployeeOnboarding.FirstOrDefaultAsync(c => c.ExternalEmailId == data.ExternalEmailId);
                    if (OnboardEmployee != null)
                    {
                        AppDbCxt.EmployeeOnboarding.Remove(OnboardEmployee);
                    }
                }

                AppDbCxt.SaveChanges();

                result.Result = data;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public async Task<ApiResponse<EmployeeOnboardingDto>> UpsertEmployeeOnboarding(EmployeeOnboardingDto data)
        {
            var result = new ApiResponse<EmployeeOnboardingDto>();
            try
            {
                if (data.Id > 0)
                {
                    data.IsReplied = true;
                    data.ResponceDate = DateTime.Now;
                    AppDbCxt.EmployeeOnboarding.Update(data);
                    await AppDbCxt.SaveChangesAsync();
                }
                else
                {
                    var Check = AppDbCxt.EmployeeOnboarding.FirstOrDefault(o => o.ExternalEmailId == data.ExternalEmailId);
                    if (Check != null)
                    {
                        result.IsSuccess = false;
                        result.Message = "Email already exist!";
                        return result;
                    }
                    else
                    {
                        var timestamp = DateTime.Now;
                        var key = $"{data.Id}-{data.ExternalEmailId}-{timestamp}".ComputeMd5Hash().ToLower();
                        var returnUrl = $"{data.BaseUrl}/EmployeeOnBoarding/{key}";
                        data.ReturnUrl = returnUrl;
                        data.SecurityStamp = key;
                        data.Date = DateTime.Now;
                        AppDbCxt.EmployeeOnboarding.Add(data);
                        await AppDbCxt.SaveChangesAsync();
                        result.Result = data;
                    }
                }

            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                return result;
            }
            result.Result = data;
            result.IsSuccess = true;
            result.Message = "Successfully Added!";
            return result;
        }

        public async Task<ApiResponse<EmployeeOnboardingDto>> GetOnboardingDataByKey(string key)
        {
            var result = new ApiResponse<EmployeeOnboardingDto>();
            try
            {
                var data = AppDbCxt.EmployeeOnboarding.FirstOrDefault(o => o.SecurityStamp == key && o.IsReplied == false);
                if (data == null)
                {
                    result.Message = "You have already Updated your details";
                    return result;
                }
                result.Result = data;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }
            return result;
        }
        public async Task<ApiResponse<EmployeeOnboardingDto>> GetOnboardingDataByID(string key)
        {
            var result = new ApiResponse<EmployeeOnboardingDto>();
            try
            {
                var data = AppDbCxt.EmployeeOnboarding.FirstOrDefault(o => o.SecurityStamp == key);
                if (data == null)
                {
                    result.Message = "No Data Found in Database!";
                    return result;
                }
                result.Result = data;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }
            return result;
        }
        public async Task<ApiResponse<EmployeeFilesMapping>> UpsertEmployeeFilesAsync(EmployeeFilesMapping data)
        {
            var result = new ApiResponse<EmployeeFilesMapping>();

            var existingData = await AppDbCxt.EmployeeFilesMapping.FirstOrDefaultAsync(e => e.EmployeeId == data.EmployeeId);

            if (existingData != null)
            {
                if (!string.IsNullOrEmpty(data.AadhaarFile))
                {
                    existingData.AadhaarFile = data.AadhaarFile;
                }
                if (!string.IsNullOrEmpty(data.PanFile))
                {
                    existingData.PanFile = data.PanFile;
                }
                if (!string.IsNullOrEmpty(data.BankPassbookFile))
                {
                    existingData.BankPassbookFile = data.BankPassbookFile;
                }
                if (!string.IsNullOrEmpty(data.CertificateFile))
                {
                    existingData.CertificateFile = data.CertificateFile;
                }
                if (!string.IsNullOrEmpty(data.ProfilePhotoFile))
                {
                    existingData.ProfilePhotoFile = data.ProfilePhotoFile;
                }

                // Update the database
                AppDbCxt.EmployeeFilesMapping.Update(existingData);
            }
            else
            {
                // Add new data to the database
                AppDbCxt.EmployeeFilesMapping.Add(data);
            }

            await AppDbCxt.SaveChangesAsync();
            return result;
        }


        public async Task<EmployeeFilesMapping> GetEmployeeFilesById(int id)
        {
            EmployeeFilesMapping result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = AppDbCxt.EmployeeFilesMapping.FirstOrDefault(o => o.EmployeeId == id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return result;
        }

        public async Task<IEnumerable<EmployeeFilesMapping>> GetAllEmployeeFileMap()
        {
            IEnumerable<EmployeeFilesMapping> result = null;

            result = AppDbCxt.EmployeeFilesMapping.ToList();
            return result;
        }

        public async Task<ApiResponse<List<EmployeeImport>>> UpsertImportEmployeeAsync(List<EmployeeImport> data)
        {
            var result = new ApiResponse<List<EmployeeImport>>();
            if (data.Count == 0)
            {
                result.IsSuccess = false;
                result.Message = "No data found to import!";
                return result;
            }
            var companyCodes = data.Select(o => o.CompanyCode.Trim()).Distinct().ToList();
            var missingCodes = companyCodes.Except(await AppDbCxt.Company.Select(o => o.CompanyCode).ToListAsync(), StringComparer.OrdinalIgnoreCase);
            if (missingCodes.Any())
            {
                result.IsSuccess = false;
                result.Message = $"The following company codes were not found: {string.Join(", ", missingCodes)}";
                return result;
            }
            try
            {
                foreach (var e in data)
                {
                    var existingEmployee = await AppDbCxt.Employee.FirstOrDefaultAsync(x => x.EmailId == e.EmailId && x.EmployeeId == e.EmployeeId);
                    var Companies = await AppDbCxt.Company.ToListAsync();
                    if (existingEmployee == null)
                    {
                        var companyId = Companies.Where(o => o.CompanyCode == e.CompanyCode).Select(o => o.Id).First();
                        var _company = Companies.Where(o => o.CompanyCode == e.CompanyCode).First();
                        var timestamp = DateTime.Now;


                        var employee = new Employee()
                        {
                            CompanyId = companyId,
                            CompanyCode = e.CompanyCode.ToUpper(),
                            EmployeeId = e.EmployeeId,
                            EmployeeName = e.EmployeeName,
                            fatherName = e.fatherName,
                            Status = e.Status,
                            EmailId = e.EmailId,
                            MobileNumber = e.MobileNumber,
                            PermanentAddress = e.PermanentAddress,
                            PState = e.PState,
                            PCountry = e.PCountry,
                            PPin = e.PPin,
                            CurrentAddress = e.CurrentAddress,
                            CState = e.CState,
                            CCountry = e.CCountry,
                            CPin = e.CPin,
                            DateOfBirth = e.DateOfBirth,
                            AadhaarNumber = e.AadhaarNumber,
                            PANNumber = e.PANNumber,
                            UANNo = e.UANNo,
                            EmpBankName = e.EmpBankName,
                            EmpAccountName = e.EmpAccountName,
                            EmpBankAccNumber = e.EmpBankAccNumber,
                            EmpBankIfsc = e.EmpBankIfsc,
                            EmergencyContactNumber = e.EmergencyContactNumber,
                            DateOfJoin = e.DateOfJoin,
                            DateOfLeaving = e.DateOfLeaving,
                            Designation = e.Designation,
                            ReportingTo = e.ReportingTo


                        };
                        var key = $"{employee.EmployeeId}-{employee.EmailId}-{timestamp}".ComputeMd5Hash().ToLower();
                        var returnUrl = $"{e.BaseUrl}/emp/{key}";
                        employee.SecurityStamp = key;
                        employee.ReturnUrl = returnUrl;
                        AppDbCxt.Employee.Add(employee);

                        _company.EmployeeEngazedCount += 1;
                        AppDbCxt.Company.Update(_company);
                    }
                    else
                    {
                        //existingEmployee.CompanyCode = e.CompanyCode;
                        //existingEmployee.NewEmployeeId = e.NewEmployeeId;
                        //existingEmployee.EmployeeName = e.EmployeeName;
                        //existingEmployee.fatherName = e.fatherName;
                        //existingEmployee.Status = e.Status;
                        //existingEmployee.MobileNumber = e.MobileNumber;
                        //existingEmployee.PermanentAddress = e.PermanentAddress;
                        //existingEmployee.PState = e.PState;
                        //existingEmployee.PCountry = e.PCountry;
                        //existingEmployee.PPin = e.PPin;
                        //existingEmployee.CurrentAddress = e.CurrentAddress;
                        //existingEmployee.CState = e.CState;
                        //existingEmployee.CCountry = e.CCountry;
                        //existingEmployee.CPin = e.CPin;
                        //existingEmployee.DateOfBirth = e.DateOfBirth;
                        //existingEmployee.AadhaarNumber = e.AadhaarNumber;
                        //existingEmployee.PANNumber = e.PANNumber;
                        //existingEmployee.UANNo = e.UANNo;
                        //existingEmployee.EmpBankName = e.EmpBankName;
                        //existingEmployee.EmpAccountName = e.EmpAccountName;
                        //existingEmployee.EmpBankAccNumber = e.EmpBankAccNumber;
                        //existingEmployee.EmpBankIfsc = e.EmpBankIfsc;
                        //existingEmployee.EmergencyContactNumber = e.EmergencyContactNumber;
                        //existingEmployee.DateOfJoin = e.DateOfJoin;
                        //existingEmployee.DateOfLeaving = e.DateOfLeaving;
                        //existingEmployee.Designation = e.Designation;
                        //existingEmployee.ReportingTo = e.ReportingTo;
                        //AppDbCxt.Employee.Update(existingEmployee);
                    }
                }
                await AppDbCxt.SaveChangesAsync();

                result.Result = data;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }
        public async Task<(bool, string)> DeleteEmployeeById(int id)
        {
            Employee result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = await AppDbCxt.Employee.FindAsync(id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            if (result != null)
            {
                if (result.EngazeCount == 0)
                {

                    AppDbCxt.Employee.Remove(result);

                    Company company = await AppDbCxt.Company.Where(o => o.CompanyCode == result.CompanyCode).FirstAsync();
                    company.AssetEngazedCount -= 1;
                    AppDbCxt.Company.Update(company);
                    try
                    {
                        await AppDbCxt.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        return (false, ex.Message);
                    }
                    return (true, "Employee deleted successfully");
                }
                else
                {
                    return (false, "Employee Can't be delete because has some allocations!");
                }
            }

            return (false, "Employee not found.");
        }

        public async Task<(bool, string)> DeleteOnboardeeById(int id)
        {
            EmployeeOnboardingDto result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = await AppDbCxt.EmployeeOnboarding.FindAsync(id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            if (result != null)
            {
                AppDbCxt.EmployeeOnboarding.Remove(result);
                try
                {
                    await AppDbCxt.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
                return (true, "Data deleted successfully");

            }

            return (false, "Employee not found.");
        }


        public async Task<Employee> ShareFormWithEmployeeViaEmail(int id)
        {
            Employee result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = await AppDbCxt.Employee.FindAsync(id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (result != null)
            {
                return result;
            }

            return result;
        }

        public async Task<EmployeeOnboardingDto> ShareFormWithOnboardeeViaEmail(int id)
        {
            EmployeeOnboardingDto result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = await AppDbCxt.EmployeeOnboarding.FindAsync(id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (result != null)
            {
                result.IsReplied = false;
                AppDbCxt.EmployeeOnboarding.Update(result);
                AppDbCxt.SaveChanges();
                result.CompanyName = await AppDbCxt.Company.Where(o => o.CompanyCode == result.CompanyCode).Select(o => o.Name).FirstOrDefaultAsync();
                return result;
            }

            return result;
        }

        public async Task<ApiResponse<Employee>> EmployeeSelfGetDetails(string key)
        {
            var result = new ApiResponse<Employee>();
            try
            {
                var data = AppDbCxt.Employee.FirstOrDefault(o => o.SecurityStamp == key && o.IsReplied == false);
                if (data == null)
                {
                    result.Message = "You have already Updated your details";
                    return result;
                }
                result.Result = data;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }
            return result;
        }

        public async Task<ApiResponse<Employee>> EmployeeSeftUpdate(Employee data)
        {
            var result = new ApiResponse<Employee>();
            try
            {
                if (data.Id > 0)
                {
                    var store = await AppDbCxt.Employee.FindAsync(data.Id);
                    if (store != null)
                    {
                        AppDbCxt.Entry(store).State = EntityState.Detached;
                        data.CompanyCode = store.CompanyCode;
                        data.EmployeeId = store.EmployeeId;
                        data.Status = store.Status;
                        data.ReportingTo = store.ReportingTo;
                    }
                    AppDbCxt.Employee.Update(data);
                    AppDbCxt.SaveChanges();
                    result.Result = data;
                    result.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        #endregion

        #region Shrepoint portal access
        public async Task<ApiResponse<EmployeePortalSPFX>> GetEmployeeByEmail(string email)
        {
            var result = new ApiResponse<EmployeePortalSPFX>();
            var ed = AppDbCxt.Employee.FirstOrDefault(o => o.EmailId == email);
            if (ed != null)
            {
                var efm = AppDbCxt.EmployeeFilesMapping.FirstOrDefault(o => o.EmployeeId == ed.Id);
                EmployeePortalSPFX employeePortalSPFX = new EmployeePortalSPFX()
                {
                    Name = ed.EmployeeName,
                    ExternalEmailId = ed.ExternalEmailId,
                    Email = ed.EmailId,
                    CompanyCode = ed.CompanyCode,
                    fatherName = ed.fatherName,
                    MobileNumber = ed.MobileNumber,
                    AadhaarNumber = ed.AadhaarNumber,
                    CurrentAddress = ed.CurrentAddress,
                    PermanentAddress = ed.PermanentAddress,
                    PCountry = ed.PCountry,
                    PState = ed.PState,         
                    EmpAccountName = ed.EmpAccountName,
                    EmpBankAccNumber = ed.EmpBankAccNumber,
                    EmpBankIfsc = ed.EmpBankIfsc,
                    EmpBankName = ed.EmpBankName,
                    ProfilePhotoFile = efm == null ? "" : $"https://assetmanagementapplication.azurewebsites.net/EmployeesZone/{efm.ProfilePhotoFile}"

                };
                result.Result = employeePortalSPFX;
                result.IsSuccess = true;
                result.Message = "User found";
            }
            else
            {
                result.IsSuccess = true;
                result.Message = "User not found!";
            }

            return result;
        }

        public async Task<ApiResponse<EmployeePortalSPFX>> UpdateEmployeeByEmail(EmployeePortalSPFX employeePortalSPFX)
        {
            var result = new ApiResponse<EmployeePortalSPFX>();
            try
            {
                var ed = AppDbCxt.Employee.FirstOrDefault(o => o.EmailId == employeePortalSPFX.Email);
                if (ed != null)
                {
                  ed.EmpAccountName = employeePortalSPFX.EmpAccountName;
                  ed.EmpBankAccNumber = employeePortalSPFX.EmpBankAccNumber;
                  ed.EmpBankName = employeePortalSPFX.EmpBankName;
                  ed.EmpBankIfsc = employeePortalSPFX.EmpBankIfsc;

                  ed.CurrentAddress = employeePortalSPFX.CurrentAddress;
                  ed.PermanentAddress = employeePortalSPFX.PermanentAddress;
                  ed.MobileNumber = employeePortalSPFX.MobileNumber;

                   AppDbCxt.Employee.Update(ed);
                   AppDbCxt.SaveChanges();
                   result.IsSuccess = true;
                   result.Message = "Successfully Updated!";

                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = "User email not found.";
                }
            
            }
            catch(Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
            }
            return result;
        }
        #endregion

        #region EmployeeSkills
        public async Task<EmployeeSkills> GetEmployeeSkillsById(int id)
        {
            EmployeeSkills result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = AppDbCxt.EmployeeSkills.FirstOrDefault(o => o.Id == id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return result;
        }
        public async Task<IEnumerable<EmployeeSkills>> GetAllEmployeeSkills()
        {
            IEnumerable<EmployeeSkills> result = null;

            result = AppDbCxt.EmployeeSkills.ToList();
            return result;
        }
        public async Task<List<EmployeeSkills>> UpsertEmployeeSkills(List<EmployeeSkills> data)
        {
            List<EmployeeSkills> result = null;

            if (data.Count() == 0)
                return result;

            foreach (EmployeeSkills area in data)
            {
                var existing = await AppDbCxt.EmployeeSkills.FirstOrDefaultAsync(c => c.Skill.ToUpper() == area.Skill.ToUpper());
                if (existing != null)
                {
                    continue;
                }
                AppDbCxt.EmployeeSkills.Add(area);
            }

            AppDbCxt.SaveChanges();

            return data;
        }

        public async Task<bool> UpsertEmployeeSkillsIDsMap(Dictionary<int, List<int>> dict)
        {
            foreach (var kvp in dict)
            {
                var employeeId = kvp.Key;
                var SkillId = kvp.Value;

                var existingEmployeeSkillId = await AppDbCxt.EmployeeSkillMapping
                    .Where(tcm => tcm.EmployeeId == employeeId)
                    .Select(tcm => tcm.EmployeeSkillId)
                    .ToListAsync();

                var EmployeeSkillIdToAdd = SkillId.Except(existingEmployeeSkillId);
                var EmployeeSkillIdToRemove = existingEmployeeSkillId.Except(SkillId);
                var campaignIdsToKeep = SkillId.Intersect(existingEmployeeSkillId);

                if (EmployeeSkillIdToRemove.Any())
                {
                    var mappingsToRemove = await AppDbCxt.EmployeeSkillMapping
                        .Where(tcm => tcm.EmployeeId == employeeId && EmployeeSkillIdToRemove.Contains(tcm.EmployeeSkillId))
                        .ToListAsync();
                    AppDbCxt.RemoveRange(mappingsToRemove);
                }

                if (EmployeeSkillIdToAdd.Any())
                {
                    var mappingsToAdd = EmployeeSkillIdToAdd.Select(c => new EmployeeSkillMapping
                    {
                        EmployeeId = employeeId,
                        EmployeeSkillId = c
                    });
                    AppDbCxt.AddRange(mappingsToAdd);
                }

                await AppDbCxt.SaveChangesAsync();
            }

            return true;
        }


        public async Task<List<EmployeeSkillMapping>> GetEmployeeSkillsIDs(int id)
        {
            var compaignIds = AppDbCxt.EmployeeSkillMapping.Where(o => o.EmployeeId == id);
            return compaignIds.ToList();
        }
        #endregion

        #region Asset
        public async Task<Asset> GetAssetById(int id)
        {
            Asset result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = AppDbCxt.Asset.FirstOrDefault(o => o.Id == id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return result;
        }

        public async Task<IEnumerable<Asset>> GetAllAsset()
        {
            IEnumerable<Asset> result = null;

            result = AppDbCxt.Asset.ToList();
            return result;
        }
        public async Task<ApiResponse<Asset>> UpsertAssetAsync(Asset data)
        {
            var result = new ApiResponse<Asset>();

            try
            {
                if (data.Id > 0)
                {
                    AppDbCxt.Asset.Update(data);
                }
                else
                {
                    // Check if Asset already exists
                    var existingAsset = await AppDbCxt.Asset.FirstOrDefaultAsync(c => c.SerialNumber == data.SerialNumber);
                    if (existingAsset != null)
                    {
                        result.Message = $"Duplicate Asset SerialNumber : {data.SerialNumber}";
                        return result;
                    }
                    AppDbCxt.Asset.Add(data);

                    Company company = await AppDbCxt.Company.Where(o => o.CompanyCode == data.CompanyCode).FirstAsync();
                    company.AssetEngazedCount += 1;
                    AppDbCxt.Company.Update(company);
                }

                AppDbCxt.SaveChanges();

                result.Result = data;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public async Task<ApiResponse<List<AssetImport>>> UpsertImportAssetAsync(List<AssetImport> data)
        {
            var result = new ApiResponse<List<AssetImport>>();
            if (data.Count == 0)
            {
                result.IsSuccess = false;
                result.Message = "No data found to import!";
                return result;
            }
            var companyCodes = data.Select(o => o.CompanyCode.Trim()).Distinct().ToList();
            var missingCodes = companyCodes.Except(await AppDbCxt.Company.Select(o => o.CompanyCode).ToListAsync(), StringComparer.OrdinalIgnoreCase);
            if (missingCodes.Any())
            {
                result.IsSuccess = false;
                result.Message = $"The following company codes were not found: {string.Join(", ", missingCodes)}";
                return result;
            }
            var AssetType = data.Select(o => o.AssetType.Trim()).Distinct().ToList();
            var missingAssetType = AssetType.Except(await AppDbCxt.AssetType.Select(o => o.Name).ToListAsync(), StringComparer.OrdinalIgnoreCase);
            if (missingAssetType.Any())
            {
                result.IsSuccess = false;
                result.Message = $"The following AssetType were not found: {string.Join(", ", missingAssetType)}";
                return result;
            }

            try
            {
                var AllAssetType = await AppDbCxt.AssetType.ToListAsync();
                var Companies = await AppDbCxt.Company.ToListAsync();
                foreach (var e in data)
                {
                    var existingAsset = await AppDbCxt.Asset.FirstOrDefaultAsync(x => x.SerialNumber == e.SerialNumber);
                    if (existingAsset == null)
                    {
                        int AssetTypeID = AllAssetType.FirstOrDefault(i => i.Name.Equals(e.AssetType, StringComparison.OrdinalIgnoreCase))?.Id ?? 0;

                        var companyId = Companies.Where(o => o.CompanyCode == e.CompanyCode).Select(o => o.Id).First();
                        var _company = Companies.Where(o => o.CompanyCode == e.CompanyCode).First();
                        var asset = new Asset()
                        {
                            CompanyId = companyId,
                            CompanyCode = e.CompanyCode.ToUpper(),
                            AssetType = e.AssetType,
                            AssetTypeId = AssetTypeID,
                            SubAssetType = e.SubAssetType,
                            Brand = e.Brand,
                            Model = e.Model,
                            Description = e.Description,
                            SerialNumber = e.SerialNumber,
                            MacAddress = e.MacAddress,
                            AcquireDate = e.AcquireDate,
                            VendorName = e.VendorName,
                            Status = AllocationStatus.Free,

                        };
                        AppDbCxt.Asset.Add(asset);

                        _company.AssetEngazedCount += 1;
                        AppDbCxt.Company.Update(_company);

                    }
                    else
                    {

                        //existingAsset.CompanyCode = e.CompanyCode.ToUpper();
                        //existingAsset.AssetType = e.AssetType;
                        //existingAsset.SubAssetType = e.SubAssetType;
                        //existingAsset.Brand = e.Brand;
                        //existingAsset.Model = e.Model;
                        //existingAsset.Description = e.Description;
                        //existingAsset.MacAddress = e.MacAddress;
                        //existingAsset.AcquireDate = e.AcquireDate;
                        //existingAsset.VendorName = e.VendorName;
                        //existingAsset.Status = AllocationStatus.Free;

                        //AppDbCxt.Asset.Update(existingAsset);
                    }
                }
                await AppDbCxt.SaveChangesAsync();

                result.Result = data;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public async Task<(bool, string)> DeleteAssetById(Asset data)
        {
            Asset result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = await AppDbCxt.Asset.FindAsync(data.Id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (result != null)
            {
                if (result.Status == AllocationStatus.Free)
                {
                    AppDbCxt.Asset.Remove(result);

                    Company company = await AppDbCxt.Company.Where(o => o.CompanyCode == result.CompanyCode).FirstAsync();
                    company.AssetEngazedCount -= 1;
                    AppDbCxt.Company.Update(company);

                    await AppDbCxt.SaveChangesAsync();
                    return (true, "Asset deleted successfully.");
                }
                else
                {
                    return (false, "Asset Can't be delete because it's allocated!");
                }
            }

            return (false, "Asset not found.");
        }
        #endregion

        #region AssetType
        public async Task<AssetType> GetAssetTypeById(int id)
        {
            AssetType result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = AppDbCxt.AssetType.Include(o => o.SubAssets).FirstOrDefault(o => o.Id == id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return result;
        }

        public async Task<IEnumerable<AssetType>> GetAllAssetType()
        {
            IEnumerable<AssetType> result = null;

            result = AppDbCxt.AssetType.Include(o => o.SubAssets).ToList();
            return result;
        }
        public async Task<ApiResponse<AssetType>> UpsertAssetTypeAsync(AssetType data)
        {
            var result = new ApiResponse<AssetType>();

            try
            {
                if (data.Id > 0)
                {
                    var existing = await AppDbCxt.AssetType.Include(tq => tq.SubAssets)
                                            .FirstOrDefaultAsync(tq => tq.Name.ToUpper() == data.Name.ToUpper());
                    if (existing == null)
                    {
                        result.Message = $"No AssetType found with Name : {data.Name}";
                        return result;
                    }

                    existing.Name = data.Name.ToUpper();


                    foreach (var answer in existing.SubAssets.ToList())
                    {
                        if (!data.SubAssets.Any(a => a.Id == answer.Id))
                        {
                            AppDbCxt.SubAsset.Remove(answer);
                        }
                    }

                    foreach (var answerDto in data.SubAssets)
                    {
                        var existingAnswer = existing.SubAssets
                                                .FirstOrDefault(a => a.Name.ToUpper() == answerDto.Name.ToUpper());
                        if (existingAnswer == null)
                        {
                            var newAnswer = new SubAsset
                            {
                                AssetTypeId = existing.Id,
                                Number = answerDto.Number,
                                Name = answerDto.Name.ToUpper(),
                            };
                            AppDbCxt.SubAsset.Add(newAnswer);
                        }
                        else
                        {
                            existingAnswer.Number = answerDto.Number;
                            existingAnswer.Name = answerDto.Name.ToUpper();
                        }
                    }
                }
                else
                {
                    var existing = await AppDbCxt.AssetType.Include(tq => tq.SubAssets)
                                            .FirstOrDefaultAsync(tq => tq.Name.ToUpper() == data.Name.ToUpper());
                    if (existing != null)
                    {
                        result.Message = $"Duplicate AssetType found with Name : {data.Name}";
                        return result;
                    }
                    var newType = new AssetType
                    {
                        Name = data.Name.ToUpper(),
                    };
                    AppDbCxt.AssetType.Add(newType);
                    await AppDbCxt.SaveChangesAsync();
                    foreach (var Dto in data.SubAssets)
                    {
                        var newSubAsset = new SubAsset
                        {
                            AssetTypeId = newType.Id,
                            Number = Dto.Number,
                            Name = Dto.Name,
                        };
                        AppDbCxt.SubAsset.Add(newSubAsset);
                    }
                }

                try
                {
                    await AppDbCxt.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                result.Result = data;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }
        #endregion

        #region Allocation
        public async Task<Allocation> GetAllocationById(int id)
        {
            Allocation result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = AppDbCxt.Allocation.FirstOrDefault(o => o.Id == id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            return result;
        }

        public async Task<bool> UnAllocation(int id)
        {
            var allocation = await AppDbCxt.Allocation.FindAsync(id);
            var data = allocation;
            if (allocation == null)
            {
                return false;
            }
            AppDbCxt.Allocation.Remove(allocation);
            Asset asset = AppDbCxt.Asset.FirstOrDefault(o => o.Id == data.AssetId);
            if (asset != null)
            {
                asset.Status = AllocationStatus.Free;
                AppDbCxt.Asset.Update(asset);
            }

            AllocationLog allocationLog = AppDbCxt.AllocationLog.FirstOrDefault(o => o.AllocationId == data.Id);
            if (allocationLog != null)
            {
                allocationLog.ReturnDate = data.ReturnDate ?? DateTime.Now;
                allocationLog.AllocationStatus = CurrentAllocationStatus.Returned;
                AppDbCxt.AllocationLog.Update(allocationLog);
            }

            Employee employee = AppDbCxt.Employee.FirstOrDefault(o => o.Id == data.EmployeeId);
            if (employee != null)
            {
                employee.EngazeCount -= 1;
                AppDbCxt.Employee.Update(employee);
            }

            await AppDbCxt.SaveChangesAsync();

            return true;
        }

        //public async Task<bool> UnAllocation1(Allocation data)
        //{
        //    using (var context = new AppDbContext())
        //    {
        //        var allocation = await context.Allocation.FindAsync(data.Id);
        //        if (allocation == null)
        //        {
        //            return false;
        //        }

        //        context.Allocation.Remove(allocation);

        //        Asset asset = await context.Asset.FirstOrDefaultAsync(o => o.Id == data.AssetId);
        //        if (asset != null)
        //        {
        //            asset.Status = AllocationStatus.Free;
        //            context.Asset.Update(asset);
        //        }

        //        AllocationLog allocationLog = await context.AllocationLog.FirstOrDefaultAsync(o => o.AllocationId == data.Id);
        //        if (allocationLog != null)
        //        {
        //            allocationLog.ReturnDate = data.ReturnDate ?? DateTime.Now;
        //            context.AllocationLog.Update(allocationLog);
        //        }

        //        Employee employee = await context.Employee.FirstOrDefaultAsync(o => o.Id == data.EmployeeId);
        //        if (employee != null)
        //        {
        //            employee.EngazeCount -= 1;
        //            context.Employee.Update(employee);
        //        }

        //        await context.SaveChangesAsync();

        //        return true;
        //    }
        //}
        public async Task<IEnumerable<Allocation>> GetAllAllocation()
        {
            IEnumerable<Allocation> result = null;

            result = AppDbCxt.Allocation.ToList();
            return result;
        }

        public async Task<ApiResponse<Allocation>> UpsertAllocationAsync(Allocation data)
        {
            var result = new ApiResponse<Allocation>();

            try
            {
                if (data.Id > 0)
                {
                    var timestamp = DateTime.Now;
                    var key = $"{data.Id}-{data.EmployeeId}-{data.AssetId}-{timestamp}".ComputeMd5Hash().ToLower();
                    var returnUrl = $"{data.BaseUrl}/allocation/{key}";
                    data.ReturnUrl = returnUrl;
                    data.Key = key;
                    AppDbCxt.Allocation.Update(data);
                    Asset asset = AppDbCxt.Asset.Where(o => o.Id == data.AssetId).First();
                    asset.Status = AllocationStatus.Allocated;
                    AppDbCxt.Asset.Update(asset);

                    AllocationLog allocationLog = AppDbCxt.AllocationLog.Where(o => o.AllocationId == data.Id).FirstOrDefault();
                    if (allocationLog != null)
                    {
                        allocationLog.IssueDate = data.IssueDate;
                        allocationLog.AllocationType = data.AllocationType;
                        allocationLog.IssueTill = data.IssueTill;
                        allocationLog.ReturnDate = data.ReturnDate;

                        allocationLog.CompanyCode = data.CompanyCode;
                        allocationLog.CompanyName = data.Company?.Name;

                        allocationLog.AssetId = data.AssetId;
                        allocationLog.AssetType = data.AssetType;
                        allocationLog.AssetModel = data.AssetModel;
                        allocationLog.AssetDescription = data.Asset?.Description;
                        allocationLog.AssetAquireDate = data.Asset.AcquireDate;

                        allocationLog.EmployeeId = data.EmployeeId;
                        allocationLog.EmployeeCompanyCode = data.EmployeeCompanyCode;
                        allocationLog.EmployeeName = data.EmployeeName;
                        allocationLog.EmployeeEmail = data.EmployeeEmail;
                        allocationLog.EmployeeMobileNumber = data.Employee?.MobileNumber;
                        allocationLog.EmployeeDesignation = data.Employee?.Designation;
                    }
                    AppDbCxt.AllocationLog.Update(allocationLog);
                }
                else
                {
                    var timestamp = DateTime.Now;
                    var key = $"{data.Id}-{data.EmployeeId}-{data.AssetId}-{timestamp}".ComputeMd5Hash().ToLower();
                    var returnUrl = $"{data.BaseUrl}/allocation/{key}";
                    data.ReturnUrl = returnUrl;
                    data.Key = key;
                    AppDbCxt.Allocation.Add(data);

                    Asset asset = AppDbCxt.Asset.Where(o => o.Id == data.AssetId).First();
                    asset.Status = AllocationStatus.Allocated;
                    asset.IsEngazed = true;
                    AppDbCxt.Asset.Update(asset);

                    Employee employee = AppDbCxt.Employee.Where(o => o.Id == data.EmployeeId).First();
                    employee.EngazeCount += 1;
                    AppDbCxt.Employee.Update(employee);

                    AppDbCxt.SaveChanges();

                    // Create a new AllocationLog record to log the update
                    AllocationLog allocationLog = new AllocationLog();
                    allocationLog.AllocationId = data.Id;
                    allocationLog.IssueDate = data.IssueDate;
                    allocationLog.AllocationType = data.AllocationType;
                    allocationLog.IssueTill = data.IssueTill;
                    allocationLog.ReturnDate = data.ReturnDate;



                    allocationLog.CompanyCode = data.CompanyCode;
                    allocationLog.CompanyName = data.Company?.Name;

                    allocationLog.AssetId = data.AssetId;
                    allocationLog.AssetType = data.AssetType;
                    allocationLog.AssetBrand = data.Asset.Brand;
                    allocationLog.AssetModel = data.AssetModel;
                    allocationLog.AssetDescription = data.Asset?.Description;
                    allocationLog.AssetAquireDate = data.Asset.AcquireDate;

                    allocationLog.EmployeeId = data.EmployeeId;
                    allocationLog.EmployeeCompanyCode = data.EmployeeCompanyCode;
                    allocationLog.EmployeeName = data.EmployeeName;
                    allocationLog.EmployeeEmail = data.EmployeeEmail;
                    allocationLog.EmployeeMobileNumber = data.Employee?.MobileNumber;
                    allocationLog.EmployeeDesignation = data.Employee?.Designation;

                    AppDbCxt.AllocationLog.Add(allocationLog);
                }

                AppDbCxt.SaveChanges();

                result.Result = data;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public async Task<ApiResponse<Allocation>> EmployeeAllocationDetails(string key)
        {
            var result = new ApiResponse<Allocation>();
            try
            {
                var data = AppDbCxt.Allocation.FirstOrDefault(o => o.Key == key && o.IsApproved == false);
                if (data == null)
                {
                    result.Message = "You have already Updated";
                    result.IsSuccess = false;
                    result.Result = null;
                    return result;
                }
                result.Result = data;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }
            return result;
        }

        public async Task<ApiResponse<Allocation>> EmployeeAllocationResponce(Allocation data)
        {
            var result = new ApiResponse<Allocation>();
            try
            {
                if (data.Responce == Responce.Approved)
                    data.Comment = string.Empty;

                if (data.Id > 0)
                {
                    data.IsApproved = true;
                    AppDbCxt.Allocation.Update(data);
                    AppDbCxt.SaveChanges();
                    result.Result = data;
                    result.IsSuccess = true;
                }
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
            }

            return result;
        }

        public async Task<Allocation> ShareAllocationDetailsToEmployeeViaEmail(int id)
        {
            Allocation result = null;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            result = await AppDbCxt.Allocation.FindAsync(id);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
            result.IsApproved = false;
            result.Responce = Responce.Pending;
            AppDbCxt.Update(result);
            AppDbCxt.SaveChanges();
            if (result != null)
            {
                return result;
            }

            return result;
        }

        public async Task<string> ReadAllocationCommentById(int id)
        {
            string comment = "No Comment Avaible!";

            var result = await AppDbCxt.Allocation.FirstOrDefaultAsync(a => a.Id == id);

            if (result != null)
            {
                comment = result.Comment;
            }

            return comment;
        }

        #endregion

        #region Dashboard
        public async Task<DashboardStatics> GetLastStatics()
        {
            var outcome = new DashboardStatics();
            outcome.CompanyCodes = await AppDbCxt.Company.Select(c => c.CompanyCode).Distinct().ToListAsync();
            outcome.Companies = AppDbCxt.Company.Count();
            outcome.Employees = AppDbCxt.Employee.Count(o => o.Status != EmployeeStatus.Resigned);
            outcome.Assets = AppDbCxt.Asset.Count();
            outcome.Allocations = AppDbCxt.Allocation.Count();
            return outcome;
        }

        public async Task<MasterStatics> GetMasterStaticsEntires()
        {
            MasterStatics data = new MasterStatics();
            data.CompaniesStatics = new List<CompaniesStatics>();
            data.AssetsStatics = new List<AssetsStatics>();
            data.EmployeeStatics = new List<EmployeeStatics>();
            data.AllocationStatics = new List<AllocationStatics>();
            bool getAllocation = true;

            // Retrieve all companies upfront and store them in memory
            var companies = await AppDbCxt.Company.ToListAsync();
            var assetsData = await AppDbCxt.Asset.ToListAsync();
            var employeeData = await AppDbCxt.Employee.ToListAsync();
            var allocationsData = await AppDbCxt.Allocation.ToListAsync();

            var assetResult = from company in companies
                              join asset in AppDbCxt.Asset
                                  on company.Id equals asset.CompanyId into assetGroup
                              select new
                              {
                                  CompanyCode = company.CompanyCode,
                                  AssetCount = assetGroup.Count()
                              };

            var employeeResult = from company in companies
                                 join employee in AppDbCxt.Employee
                                     on company.Id equals employee.CompanyId into employeeGroup
                                 select new
                                 {
                                     CompanyCode = company.CompanyCode,
                                     EmployeeCount = employeeGroup.Count()
                                 };

            var allocationResult = from company in companies
                                   join allocation in AppDbCxt.Allocation
                                       on company.Id equals allocation.CompanyId into allocationGroup
                                   select new
                                   {
                                       CompanyCode = company.CompanyCode,
                                       AllocationCount = allocationGroup.Count()
                                   };

            var result = from asset in assetResult
                         join employee in employeeResult
                             on asset.CompanyCode equals employee.CompanyCode
                         join allocation in allocationResult
                             on asset.CompanyCode equals allocation.CompanyCode
                         select new
                         {
                             CompanyCode = asset.CompanyCode,
                             AssetCount = asset.AssetCount,
                             EmployeeCount = employee.EmployeeCount,
                             AllocationCount = allocation.AllocationCount
                         };

            foreach (var item in result)
            {
                // Find the corresponding Company entity in memory based on CompanyCode
                var company = companies.FirstOrDefault(c => c.CompanyCode == item.CompanyCode);

                CompaniesStatics companyStatics = new CompaniesStatics
                {
                    company = company,
                    CompanyCode = item.CompanyCode,
                    TotalEmployees = item.EmployeeCount,
                    TotalAssets = item.AssetCount,
                    TotalAllocations = item.AllocationCount
                };
                data.CompaniesStatics.Add(companyStatics);

                var assetsForCompany = assetsData.Where(a => a.CompanyId == company.Id).ToList();
                int totalAssets = item.AssetCount;
                int freeAssets = assetsForCompany.Count(o => o.Status == AllocationStatus.Free);
                int allocatedAssets = assetsForCompany.Count(o => o.Status == AllocationStatus.Allocated);
                int active = assetsForCompany.Count(o => o._AssetStatus == AssetStatus.Active);
                int discarded = assetsForCompany.Count(o => o._AssetStatus == AssetStatus.Discarded);

                AssetsStatics assetsStatics = new AssetsStatics
                {
                    asset = assetsForCompany,
                    CompanyCode = item.CompanyCode,
                    TotalAssets = totalAssets,
                    FreeAssets = freeAssets,
                    AllocatedAssets = allocatedAssets,
                    ActiveAssets = active,
                    DiscardedAssets = discarded
                };
                data.AssetsStatics.Add(assetsStatics);


                var employeeForCompany = employeeData.Where(a => a.CompanyId == company.Id).ToList();
                int totalEmployee = item.EmployeeCount;
                var employeeByDepartment = employeeForCompany
                    .GroupBy(e => e.Department)
                    .Select(g => new EmployeeByDepartment
                    {
                        Department = g.Key,
                        Count = g.Count()
                    })
                    .ToList();
                var employeeByStatus = employeeForCompany
                    .GroupBy(e => e.Status)
                    .Select(g => new EmployeeByStatus
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToList();

                EmployeeStatics employeeStatics = new EmployeeStatics
                {
                    employee = employeeForCompany,
                    CompanyCode = item.CompanyCode,
                    TotalEmployee = totalEmployee,
                    EmployeeByDepartment = employeeByDepartment,
                    EmployeeByStatus = employeeByStatus,
                };
                data.EmployeeStatics.Add(employeeStatics);


                if (getAllocation)
                {
                    var allocationsForCompany = allocationsData.Where(a => a.CompanyId == company.Id).ToList();
                    int totalAllocation = item.AllocationCount;
                    int temporaryAllocation = allocationsForCompany.Count(o => o.AllocationType == AllocationType.Temporary);
                    int permanentAllocation = allocationsForCompany.Count(o => o.AllocationType == AllocationType.Permanent);
                    AllocationStatics allocationStatics = new AllocationStatics
                    {
                        allocation = allocationsForCompany,
                        CompanyCode = item.CompanyCode,
                        TemporaryAlloction = allocationsData.Count(o => o.AllocationType == AllocationType.Temporary),
                        PermanentAlloction = allocationsData.Count(o => o.AllocationType == AllocationType.Permanent)
                    };
                    data.AllocationStatics.Add(allocationStatics);
                    //getAllocation = false;
                }
            }

            return data;
        }
        #endregion

        #region ReportGenerate
        public async Task<IEnumerable<AllocationLog>> GetAllAllocationLog()
        {
            List<AllocationLog> result = new List<AllocationLog>();

            result = await AppDbCxt.AllocationLog.ToListAsync();
            return result;
        }

        public async Task<IEnumerable<AllocationLog>> GetFilteredAllocationLogs(AllocationlogReportGenerate data)
        {
            var query = AppDbCxt.AllocationLog.AsQueryable();

            if (data.CompanyCode && data.CompanyCodeSelected != null && data.CompanyCodeSelected.Any())
            {
                query = query.Where(log => data.CompanyCodeSelected.Contains(log.CompanyCode));
            }

            if (data.AssetType && data.AssetTypeSelected != null && data.AssetTypeSelected.Any())
            {
                query = query.Where(log => data.AssetTypeSelected.Contains(log.AssetType));
            }

            if (data.Brand)
            {
                query = query.Include(log => log.AssetBrand);
            }

            if (data.Model)
            {
                query = query.Include(log => log.AssetModel);
            }

            try
            {
                var existingData = await query.ToListAsync();
                return existingData;
            }
            catch (Exception ex)
            {

                return null;
            }
        }
        #endregion


        #region Trasnfer
        public async Task<ApiResponse<Employee>> EmployeeTransfer(EmployeeTransferModel data)
        {
            int newEmpId = 0;
            var result = new ApiResponse<Employee>();
            try
            {
                if (data != null)
                {
                    var allocations = AppDbCxt.Allocation.Where(a => a.EmployeeId == data.Id).ToList();
                    if (allocations.Count > 0)
                    {
                        foreach (var allocation in allocations)
                        {
                            var response = await UnAllocation(allocation.Id);
                        }
                    }

                    var modEmployee = await AppDbCxt.Employee.FirstOrDefaultAsync(a => a.Id == data.Id);
                    if (modEmployee != null)
                    {
                        modEmployee.DateOfLeaving = data.TransferDate;
                        modEmployee.Status = EmployeeStatus.Resigned;
                        AppDbCxt.Update(modEmployee);
                        await AppDbCxt.SaveChangesAsync();

                        var timestamp = DateTime.Now;
                        var key = $"{data.NewEmployeeId}-{data.NewEmail}-{timestamp}".ComputeMd5Hash().ToLower();
                        var returnUrl = $"{data.BaseUrl}/emp/{key}";

                        var employee = new Employee()
                        {
                            CompanyId = data.TargetCompanyId,
                            CompanyCode = data.TargetCompanyCode.ToUpper(),
                            EmployeeId = data.NewEmployeeId,
                            EmployeeName = modEmployee.EmployeeName,
                            fatherName = modEmployee.fatherName,
                            Status = EmployeeStatus.Permanent,
                            EmailId = data.NewEmail,
                            MobileNumber = modEmployee.MobileNumber,
                            PermanentAddress = modEmployee.PermanentAddress,
                            PState = modEmployee.PState,
                            PCountry = modEmployee.PCountry,
                            PPin = modEmployee.PPin,
                            checkbox = modEmployee.checkbox,
                            CurrentAddress = modEmployee.CurrentAddress,
                            CState = modEmployee.CState,
                            CCountry = modEmployee.CCountry,
                            CPin = modEmployee.CPin,
                            DateOfBirth = modEmployee.DateOfBirth,
                            AadhaarNumber = modEmployee.AadhaarNumber,
                            PANNumber = modEmployee.PANNumber,
                            UANNo = modEmployee.UANNo,
                            EmpBankName = modEmployee.EmpBankName,
                            EmpAccountName = modEmployee.EmpAccountName,
                            EmpBankAccNumber = modEmployee.EmpBankAccNumber,
                            EmpBankIfsc = modEmployee.EmpBankIfsc,
                            EmergencyContactNumber = modEmployee.EmergencyContactNumber,
                            DateOfJoin = data.TransferDate.AddDays(1),
                            DateOfLeaving = DateTime.MinValue,
                            Department = modEmployee.Department,
                            EmployeeEducation = modEmployee.EmployeeEducation,
                            EmployeeCategory = modEmployee.EmployeeCategory,
                            EmployeeEducationDetails = modEmployee.EmployeeEducationDetails,
                            ReportingTo = modEmployee.ReportingTo,
                            SecurityStamp = key,
                            ReturnUrl = returnUrl,
                            Designation = modEmployee.Designation
                        };

                        AppDbCxt.Employee.Add(employee);
                        await AppDbCxt.SaveChangesAsync(); // Save to get the generated Id
                        newEmpId = employee.Id;

                        if (allocations.Count > 0)
                        {
                            foreach (var allocation in allocations)
                            {
                                var newAllocation = new Allocation()
                                {
                                    AllocationType = allocation.AllocationType,
                                    IssueDate = DateTime.Now.AddDays(1),
                                    IssueTill = allocation.IssueTill,
                                    AssetId = allocation.AssetId,
                                    AssetModel = allocation.AssetModel,
                                    AssetType = allocation.AssetType,
                                    CompanyCode = allocation.CompanyCode,
                                    CompanyId = allocation.CompanyId,
                                    IsVerified = allocation.IsVerified,
                                    Responce = allocation.Responce,
                                    ReturnDate = allocation.ReturnDate,
                                    EmployeeId = employee.Id,
                                    EmployeeCompanyCode = employee.CompanyCode,
                                    EmployeeName = employee.EmployeeName,
                                    EmployeeEmail = employee.EmailId,
                                    BaseUrl = data.BaseUrl
                                };

                                var response = await UpsertAllocationAsync(newAllocation);
                            }
                        }

                        result.Result = employee;
                        result.IsSuccess = true;
                        result.Message = "Employee Data Transferred Successfully";

                        var fileMapping = await AppDbCxt.EmployeeFilesMapping.FirstOrDefaultAsync(e => e.EmployeeId == data.Id);
                        if (fileMapping != null)
                        {
                            string Aadhaar = ReplaceFirstWordAfterHyphen(fileMapping.AadhaarFile, data.NewEmployeeId);
                            string Pan = ReplaceFirstWordAfterHyphen(fileMapping.PanFile, data.NewEmployeeId);
                            string BankPassBook = ReplaceFirstWordAfterHyphen(fileMapping.BankPassbookFile, data.NewEmployeeId);
                            string Cert = ReplaceFirstWordAfterHyphen(fileMapping.CertificateFile, data.NewEmployeeId);
                            string Profile = ReplaceFirstWordAfterHyphen(fileMapping.ProfilePhotoFile, data.NewEmployeeId);

                            // Copy the files on the file system
                            CopyFile(fileMapping.AadhaarFile, Aadhaar);
                            CopyFile(fileMapping.PanFile, Pan);
                            CopyFile(fileMapping.BankPassbookFile, BankPassBook);
                            CopyFile(fileMapping.CertificateFile, Cert);
                            CopyFile(fileMapping.ProfilePhotoFile, Profile);

                            var newFileMapping = new EmployeeFilesMapping
                            {
                                EmployeeId = newEmpId, // Use the newly generated Id
                                AadhaarFile = Aadhaar,
                                PanFile = Pan,
                                BankPassbookFile = BankPassBook,
                                CertificateFile = Cert,
                                ProfilePhotoFile = Profile
                            };

                            AppDbCxt.EmployeeFilesMapping.Add(newFileMapping);
                            await AppDbCxt.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                return result;
            }
            return result;
        }


        private void CopyFile(string oldFileName, string newFileName)
        {
            try
            {
                string path;
//#if DEBUG
                path = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName + "\\AssetManagement\\Client\\wwwroot\\EmployeesZone";
//#else
//        path = Path.Combine(env.ContentRootPath, "wwwroot", "EmployeesZone");
//#endif

                string oldFilePath = Path.Combine(path, oldFileName);
                string newFilePath = Path.Combine(path, newFileName);

                if (File.Exists(oldFilePath))
                {
                    File.Copy(oldFilePath, newFilePath);
                }
            }
            catch (Exception ex)
            {
                // Log or handle the error as needed
            }
        }

        private string ReplaceFirstWordAfterHyphen(string input, string replacement)
        {
            // Split the input string by hyphens
            string[] parts = input.Split('-');

            // Trim whitespace from each part
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim();
            }

            // Check if there are enough parts to replace the first word after the first hyphen
            if (parts.Length > 1)
            {
                // Replace the first word after the first hyphen with the replacement string
                parts[1] = replacement;
            }

            // Join the parts back together with hyphens and return the result
            return string.Join(" - ", parts);
        }



        public async Task<ApiResponse<Asset>> AssetTransfer(AssetTransferModel data)
        {
            var result = new ApiResponse<Asset>();

            try
            {
                if (data != null)
                {
                    var allocation = AppDbCxt.Allocation.Where(a => a.AssetId == data.AssetID).FirstOrDefault();
                    if (allocation != null)
                    {
                        var responce = await UnAllocation(allocation.Id);
                    }

                    var modAsset = await AppDbCxt.Asset.FirstOrDefaultAsync(a => a.Id == data.AssetID);
                    if (modAsset != null)
                    {
                        modAsset.CompanyId = data.TargetCompanyId;
                        modAsset.CompanyCode = data.TargetCompanyCode;
                        AppDbCxt.Update(modAsset);
                        await AppDbCxt.SaveChangesAsync();

                        if (allocation != null)
                        {
                            var NewAllocation = new Allocation();
                            NewAllocation.AllocationType = allocation.AllocationType;
                            NewAllocation.IssueDate = DateTime.Now.AddDays(1);
                            NewAllocation.IssueTill = allocation.IssueTill;
                            NewAllocation.AssetId = allocation.AssetId;
                            NewAllocation.AssetModel = allocation.AssetModel;
                            NewAllocation.AssetType = allocation.AssetType;
                            NewAllocation.CompanyCode = modAsset.CompanyCode;
                            NewAllocation.CompanyId = modAsset.CompanyId;

                            NewAllocation.IsVerified = allocation.IsVerified;
                            NewAllocation.Responce = allocation.Responce;
                            NewAllocation.ReturnDate = allocation.ReturnDate;

                            NewAllocation.EmployeeId = allocation.EmployeeId;
                            NewAllocation.EmployeeCompanyCode = allocation.EmployeeCompanyCode;
                            NewAllocation.EmployeeName = allocation.EmployeeName;
                            NewAllocation.EmployeeEmail = allocation.EmployeeEmail;
                            NewAllocation.BaseUrl = data.BaseUrl;

                            var responce = await UpsertAllocationAsync(NewAllocation);
                        }


                        result.Result = modAsset;
                        result.IsSuccess = true;
                        result.Message = "Asset Data Transfered Successfully";
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = ex.Message;
                return result;
            }
            return result;
        }
        #endregion

        #region Custom Report
        public async Task<ReportFilters> GetEmployeeReportFileters()
        {
            var result = new List<string>();
            ReportFilters reportFilters = new ReportFilters();

            var manager = AppDbCxt.Employee.Select(o => o.ReportingTo).ToHashSet().Where(o => !o.Equals("")).ToList();
            var location = AppDbCxt.Employee.Select(o => o.PState).ToHashSet().Where(o => !o.Equals("")).ToList();
            var designation = AppDbCxt.Employee.Select(o => o.Designation).ToHashSet().Where(o => !o.Equals("")).ToList();
            //var branch = AppDbCxt.Employee.Select(o => o.).ToHashSet().Where(o => !o.Equals("")).ToList();
            reportFilters.Managers = manager;
            reportFilters.Locations = location;
            reportFilters.Designations = designation;
            //reportFilters.Branches = brach;
            return reportFilters;
        }

        public async Task<List<Employee>> GetFilteredEmployeeReport(EmployeeFilterModel model)
        {
            var result = new List<Employee>();
            try
            {
                var employeeData = AppDbCxt.Employee.ToList();
                if (model.EmployeeStatus == "Resigned")
                    employeeData = employeeData.Where(c => c.Status == EmployeeStatus.Resigned).ToList();
                if (model.EmployeeStatus == "Active")
                    employeeData = employeeData.Where(c => c.Status == EmployeeStatus.Permanent || c.Status == EmployeeStatus.Probation || c.Status == EmployeeStatus.Temporary).ToList();
                if (model.Company != null)
                    employeeData = employeeData.Where(c => c.CompanyCode == model.Company).ToList();

                if (model.Designation != null)
                    employeeData = employeeData.Where(c => c.Designation == model.Designation).ToList();

                if (model.ManagerName != null)
                    employeeData = employeeData.Where(c => c.ReportingTo == model.ManagerName).ToList();

                if (model.Location != null)
                    employeeData = employeeData.Where(c => c.PState == model.Location).ToList();

                if (model.JoinDateStart != DateTime.MinValue && model.JoinDateEnd != DateTime.MinValue)
                {
                    employeeData = employeeData.Where(o => o.DateOfJoin >= model.JoinDateStart && o.DateOfJoin <= model.JoinDateEnd).ToList();
                }

                if (model.ResignDateStart != DateTime.MinValue && model.ResignDateEnd != DateTime.MinValue)
                {
                    employeeData = employeeData.Where(o => o.DateOfLeaving >= model.ResignDateStart && o.DateOfLeaving <= model.ResignDateEnd).ToList();
                }

                var employeesWithSkills = employeeData
                    .GroupJoin(
                        AppDbCxt.EmployeeSkillMapping,
                        employee => employee.Id,
                        mapping => mapping.EmployeeId,
                        (employee, mappings) => new { Employee = employee, Mappings = mappings }
                    )
                    .SelectMany(
                        x => x.Mappings.DefaultIfEmpty(),
                        (employee, mapping) => new { Employee = employee.Employee, Mapping = mapping }
                    )
                    .GroupJoin(
                        AppDbCxt.EmployeeSkills,
                        x => x.Mapping?.EmployeeSkillId,
                        skill => skill.Id,
                        (x, skills) => new { Employee = x.Employee, Skills = skills }
                    )
                    .SelectMany(
                        x => x.Skills.DefaultIfEmpty(),
                        (employee, skill) => new { Employee = employee.Employee, Skill = skill != null ? skill.Skill : string.Empty }
                    )
                    .GroupBy(
                        x => new
                        {
                            x.Employee.Id,
                            x.Employee.CompanyId,
                            x.Employee.CompanyCode,
                            x.Employee.EmployeeId,
                            x.Employee.EmployeeName,
                            x.Employee.Status,
                            x.Employee.EmailId,
                            x.Employee.ExternalEmailId,
                            x.Employee.MobileNumber,
                            x.Employee.PermanentAddress,
                            x.Employee.PCountry,
                            x.Employee.PState,
                            x.Employee.PPin,
                            x.Employee.AadhaarNumber,
                            x.Employee.PANNumber,
                            x.Employee.UANNo,
                            x.Employee.EmergencyContactNumber,
                            x.Employee.EmployeeEducation,
                            x.Employee.EmployeeEducationDetails,
                            x.Employee.EmployeeCategory,
                            x.Employee.Department,
                            x.Employee.DateOfBirth,
                            x.Employee.DateOfJoin,
                            x.Employee.DateOfLeaving,
                            x.Employee.Designation,
                            x.Employee.ReportingTo,
                            x.Employee.fatherName,
                            x.Employee.EmpBankName,
                            x.Employee.EmpBankAccNumber,
                            x.Employee.EmpBankIfsc,
                            x.Employee.OtherSkills,

                        },
                        (key, skills) => new
                        {
                            Employee = key,
                            Skills = string.Join(", ", skills.Select(x => x.Skill).Where(skill => !string.IsNullOrEmpty(skill)))
                        }
                    )
                    .ToList();

                result = employeesWithSkills.Select(anonymousType => new Employee
                {
                    Id = anonymousType.Employee.Id,
                    CompanyId = anonymousType.Employee.CompanyId,
                    CompanyCode = anonymousType.Employee.CompanyCode,
                    EmployeeId = anonymousType.Employee.EmployeeId,
                    EmployeeName = anonymousType.Employee.EmployeeName,
                    Status = anonymousType.Employee.Status,
                    EmailId = anonymousType.Employee.EmailId,
                    ExternalEmailId = anonymousType.Employee.ExternalEmailId,
                    MobileNumber = anonymousType.Employee.MobileNumber,
                    PermanentAddress = anonymousType.Employee.PermanentAddress,
                    PCountry = anonymousType.Employee.PCountry,
                    PState = anonymousType.Employee.PState,
                    PPin = anonymousType.Employee.PPin,
                    AadhaarNumber = anonymousType.Employee.AadhaarNumber,
                    PANNumber = anonymousType.Employee.PANNumber,
                    UANNo = anonymousType.Employee.UANNo,
                    EmergencyContactNumber = anonymousType.Employee.EmergencyContactNumber,
                    EmployeeEducation = anonymousType.Employee.EmployeeEducation,
                    EmployeeEducationDetails = anonymousType.Employee.EmployeeEducationDetails,
                    EmployeeCategory = anonymousType.Employee.EmployeeCategory,
                    Department = anonymousType.Employee.Department,
                    DateOfBirth = anonymousType.Employee.DateOfBirth,
                    DateOfJoin = anonymousType.Employee.DateOfJoin,
                    DateOfLeaving = anonymousType.Employee.DateOfLeaving,
                    Designation = anonymousType.Employee.Designation,
                    ReportingTo = anonymousType.Employee.ReportingTo,
                    fatherName = anonymousType.Employee.fatherName,
                    EmpBankName = anonymousType.Employee.EmpBankName,
                    EmpBankAccNumber = anonymousType.Employee.EmpBankAccNumber,
                    EmpBankIfsc = anonymousType.Employee.EmpBankIfsc,
                    OtherSkills = anonymousType.Employee.OtherSkills,
                    Skills = anonymousType.Skills,
                })
                .ToList();

                if (model.Skills != null && model.Skills.Any())
                {
                    //result = result
                    //    .Where(employee =>
                    //    {
                    //        // Split the Skills property by commas
                    //        var employeeSkills = employee.Skills.Split(',');

                    //        // Check if any of the specified skills are in the employee's skills
                    //        return model.Skills.Any(skill => employeeSkills.Contains(skill.Trim()));
                    //    })
                    //    .ToList();

                    if (model.Skills != null && model.Skills.Any())
                    {
                        result = result
                            .Where(employee => model.Skills.Any(skill => employee.Skills.Contains(skill)))
                            .ToList();
                    }

                }




            }
            catch (Exception ex)
            {

            }
            return result;

        }

        public async Task<AssetReportFilters> GetAssetReportFilters()
        {
            AssetReportFilters reportFilters = new AssetReportFilters();

            var assets = AppDbCxt.Asset;

            reportFilters.AssetType = await assets
                .Select(o => new assetType
                {
                    CompanyCode = o.CompanyCode,
                    AssetTypeName = o.AssetType
                })
                .Where(b => !string.IsNullOrWhiteSpace(b.AssetTypeName))
                .Distinct()
                .ToListAsync();

            reportFilters.Brand = await assets
                .Select(o => new brand
                {
                    BrandName = o.Brand,
                    AssetType = o.AssetType
                })
                .Where(b => !string.IsNullOrWhiteSpace(b.BrandName))
                .Distinct()
                .ToListAsync();

            reportFilters.Model = await assets
                .Select(o => new model
                {
                    Brand = o.Brand,
                    ModelName = o.Model
                })
                .Where(b => !string.IsNullOrWhiteSpace(b.ModelName))
                .Distinct()
                .ToListAsync();

            return reportFilters;
        }


        public async Task<List<Asset>> GetFilteredAssetReport(AssetFilterModel model)
        {
            var result = new List<Asset>();
            try
            {
                var assetData = AppDbCxt.Asset.ToList();
                if (model.Company != null)
                    assetData = assetData.Where(c => c.CompanyCode == model.Company).ToList();

                if (model.AssetType != null)
                    assetData = assetData.Where(c => c.AssetType == model.AssetType).ToList();

                if (model.Brand != null)
                    assetData = assetData.Where(c => c.Brand == model.Brand).ToList();

                if (model.Model != null)
                    assetData = assetData.Where(c => c.Model == model.Model).ToList();

                if (model.Status != null)
                    assetData = assetData.Where(c => c.Status == model.Status).ToList();

                if (model.AquireDateStart != DateTime.MinValue && model.AquireDateEnd != DateTime.MinValue)
                {
                    assetData = assetData.Where(o => o.AcquireDate >= model.AquireDateStart && o.AcquireDate <= model.AquireDateEnd).ToList();
                }

                if (model.DiscardDateStart != DateTime.MinValue && model.DiscardDateEnd != DateTime.MinValue)
                {
                    assetData = assetData.Where(o => o.DiscardDate >= model.DiscardDateStart && o.DiscardDate <= model.DiscardDateEnd).ToList();
                }
                result = assetData;
                
            }
            catch (Exception ex)
            {
                
            }
            return result;

        }
        #endregion
    }
}
