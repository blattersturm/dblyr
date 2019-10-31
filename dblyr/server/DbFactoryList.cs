using System;
using System.Collections.Generic;
using System.Data.Common;

namespace CitizenFX.DataLayer
{
    public class DbFactoryList
    {
        private Dictionary<string, DbProviderFactory> m_factories = new Dictionary<string, DbProviderFactory>();
        private Dictionary<string, DbDataAbstraction> m_dataAbstraction = new Dictionary<string, DbDataAbstraction>();

        public DbFactoryList()
        {
            m_factories.Add("mysql", MySql.Data.MySqlClient.MySqlClientFactory.Instance);
            m_factories.Add("pgsql", Npgsql.NpgsqlFactory.Instance);

            m_dataAbstraction.Add("mysql", new MySqlDataAbstraction());
            m_dataAbstraction.Add("pgsql", new PgsqlDataAbstraction());
        }

        public DbProviderFactory GetFactory(string name)
        {
            if (m_factories.TryGetValue(name, out var factory))
            {
                return factory;
            }

            return null;
        }

        public DbDataAbstraction GetDataAbstraction(string name)
        {
            if (m_dataAbstraction.TryGetValue(name, out var da))
            {
                return da;
            }

            return null;
        }

        public IEnumerable<string> GetNames()
        {
            return m_factories.Keys;
        }
    }
}