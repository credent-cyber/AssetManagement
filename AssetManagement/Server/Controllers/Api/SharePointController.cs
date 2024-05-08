using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AssetManagement.Server.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SharePointController : ControllerBase
    {
        private readonly SharePointService _sharePointService;

        public SharePointController(SharePointService sharePointService)
        {
            _sharePointService = sharePointService;
        }

        [HttpPost("updateListItem")]
        public async Task<IActionResult> UpdateListItem([FromBody] UpdateListItemRequest request)
        {
            try
            {
                await _sharePointService.UpdateListItem(itemId: 123, updatedValue: "New value");
               // await _sharePointService.UpdateListItem(request.ItemId, request.UpdatedValue);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating item: {ex.Message}");
            }
        }
    }

    public class UpdateListItemRequest
    {
        public int ItemId { get; set; }
        public string UpdatedValue { get; set; }
    }
}

