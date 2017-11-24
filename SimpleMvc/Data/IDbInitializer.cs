using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMvc.Data
{
    public interface IDbInitializer
    {
        Task InitializeAsync();
    }
}
