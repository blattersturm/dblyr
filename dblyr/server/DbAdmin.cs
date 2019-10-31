using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using DotLiquid;
using StackExchange.Profiling;
using static CitizenFX.Core.Native.API;

namespace CitizenFX.DataLayer
{
    public class DbAdmin : BaseScript
    {
        private bool ticked;

        [Tick]
        public Task OnTick()
        {
            if (!ticked)
            {
                if (GetResourceState("webadmin") == "started")
                {
                    InitWebadmin();
                }

                ticked = true;
            }

            return Task.CompletedTask;
        }

        private void InitWebadmin()
        {
            var homeTpl = Template.Parse(LoadResourceFile(GetCurrentResourceName(), "Views/home.liquid"));

            Exports["webadmin"].registerPluginPage("dblyr-db", new Func<IDictionary<string, object>, string>((args) =>
            {
                if (!Exports["webadmin"].isInRole("webadmin.dblyr"))
                {
                    return "";
                }

                if (!args.TryGetValue("mode", out var mode))
                {
                    mode = "home";
                }
                
                var modeStr = mode.ToString();
                switch (modeStr)
                {
                    case "home":
                    {
                        var profiler = DbMain.Self.Profiler;

                        var timings = new Stack<Timing>();
                        timings.Push(profiler.Root);

                        var queries = new List<Tuple<string, CustomTiming>>();

                        while (timings.Count > 0)
                        {
                            var timing = timings.Pop();

                            if (timing.CustomTimings.TryGetValue("sql", out var sqls))
                            {
                                foreach (var ct in sqls)
                                {
                                    if (ct.ExecuteType != "OpenAsync")
                                    {
                                        queries.Add(Tuple.Create(timing.Name, ct));
                                    }
                                }
                            }

                            if (timing.HasChildren)
                            {
                                var children = timing.Children;
                                for (var i = children.Count - 1; i >= 0; i--) timings.Push(children[i]);
                            }
                        }

                        var latestQueries = queries.OrderByDescending(a => a.Item2.StartMilliseconds).Take(25).Select(a => Hash.FromAnonymousObject(new
                        {
                            ResourceName = a.Item1,
                            Command = a.Item2.CommandString,
                            CommandType = a.Item2.ExecuteType,
                            Duration = (float)(a.Item2.DurationMilliseconds ?? 0)
                        }));

                        return homeTpl.Render(Hash.FromAnonymousObject(new
                        {
                            RecentQueries = latestQueries
                        }));
                    }
                }

                return "";
            }));

            Exports["webadmin"].registerPluginOutlet("nav/sideList", new Func<string>(() =>
            {
                if (!Exports["webadmin"].isInRole("webadmin.dblyr"))
                {
                    return "";
                }

                return $@"<li class=""nav-item"">
                    <a class=""nav-link"" href=""{Exports["webadmin"].getPluginUrl("dblyr-db")}"">
                        <i class=""nav-icon fa fa-database""></i> Database
                    </a>
                </li>";
            }));
        }
    }
}