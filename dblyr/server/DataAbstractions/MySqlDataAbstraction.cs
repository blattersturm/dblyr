using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace CitizenFX.DataLayer
{
    internal class MySqlDataAbstraction : DbDataAbstraction
    {
        public override string AppendInsertIdQuery(string query)
        {
            return query + (!query.EndsWith(";") ? ";" : "") + " SELECT LAST_INSERT_ID();";
        }
    }
}