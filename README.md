# epi-stb-contemporaryresearch
plugin for entire range of contemporary research devices - only two-way feedback is for power.  Feel free to fork me!

``` json
{
	"key": "Tuner01",
	"uid": 1920,
	"name": "CR Tuner",
	"type": "contemporaryresearch",
	"group": "Tuners",
	"properties": 
	{
	    "unitId": "1",
	    "control": 
	    {
	        "controlPortDevKey": "processor",
	        "comParams": 
	        {
	            "dataBits": 8,
	            "softwareHandshake": "None",
	            "baudRate": 115200,
	            "parity": "None",
	            "stopBits": 1,
	            "hardwareHandshake": "None",
	            "protocol": "RS232"
	        },
	        "method": "com",
	        "controlPortNumber": 5
	    }
	}
}
```
