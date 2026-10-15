using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoComply.Models;

namespace DoComply.Pages.Admin
{
    public class VerificationQueueModel : PageModel
    {
        private readonly DoComplyContext _context;

        public VerificationQueueModel(DoComplyContext context)
        {
            _context = context;
        }

        public List<VerificationQueueItem> Queue { get; set; } = new List<VerificationQueueItem>();

        public async Task OnGetAsync()
        {
            var dbQueue = await _context.VwVerificationQueues.ToListAsync();

            if (dbQueue.Any())
            {
                Queue = dbQueue.Select(q => new VerificationQueueItem
                {
                    Id = (int)q.DocumentSubmissionId,
                    SupplierName = q.SupplierName,
                    DocumentName = q.Document,
                    SubmittedTime = q.SubmittedAtUtc.ToLocalTime().ToString("MMM dd, hh:mm tt"),
                    Status = q.Status
                }).ToList();
            }
            else
            {
                Queue = SampleDataFixture.GetSampleVerificationQueue();
            }
        }
    }
}
