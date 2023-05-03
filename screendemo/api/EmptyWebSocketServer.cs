using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;
using WebSocketSharp;

namespace screendemo.api
{
    class EmptyWebSocketServer : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {

        }
    }
}
