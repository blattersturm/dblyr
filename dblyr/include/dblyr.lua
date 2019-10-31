if not ScheduleResourceTick then
    if IsDuplicityVersion() then
        print("^1ERROR: dblyr can only run on FXServer 1550 or above. Returning a dummy object.^7")
    else
        print("^1ERROR: dblyr can only run as a server script. Returning a dummy object.^7")
    end

    dblyr = function()
        local oddMt

        oddMt = setmetatable({}, {
            __index = function()
                return oddMt
            end,
            __call = function()
                return oddMt
            end
        })

        return oddMt
    end

    return
end

local resultQueue = {}
local function queueResult(cb)
    table.insert(resultQueue, cb)
end

local function dblyrConnection(name, object)
    local conn = {
        name = name,
        conn = object,
        res = GetCurrentResourceName()
    }

    local api = {}

    AddEventHandler('onResourceStop', function(name)
        if name == 'dblyr' then
            conn.conn = nil
        end
    end)

    AddEventHandler('onServerResourceStart', function(name)
        if name == 'dblyr' then
            conn.conn = exports['dblyr']:getConnectionObject(conn.name)
        end
    end)

    function api.onReady(cb)
        CreateThread(function()
            while not conn.conn or not conn.conn.IsConnected() do
                Wait(0)
            end

            cb()
        end)
    end

    local function makeRequestSync(query, params, reqFn)
        if not params then
            params = {}
        end

        local err, result = table.unpack(reqFn(query, params))

        if err then
            error(err)
        end

        return result
    end

    local function makeRequest(query, params, cb, reqFn)
        if not cb and type(params) ~= 'table' then
            cb = params
            params = {}
        end

        if not params then
            params = {}
        end

        local req

        if not cb then
            req = {}

            cb = function(err, result)
                req.ready = true
                req.error = err
                req.result = result

                ScheduleResourceTick(conn.res)
            end
        else
            local lastCb = cb

            cb = function(err, result)
                queueResult(function()
                    if err then
                        error(err)
                    end

                    lastCb(result)
                end)

                ScheduleResourceTick(conn.res)
            end
        end

        reqFn(query, params, cb)

        if req then
            while not req.ready do
                Wait(0)
            end

            if req.error then
                error(req.error)
            end

            return req.result
        end
    end

    -- NOTE: these are repeated and return a temporary so that the Lua debug API
    -- can get useful function names from each function
    function api.insert(query, params, cb)
        local v = makeRequest(query, params, cb, conn.conn.Insert)
        return v
    end

    function api.execute(query, params, cb)
        local v = makeRequest(query, params, cb, conn.conn.Execute)
        return v
    end

    function api.fetchScalar(query, params, cb)
        local v = makeRequest(query, params, cb, conn.conn.FetchScalar)
        return v
    end

    function api.fetchAll(query, params, cb)
        local v = makeRequest(query, params, cb, conn.conn.FetchAll)
        return v
    end

    function api.insertSync(query, params)
        local v = makeRequestSync(query, params, conn.conn.InsertSync)
        return v
    end

    function api.executeSync(query, params)
        local v = makeRequestSync(query, params, conn.conn.ExecuteSync)
        return v
    end

    function api.fetchScalarSync(query, params)
        local v = makeRequestSync(query, params, conn.conn.FetchScalarSync)
        return v
    end

    function api.fetchAllSync(query, params)
        local v = makeRequestSync(query, params, conn.conn.FetchAllSync)
        return v
    end

    function api.executeTransaction(queries, params, cb)
        local v = makeRequest(queries, params, cb, conn.conn.ExecuteTransaction)
        return v
    end

    function api.executeTransactionSync(queries, params)
        local v = makeRequestSync(queries, params, conn.conn.ExecuteTransactionSync)
        return v
    end

    return api
end

CreateThread(function()
    while true do
        Wait(0)

        for _, r in ipairs(resultQueue) do
            local success, err = pcall(r)

            if not success then
                -- TODO: somehow handle its stack.. idk
                print(err)
            end
        end

        resultQueue = {}
    end
end)

local dblyrImpl = {

}

function dblyrImpl.getConnection(name)
    local connObject = GetResourceState('dblyr') == 'started' and exports['dblyr']:getConnectionObject(name) or nil

    return dblyrConnection(name, connObject)
end

function dblyr()
    return dblyrImpl
end