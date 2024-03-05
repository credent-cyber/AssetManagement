using AssetManagement.DataContext;
using AssetManagement.Dto;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlazorWasm_Starter.Server.Controllers.Api.OData
{
    [Authorize]
    public class DetailsController : ODataController
    {
        public ILogger<DetailsController> Logger { get; }
        public AppDbContext DbContext { get; }
        public DetailsController(ILogger<DetailsController> logger, AppDbContext dbContext)
        {
            Logger = logger;  
            DbContext = dbContext;
        }

        [EnableQuery]

        public IQueryable<Details> Get()
        {
            var data = DbContext.Details.AsQueryable();
            return data;
        }
    }
}
