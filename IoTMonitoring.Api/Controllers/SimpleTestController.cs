using IoTMonitoring.Api.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace IoTMonitoring.Api.Controllers
{
    [Route("api/simple-test")]
    [ApiController]
    public class SimpleTestController : ControllerBase
    {
        [HttpGet]
        public ActionResult<object> Get()
        {
            return Ok(new { message = "API가 정상 작동합니다!", timestamp = DateTimeHelper.Now });
        }
    }
}