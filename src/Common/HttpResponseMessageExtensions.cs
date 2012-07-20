using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Http {

	internal static class HttpResponseMessageExtensions {

		internal static Task<HttpResponseMessage> ToTask(this HttpResponseMessage responseMessage) {
            
            return TaskHelpers.FromResult(responseMessage);
		}
	}
}
