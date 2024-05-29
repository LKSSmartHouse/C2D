// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub device SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples

using System;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;
using System.Linq;

using ClientMessage = Microsoft.Azure.Devices.Client.Message;
using DevicesMessage = Microsoft.Azure.Devices.Message;

using ClientTransportType = Microsoft.Azure.Devices.Client.TransportType;
using DevicesTransportType = Microsoft.Azure.Devices.TransportType;
using Microsoft.Azure.Amqp.Framing;

namespace simulatedDevice
{
    class SimulatedDevice
    {
        private static DeviceClient s_deviceClient;

        private static RegistryManager registryManager;

        // The device connection string to authenticate the device with your IoT hub.
        private const string s_connectionString = "HostName=iothub-deusto.azure-devices.net;DeviceId=smartmeter1;SharedAccessKey=z1f6oTrnx/tPFBizr41sD32lmoUB6xCLqs2JvgYMj9A=";

        // Async method to send simulated telemetry
        private static async void ReceiveC2dAsync()
        {
            Console.WriteLine("\nReceiving cloud to device messages from service");
            while (true)
            {
                registryManager = RegistryManager.CreateFromConnectionString("HostName=iothub-deusto.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=b9UWI1NdpA0lvAV6NMJ44G25pGzAOJnGNAIoTKukjgg=");
                Twin twin = await registryManager.GetTwinAsync("smartmeter1");
                var jsonContent = twin.ToJson();

                ClientMessage receivedMessage = await s_deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;
                Console.ForegroundColor = ConsoleColor.Yellow;
                String message = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                Console.WriteLine("Received message: {0}", message);
                List<String> desiredProperties = ReadDesiredProperties(message);
                UpdateTwin(jsonContent, desiredProperties);
                Console.ResetColor();

                await s_deviceClient.CompleteAsync(receivedMessage);
            }
        }
        private static List<String> ReadDesiredProperties(String message) {
            List<String> properties = new List<String>();
            String[] datas = message.Split(",");
            foreach (String s in datas) {
                properties.Add(s);
            }
            return properties;
        }
        
        static void UpdateTwin(string jsonContent, List<string> desiredProperties) {
            JObject jsonObject = JObject.Parse(jsonContent);
            Console.WriteLine(jsonObject.ToString());
            dynamic jsonObj = JsonConvert.DeserializeObject(jsonObject.ToString());
            jsonObj["properties"]["desired"]["frecuency"] = desiredProperties[0];
            jsonObj["properties"]["desired"]["reducir_consumo"] = desiredProperties[1];
            TwinCollection updatedTwin = new TwinCollection(jsonObj.ToString());
            Console.WriteLine(updatedTwin.ToString());
            string jsonPatch = $@"{{
                    'properties': {{
                        'desired': {{
                            'Frequency': '{desiredProperties[0]}',
                            'reducir_consumo': '{desiredProperties[1]}'
                            }}
                        }}
                    }}
                }}";
            Twin twin = registryManager.GetTwinAsync("smartmeter1");
            TwinCollection desiredProperties1 = new TwinCollection(jsonPatch);

            twin.Properties.Desired = desiredProperties1;

            // Actualizar el gemelo
            twin = registryManager.UpdateTwinAsync(jsonObj["deviceId"], twin, twin.ETag);

        }

        private static void Main()
        {
            Console.WriteLine("IoT Hub Quickstarts - Simulated device. Ctrl-C to exit.\n");

            // Connect to the IoT hub using the MQTT protocol
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_connectionString, ClientTransportType.Mqtt);
            ReceiveC2dAsync();
            Console.ReadLine();
        }
    }
}
