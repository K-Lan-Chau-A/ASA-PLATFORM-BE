using ASA_PLATFORM_REPO.Repository;
using ASA_PLATFORM_SERVICE.Interface;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASA_PLATFORM_SERVICE.Implenment
{
    public class LogActivityService : ILogActivityService
    {
        private readonly LogActivityRepo _logActivityRepo;
        private readonly IMapper _mapper;
        public LogActivityService(LogActivityRepo logActivityRepo, IMapper mapper)
        {
            _logActivityRepo = logActivityRepo;
            _mapper = mapper;
        }
    }
}
