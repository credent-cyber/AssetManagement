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
            var data = DbContext.Asset.AsQueryable();
            return data;
        }
    }
}
