using System.Data.Common;
using System.Threading.Tasks;

namespace CitizenFX.DataLayer
{
    public abstract class DbDataAbstraction
    {
        public abstract string AppendInsertIdQuery(string query);
    }
}