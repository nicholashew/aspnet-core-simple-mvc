using SimpleMvc.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimpleMvc.Data.Repository
{
    public interface ITicketRepository : IRepository<Ticket>
    {
        Task<Ticket> GetByIdAsync(int id, bool includeProperties);
        Task<IEnumerable<Ticket>> GetListByUserIdAsync(int userId, bool includeProperties);
    }
}