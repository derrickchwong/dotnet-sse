using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;


namespace CRDOrderService.Controllers
{
    [ApiController]
    [Route("api")]
    public class DemoController : ControllerBase
    {
        public static readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _tcs = new ConcurrentDictionary<string, TaskCompletionSource<object>>();

        [HttpGet("start/{id}")]
        public async Task Start(string id)
        {
            var tcs = new TaskCompletionSource<object>();
            if (_tcs.TryAdd(id, tcs)){

                Response.Headers.Add("Content-Type", "application/json");
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");

                // Set up a timeout for the Task
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(100)); // Timeout after 100 seconds
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Timeout occurred
                    Response.StatusCode = 408; // Request Timeout
                    await Response.BodyWriter.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"data: Timeout\n\n"));
                    
                }
                else
                {
                    var result = await tcs.Task;
                    await Response.BodyWriter.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"data: {result}\n\n"));
                }

                await Response.BodyWriter.FlushAsync(); 
                
            }else{
                Response.StatusCode = 400;
            }
            
        }

        [HttpPost("push/{id}")]
        public IActionResult Push([FromBody] MessageData data, string id)
        {
            
            if (_tcs.TryGetValue(id, out var tcs))
            {
                string message = data.message;
                tcs.SetResult(message);
                tcs.TrySetCanceled();
                return Ok();
            }

            return BadRequest("invalid id " + id);
        }
    }

    public class MessageData
    {
        public string message { get; set; }
    }
}
