using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace CitizenFX.DataLayer
{
    internal class PgsqlDataAbstraction : DbDataAbstraction
    {
        public override string AppendInsertIdQuery(string query)
        {
            return query + (!query.EndsWith(";") ? ";" : "") + " SELECT LASTVAL();";
        }
    }
}