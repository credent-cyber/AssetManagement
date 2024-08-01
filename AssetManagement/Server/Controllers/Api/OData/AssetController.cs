using AssetManagement.DataContext;
using AssetManagement.Dto;
using AssetManagement.Dto.Models;
using BlazorWasm_Starter.Server.Controllers.Api.OData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace AssetManagement.Server.Controllers.Api.OData
{
    [Authorize]
    public class AssetController : ODataController
    {
        public ILogger<AssetController> Logger { get; }
        public AppDbContext DbContext { get; }
        public AssetController(ILogger<AssetController> logger, AppDbContext dbContext)
        {
            Logger = logger;
            DbContext = dbContext;
        }

        [EnableQuery]

        public IQueryable<Asset> Get()
        {
            var data = from ast in DbContext.Asset.AsQueryable()
                       join alloc in DbContext.Allocation.AsQueryable() on ast.Id equals alloc.AssetId into allocGroup
                       from alloc in allocGroup.DefaultIfEmpty()
                       join emp in DbContext.Employee.AsQueryable() on alloc.EmployeeId equals emp.Id into empGroup
                       from emp in empGroup.DefaultIfEmpty()
                       select new Asset
                       {
                           Id = ast.Id,
                           CompanyId = ast.CompanyId,
                           CompanyCode = ast.CompanyCode,
                           AssetTypeId = ast.AssetTypeId,
                           AssetType = ast.AssetType,
                           SubAssetType = ast.SubAssetType,
                           Brand = ast.Brand,
                           Model = ast.Model,
                           SerialNumber = ast.SerialNumber,
                           Description = ast.Description,
                           MacAddress = ast.MacAddress,
                           AcquireDate = ast.AcquireDate,
                           VendorName = ast.VendorName,
                           DiscardDate = ast.DiscardDate,
                           Status = ast.Status,
                           IsEngazed = ast.IsEngazed,
                           _AssetStatus = ast._AssetStatus,
                           EmployeeName = alloc != null && emp != null ? $"{emp.EmployeeName} ({emp.EmailId})" : "Unassigned"
                       };

            return data.AsQueryable();
        }


    }
}
