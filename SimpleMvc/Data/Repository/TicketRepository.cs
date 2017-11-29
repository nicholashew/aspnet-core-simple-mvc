using SimpleMvc.Enums;
using SimpleMvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleMvc.Data.Repository
{
    public class TicketRepository : RepositoryBase<Ticket>, ITicketRepository
    {
        public TicketRepository(ApplicationDbContext context)
            : base(context)
        { }

        public async Task<Ticket> GetByIdAsync(int id, bool includeProperties)
        {
            return await GetSingleAsync(
                t => t.Id == id,
                props => includeProperties ? props.Attachments : null,
                props => includeProperties ? props.Problem : null,
                props => includeProperties ? props.Building : null
            );
        }

        public async Task<IEnumerable<Ticket>> GetListByUserIdAsync(int userId, bool includeProperties)
        {
            IEnumerable<Ticket> tickets = await AllIncludingAsync(
                t => t.UserId == userId &&
                (
                    t.TicketStatus == TicketStatus.New ||
                    t.TicketStatus == TicketStatus.Acknowledge ||
                    t.TicketStatus == TicketStatus.Response ||
                    (t.TicketStatus == TicketStatus.Completed && t.TicketCompletedDate >= DateTime.Today.AddDays(-90))
                ),
                props => includeProperties ? props.Attachments : null,
                props => includeProperties ? props.Problem : null,
                props => includeProperties ? props.Building : null
            );

            return tickets.OrderBy(t =>
                t.TicketStatus == TicketStatus.New ? 0 :
                    t.TicketStatus == TicketStatus.Completed && t.TicketRating.GetValueOrDefault() <= 0 ? 1 :
                        t.TicketStatus == TicketStatus.Acknowledge || t.TicketStatus == TicketStatus.Response ? 2 :
                            t.TicketStatus == TicketStatus.Completed && t.TicketRating > 0 ? 3 : 4
            ).ThenByDescending(t => t.CreatedDate);
        }
    }
}