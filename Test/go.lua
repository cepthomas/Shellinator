
print("go.lua stdout")

rf = io.open ('_lua_out.txt', "a+") -- "w+")

local start_date = os.date()

rf:write('go.lua >>>>>\n')
rf:write(os.date(), '\n')

rf:close()
