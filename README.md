# Screendemo
### Screen sharing over http
## Description
Console utility for transferring a screen image. Written on C# with SharpDX for Windows.
## Arguments
Show help:
```
--help
```

Use that url for site:
```
--url "http://localhost:8000/"
```

Show verbose messages:
```
--verbose
```

## Publication
To publish on a server with a white ip address, you can use any traffic tunneling utility (ngrok like).
**The only requirement is to replace host in the http headers with the one passed in the url.**

An example of a working config for frp (https://github.com/fatedier/frp) on the client side:
```
[common]
server_addr = your_frp_server_address
server_port = your_frp_server_port

[web]
type = http
local_ip = 127.0.0.1
local_port = 8000
host_header_rewrite = localhost:8000
custom_domains = your_frp_server_address
locations = /
```
## Build and run
The program is built in Visual Studio 2022.

In the folder with exe files, you need to put the `resources` folder from `bin\Debug\net6.0`
## Download prebuild binaries
You can download the compiled program in the releases section
