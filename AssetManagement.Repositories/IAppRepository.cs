using AssetManagement.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetManagement.Repositories
{
    public interface IAppRepository
    {
        Task<Details> GetDetailsById(int id);
        Task<IEnumerable<Details>> GetAllDetails();
    }
}
