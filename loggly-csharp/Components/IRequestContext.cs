using System.Collections.Generic;
namespace Loggly
{
   public interface IRequestContext
   {
      string Url { get; }
      IList<KeyValuePair<string, string>> HttpHeaders { get; }

   }
}