using System.Collections.Generic;
using System.Threading.Tasks;

using static CitizenFX.Core.Native.API;

namespace CitizenFX.DataLayer
{   
    class DbContext
    {
        private Dictionary<string, FxDbConnection> m_connections = new Dictionary<string, FxDbConnection>();
        private DbFactoryList m_factoryList;

        public DbContext(DbFactoryList factoryList)
        {
            this.m_factoryList = factoryList;
        }

        public FxDbConnection GetConnection(string name)
        {
            if (!m_connections.TryGetValue(name, out var connection))
            {
                connection = new FxDbConnection(name, this.m_factoryList);
                m_connections[name] = connection;
            }

            return connection;
        }

        public Task Tick()
        {
            foreach (var conn in m_connections)
            {
                conn.Value.Tick();
            }

            return Task.CompletedTask;
        }
    }
}