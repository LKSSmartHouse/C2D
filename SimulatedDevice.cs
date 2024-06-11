// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This application uses the Azure IoT Hub device SDK for .NET
// For samples see: https://github.com/Azure/azure-iot-sdk-csharp/tree/master/iothub/device/samples

using System;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices;
using System.Text;
using System.Collections.Generic;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

using ClientMessage = Microsoft.Azure.Devices.Client.Message;
using ClientTransportType = Microsoft.Azure.Devices.Client.TransportType;

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

                ClientMessage receivedMessage = await s_deviceClient.ReceiveAsync();
                if (receivedMessage == null) continue;

                Console.ForegroundColor = ConsoleColor.Yellow;
                String message = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                Console.WriteLine("Received message: {0}", message);

                List<String> desiredProperties = ReadDesiredProperties(message, twin);

                string jsonPatch = @"{ 'properties': { 'desired': {";
                if (desiredProperties.Count > 0 && !string.IsNullOrEmpty(desiredProperties[0]))
                {
                    jsonPatch += $"'mode_': '{desiredProperties[0]}',";
                }
                if (desiredProperties.Count > 1 && !string.IsNullOrEmpty(desiredProperties[1]))
                {
                    jsonPatch += $"'furnance1Fixed_': '{desiredProperties[1]}',";
                }
                if (desiredProperties.Count > 2 && !string.IsNullOrEmpty(desiredProperties[2]))
                {
                    jsonPatch += $"'furnance2Fixed_': '{desiredProperties[2]}',";
                }
                if (desiredProperties.Count > 3 && !string.IsNullOrEmpty(desiredProperties[3]))
                {
                    jsonPatch += $"'freq_': '{desiredProperties[3]}',";
                }
                jsonPatch = jsonPatch.TrimEnd(','); 
                jsonPatch += "} } }";

                await registryManager.UpdateTwinAsync(twin.DeviceId, jsonPatch, twin.ETag);
                Console.WriteLine("Device Twin actualizado");
                Console.ResetColor();

                await s_deviceClient.CompleteAsync(receivedMessage);
            }
        }
        private static List<String> ReadDesiredProperties(String message, Twin twin) {
            List<String> properties = new List<String>();

            try {
                JObject jsonMessage = JObject.Parse(message);

                if (jsonMessage.TryGetValue("mode_", out JToken modeToken))
                {
                    properties.Add(modeToken.ToString());
                }
                else if (twin.Properties.Desired.Contains("mode_"))
                {
                    properties.Add(twin.Properties.Desired["mode_"].ToString());
                }

                if (jsonMessage.TryGetValue("furnance1Fixed_", out JToken furnance1Token))
                {
                    properties.Add(furnance1Token.ToString());
                }
                else if (twin.Properties.Desired.Contains("furnance1Fixed_"))
                {
                    properties.Add(twin.Properties.Desired["furnance1Fixed_"].ToString());
                }

                if (jsonMessage.TryGetValue("furnance2Fixed_", out JToken furnance2Token))
                {
                    properties.Add(furnance2Token.ToString());
                }
                else if (twin.Properties.Desired.Contains("furnance2Fixed_"))
                {
                    properties.Add(twin.Properties.Desired["furnance2Fixed_"].ToString());
                }

                if (jsonMessage.TryGetValue("freq_", out JToken freqToken))
                {
                    properties.Add(freqToken.ToString());
                }
                else if (twin.Properties.Desired.Contains("freq_"))
                {
                    properties.Add(twin.Properties.Desired["freq_"].ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al parsear el mensaje JSON: {ex.Message}");
            }

            return properties;
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
