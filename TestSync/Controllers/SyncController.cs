using System;
using System.Text;
using System.Threading.Tasks;
using Dotmim.Sync;
using Dotmim.Sync.Enumerations;
using Dotmim.Sync.Web.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace TestSync.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private WebServerAgent webServerAgent;
        private readonly IWebHostEnvironment env;

        public SyncController(WebServerAgent webServerAgent, IWebHostEnvironment env)
        {
            this.webServerAgent = webServerAgent;
            this.env = env;
        }

        // [HttpPost]
        // public Task Post() 
        //     => webServerAgent.HandleRequestAsync(this.HttpContext);

        [HttpPost]
        public async Task Post()
        {
            var scopeName = this.HttpContext.GetScopeName();
            var endpoint = this.HttpContext.GetEndpoint();
            var version = this.HttpContext.GetVersion();
            var currentStep = this.HttpContext.GetCurrentStep();
            
            webServerAgent.OnHttpGettingRequest(req =>
                Console.WriteLine("Receiving Client Request:" + req.Context.SyncStage +
                                  ". " + req.HttpContext.Request.Host.Host + "."));
            
            webServerAgent.OnHttpSendingResponse(res =>
                Console.WriteLine("Sending Client Response:" + res.Context.SyncStage +
                                  ". " + res.HttpContext.Request.Host.Host));

            webServerAgent.OnHttpGettingChanges(args
                => Console.WriteLine("Getting Client Changes" + args));
            webServerAgent.OnHttpSendingChanges(args
                => Console.WriteLine("Sending Server Changes" + args));

            Console.WriteLine(scopeName, endpoint, version, currentStep);
            
            
            
            webServerAgent.RemoteOrchestrator.OnApplyChangesConflictOccured(e =>
            {
                var conflict = e.GetSyncConflictAsync();

                
                Console.WriteLine($"LocalRow : {conflict.Result.LocalRow}");
                Console.WriteLine($"RemoteRow : {conflict.Result.RemoteRow}");
                Console.WriteLine($"RemoteRow : {conflict.Result.RemoteRow.GetType()}");
                Console.WriteLine($"Id : {conflict.Id}");
                Console.WriteLine($"Result.Type : {conflict.Result.Type}");
                Console.WriteLine($"CreationOptions : {conflict.CreationOptions}");
                
                var choose = Console.ReadLine();

                if (choose == "1")
                    e.Resolution = ConflictResolution.ServerWins;
                else if (choose == "2")
                    e.Resolution = ConflictResolution.ClientWins;
                else
                {
                    e.Resolution = ConflictResolution.MergeRow;
                    e.FinalRow["Name"] = "SomeCity";
                }
                
            });
            
            await webServerAgent.HandleRequestAsync(this.HttpContext);
        }
        
        [HttpGet]
        public async Task Get()
        {
            if (env.IsDevelopment())
            {
                await this.HttpContext.WriteHelloAsync(webServerAgent);
            }
            else
            {
                var stringBuilder = new StringBuilder();

                stringBuilder.AppendLine("<!doctype html>");
                stringBuilder.AppendLine("<html>");
                stringBuilder.AppendLine("<title>Web Server properties</title>");
                stringBuilder.AppendLine("<body>");
                stringBuilder.AppendLine(" PRODUCTION. Write Whatever You Want Here ");
                stringBuilder.AppendLine("</body>");
                await this.HttpContext.Response.WriteAsync(stringBuilder.ToString());
            }
        }

    }
}