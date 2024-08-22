using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;


namespace CRDOrderService.Controllers
{
    [ApiController]
    [Route("api")]
    public class DemoController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _emitters = new ConcurrentDictionary<string, TaskCompletionSource<object>>();

        [HttpGet("start/{id}")]
        public async Task Start(string id)
        {
            var emitter = new TaskCompletionSource<object>();
            if (_emitters.TryAdd(id, emitter)){
                // _emitters[id] = emitter;

                Response.Headers.Add("Content-Type", "text/event-stream");
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");

                // Set up a timeout for the Task
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5)); // Timeout after 5 seconds
                var completedTask = await Task.WhenAny(emitter.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Timeout occurred
                    Response.StatusCode = 408; // Request Timeout
                    
                    // TODO send a cancel message to pubsub 

                    await Response.BodyWriter.WriteAsync(System.Text.Encoding.UTF8.GetBytes($"data: Timeout\n\n"));
                    
                }
                else
                {
                    // emitter.Task completed successfully
                    var result = await emitter.Task;
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
            
            if (_emitters.TryGetValue(id, out var emitter))
            {
                string message = data.message;

                emitter.SetResult(message);
                
                emitter.TrySetCanceled();
                
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
