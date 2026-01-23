using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizFlow.Application.Common.Interfaces
{
    public interface IMessageService
    {
        string GetMessage(string key);
        string GetMessage(string key, params object[] args);
        string GetCurrentLanguage();
        void SetLanguage(string languageCode);
    }
}
