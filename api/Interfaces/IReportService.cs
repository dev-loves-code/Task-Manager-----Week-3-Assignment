using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Account;

namespace api.Interfaces
{
    public interface IReportService
    {
        Task QueueWeeklyReports();
        Task GeneratePdf(UserDtoWithID user);
    }
}