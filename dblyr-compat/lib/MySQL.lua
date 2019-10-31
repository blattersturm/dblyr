if not ScheduleResourceTick then
    if IsDuplicityVersion() then
        print("^1ERROR: dblyr can only run on FXServer 1550 or above.^7")
    else
        print("^1ERROR: dblyr can only run as a server script.^7")
    end

    return
end

-- HACK as we can't load a random server_script import
if not dblyr then
    load(LoadResourceFile('dblyr', 'include/dblyr.lua'), '@@dblyr/include/dblyr.lua')()
end

MySQL = {
    Async = {},
    Sync = {}
}

local dblyr = dblyr()
local mysql = dblyr.getConnection('mysql')

function MySQL.Sync.execute(query, params)
    return mysql.execute(query, params)
end

function MySQL.Sync.fetchAll(query, params)
    return mysql.fetchAll(query, params)
end

function MySQL.Sync.fetchScalar(query, params)
    return mysql.fetchScalar(query, params)
end

function MySQL.Sync.insert(query, params)
    return mysql.insert(query, params)
end

function MySQL.Sync.transaction(queries, params)
    return mysql.executeTransaction(queries, params)
end

function MySQL.Async.execute(query, params, cb)
    return mysql.execute(query, params, cb)
end

function MySQL.Async.fetchAll(query, params, cb)
    return mysql.fetchAll(query, params, cb)
end

function MySQL.Async.fetchScalar(query, params, cb)
    return mysql.fetchScalar(query, params, cb)
end

function MySQL.Async.insert(query, params, cb)
    return mysql.insert(query, params, cb)
end

function MySQL.Async.transaction(queries, params, cb)
    return mysql.executeTransaction(queries, params, cb)
end

function MySQL.ready(cb)
    return mysql.onReady(cb)
end