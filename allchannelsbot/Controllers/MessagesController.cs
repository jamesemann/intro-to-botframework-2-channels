using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace allchannelsbot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            await Conversation.SendAsync(activity, () => new EchoDialog());

            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }
    }

    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(Dialog1);
        }

        public async Task Dialog1(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var inboundMessage = await argument;
            var outboundMessage = context.MakeMessage();

            if (inboundMessage.ChannelId == "sms")
            {
                // as my twilio account doesn't support MMS (images) then don't send a gif for sms channel
                outboundMessage.Text = "James needs to upgrade his Twilio to support MMS :-(";
            }
            else
            {
                // get a random gif from giphy.com and send it as a card
                var client = new HttpClient() { BaseAddress = new Uri("http://api.giphy.com") };
                var result = client.GetStringAsync("/v1/gifs/trending?api_key=dc6zaTOxFJmzC").Result;
                var data = ((dynamic)JObject.Parse(result)).data;
                var gif = data[(int)Math.Floor(new Random().NextDouble() * data.Count)];
                var gifUrl = gif.images.fixed_height.url.Value;
                var slug = gif.slug.Value;

                outboundMessage.Attachments = new List<Attachment>();
                outboundMessage.Attachments.Add(new Attachment()
                {
                    ContentUrl = gifUrl,
                    ContentType = "image/gif",
                    Name = slug + ".gif"
                });
            }

            await context.PostAsync(outboundMessage);
            context.Wait(Dialog1);
        }
    }
}