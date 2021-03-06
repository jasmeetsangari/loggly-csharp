using System;
using System.Collections.Generic;
using System.Threading;
using Loggly.Responses;
using Newtonsoft.Json;
using System.Linq;

namespace Loggly
{
   public class Logger : ILogger, IRequestContext
   {
      private string _url = "logs.loggly.com/";
      private readonly string _inputKey;

      public const string TAG_HEADER = "X-LOGGLY-TAG";

      private IList<KeyValuePair<string, string>> _httpHeaders;


      public Logger(string inputKey, string alternativeUrl = null)
      {
          if (!string.IsNullOrEmpty(alternativeUrl))
              _url = alternativeUrl;

         _inputKey = inputKey;
      }

      public LogResponse LogSync(string message)
      {
         return LogSync(message, false);
      }

      public void Log(string message)
      {
         Log(message, false);
      }

      public void Log(string message, Action<LogResponse> callback)
      {
         Log(message, false, callback);
      }

      public string Url
      {
         get { return _url; }
      }

      public IList<KeyValuePair<string, string>> HttpHeaders
      {
          get { return _httpHeaders; }
      }

      public LogResponse LogSync(string message, bool json)
      {
         var synchronizer = new AutoResetEvent(false);

         LogResponse response = null;
         Log(message, json, r =>
         {
            response = r;
            synchronizer.Set();
         });

         synchronizer.WaitOne();
         return response;
      }

      public void Log(string message, string category)
      {
         Log(message, category, null);
      }

      public void Log(string message, string category, IDictionary<string, object> data)
      {
          if (data != null)
          {
              var tagKey = data.Keys.FirstOrDefault(ky => ky.ToLower() == "tags");
              if (tagKey != null)
              {
                  var tag = data[tagKey];
                  if (tag != null && !string.IsNullOrEmpty(tag.ToString()))
                  {
                      if (_httpHeaders == null)
                          _httpHeaders = new List<KeyValuePair<string, string>>();
                      _httpHeaders.Add(new KeyValuePair<string, string>(TAG_HEADER, tag.ToString()));
                  }
              }
          }

         var logEntry = new Dictionary<string, object>(data ?? new Dictionary<string, object>())
                        {
                           {"message", message}, {"category", category}
                        };
         var jsonLogEntry = JsonConvert.SerializeObject(logEntry);
         Log(jsonLogEntry, true);
      }

      public void LogInfo(string message)
      {
         LogInfo(message, null);
      }

      public void LogInfo(string message, IDictionary<string, object> data)
      {
         Log(message, "info", data);
      }

      public void LogVerbose(string message)
      {
         LogVerbose(message, null);
      }

      public void LogVerbose(string message, IDictionary<string, object> data)
      {
         Log(message, "verbose", data);
      }

      public void LogWarning(string message)
      {
         LogWarning(message, null);
      }

      public void LogWarning(string message, IDictionary<string, object> data)
      {
         Log(message, "warning", data);
      }

      public void LogError(string message, Exception ex)
      {
         LogError(message, ex, null);
      }

      public void LogError(string message, Exception ex, IDictionary<string, object> data)
      {
         var exceptionData = new Dictionary<string, object>(data ?? new Dictionary<string, object>())
                             {
                                {"exception", ex.ToString()}
                             };
         Log(message, "error", exceptionData);
      }

      public void Log(string message, bool json)
      {
         Log(message, json, null);
      }

      public void Log(string message, bool json, Action<LogResponse> callback)
      {
         var communicator = new Communicator(this);
         var callbackWrapper = callback == null ? (Action<Response>) null : r =>
         {
            if (r.Success)
            {
               var res = JsonConvert.DeserializeObject<LogResponse>(r.Raw);
               res.Success = true;
               callback(res);
            }
            else
            {
               var res = new LogResponse{ Success = false };
               callback(res);
            }
         };
         communicator.SendPayload(Communicator.POST, string.Concat("inputs/", _inputKey), message, json, callbackWrapper);
      }
   }
}